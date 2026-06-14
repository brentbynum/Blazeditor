using Blazeditor.Application.Models;
using System.Text.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Png;
using Size = Blazeditor.Application.Models.Size;

namespace Blazeditor.Application.Services;

/// <summary>
/// Scoped service that owns the in-memory <see cref="Definition"/> graph (areas, tile maps,
/// tile palettes, portals) for the current user session. It is the single point through which
/// the editor UI reads and mutates game data, persists it via <see cref="DefinitionSerializerService"/>,
/// and is notified of changes via <see cref="OnChanged"/>.
/// One instance is created per DI scope (per circuit/session in Blazor Server).
/// </summary>
public class DefinitionManager : IDisposable
{
    private readonly DefinitionSerializerService _serializerService;
    private readonly IWebHostEnvironment _env;

    /// <summary>True when the in-memory <see cref="Definition"/> has unsaved changes.</summary>
    public bool IsDirty { get; private set; } = false;

    /// <summary>The full definition graph currently loaded into memory. Replaced wholesale on <see cref="LoadDefinition"/>.</summary>
    private Definition _definition = new Definition();

    /// <summary>The area currently focused in the editor UI. Not persisted and does not raise <see cref="OnChanged"/>.</summary>
    public Area? SelectedArea { get; set; }

    /// <summary>Raised after any mutation to <see cref="_definition"/> so UI components can refresh.</summary>
    public event Action<Definition>? OnChanged;

    // --- Undo/Redo for Tile Edits ---
    // Captures enough state to reverse a single ExecuteTileEdit call: the tile/elevation that
    // was at (X, Y) on the given area/tile-map level before the edit, and the values applied by it.
    private class TileEditUndo
    {
        public Guid AreaId { get; set; }
        public int TileMapLevel { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int OldElevation { get; set; }
        public int NewElevation { get; set; }
        public Tile? OldTile { get; set; }
        public Tile? NewTile { get; set; }
    }
    private readonly Stack<TileEditUndo> _tileUndoStack = new();
    private readonly Stack<TileEditUndo> _tileRedoStack = new();

    public DefinitionManager(DefinitionSerializerService serializerService, IWebHostEnvironment env)
    {
        Console.WriteLine("Creating DefinitionManager.");
        _serializerService = serializerService;
        _env = env;
        // Synchronously block on the initial load so the definition is ready as soon as the
        // service is constructed (DI does not support async construction).
        LoadDefinition();
    }

    /// <summary>
    /// Places (or clears, if <paramref name="newTile"/> is null) a tile at (<paramref name="x"/>, <paramref name="y"/>)
    /// on the given area/level, recording the previous state on the undo stack and clearing the redo stack.
    /// </summary>
    public void ExecuteTileEdit(Guid areaId, int level, int x, int y, Tile? newTile, int elevation = 0)
    {
        if (!_definition.Areas.TryGetValue(areaId, out var area)) return;
        if (!area.TileMaps.TryGetValue(level, out var map)) return;
        var oldPlacement = map.GetPlacement(x, y);
        // Resolve the previously placed tile (if any) from its source palette so the undo entry
        // can restore the exact tile reference, not just its id.
        var oldTile = oldPlacement != null && oldPlacement.TileId.HasValue && oldPlacement.PaletteId.HasValue && GetPalette(oldPlacement.PaletteId.Value).Tiles.TryGetValue(oldPlacement.TileId.Value, out var t) ? t : null;
        var undo = new TileEditUndo
        {
            AreaId = areaId,
            TileMapLevel = level,
            X = x,
            Y = y,
            NewElevation = elevation,
            OldElevation = oldPlacement?.Elevation ?? 0,
            OldTile = oldTile,
            NewTile = newTile
        };
        _tileUndoStack.Push(undo);
        _tileRedoStack.Clear();
        map.SetPlacement(x, y, newTile?.Id, newTile?.SourcePaletteId, elevation);
        MarkDirty();
        OnChanged?.Invoke(_definition);
    }

    /// <summary>Reverts the most recent <see cref="ExecuteTileEdit"/> and pushes it onto the redo stack.</summary>
    public void UndoTileEdit()
    {
        if (_tileUndoStack.Count == 0) return;
        var edit = _tileUndoStack.Pop();
        if (!_definition.Areas.TryGetValue(edit.AreaId, out var area)) return;
        if (!area.TileMaps.TryGetValue(edit.TileMapLevel, out var map)) return;
        map.SetPlacement(edit.X, edit.Y, edit.OldTile?.Id, edit.OldTile?.SourcePaletteId, edit.OldElevation);
        _tileRedoStack.Push(edit);
        MarkDirty();
        OnChanged?.Invoke(_definition);
    }

    /// <summary>Re-applies the most recently undone <see cref="ExecuteTileEdit"/> and pushes it back onto the undo stack.</summary>
    public void RedoTileEdit()
    {
        if (_tileRedoStack.Count == 0) return;
        var edit = _tileRedoStack.Pop();
        if (!_definition.Areas.TryGetValue(edit.AreaId, out var area)) return;
        if (!area.TileMaps.TryGetValue(edit.TileMapLevel, out var map)) return;
        map.SetPlacement(edit.X, edit.Y, edit.NewTile?.Id, edit.NewTile?.SourcePaletteId, edit.NewElevation);
        _tileUndoStack.Push(edit);
        MarkDirty();
        OnChanged?.Invoke(_definition);
    }

    /// <summary>
    /// Persists the entire in-memory <see cref="Definition"/> (areas, tile palettes, portals) to the
    /// database and clears <see cref="IsDirty"/>. Errors are logged but not rethrown, so callers cannot
    /// detect failure other than via <see cref="IsDirty"/> remaining true.
    /// </summary>
    public virtual async Task SaveAsync()
    {
        try
        {
            await _serializerService.SaveDefinitionAsync(_definition);
            IsDirty = false;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[DefinitionManager] Error saving definition: {ex.Message}\n{ex.StackTrace}");
        }
    }

    /// <summary>
    /// Replaces the in-memory <see cref="Definition"/> with the contents of the database and resets
    /// <see cref="IsDirty"/>. Each loaded tile map is resized to match its area's current
    /// <see cref="Area.Size"/>, since an area's configured size may have changed since the tile map was
    /// last saved.
    /// </summary>
    public virtual void LoadDefinition()
    {
        try
        {
            _definition = _serializerService.LoadDefinition();
            foreach (var area in _definition.Areas)
            {
                foreach (var tileMap in area.Value.TileMaps)
                {
                    tileMap.Value.Resize(area.Value.Size);
                    // No need to rebuild Tiles
                }
            }
            IsDirty = false;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[DefinitionManager] Error loading definition: {ex.Message}\n{ex.StackTrace}");
        }
    }

    private void MarkDirty() => IsDirty = true;

    public IEnumerable<Area> GetAreas()
    {
        return _definition.Areas.Values;
    }

    /// <summary>Adds a new, empty area (no tile maps) to the definition.</summary>
    public Area AddArea(string name, string description, Size areaSize, int cellSize = 64)
    {
        var area = new Area(name, description, areaSize, cellSize);
        _definition.Areas[area.Id] = area;
        MarkDirty();
        OnChanged?.Invoke(_definition);
        return area;
    }

    /// <summary>Adds a new area sized in 32x32 grid units, pre-populated with a single "Default" tile map at layer 0.</summary>
    public Area AddArea(string name, string description, int width, int height, int cellSize = 64)
    {
        var area = new Area(name, description, new Size(width, height), cellSize);
        area.TileMaps[0] = new TileMap("Default", $"{name} map", 0, area.Size);
        _definition.Areas[area.Id] = area;
        MarkDirty();
        OnChanged?.Invoke(_definition);
        return area;
    }

    /// <summary>Removes an area and its tile maps. No-op if the area does not exist.</summary>
    public void RemoveArea(Guid areaId)
    {
        _definition.Areas.Remove(areaId);
        MarkDirty();
        OnChanged?.Invoke(_definition);
    }

    /// <summary>
    /// Adds a tile palette to an area's <see cref="Area.TilePaletteIds"/> if not already present,
    /// marking the definition dirty and raising <see cref="OnChanged"/>. No-op if already present.
    /// </summary>
    public void AddTilePaletteToArea(Area area, Guid paletteId)
    {
        if (area.TilePaletteIds.Contains(paletteId)) return;
        area.TilePaletteIds.Add(paletteId);
        MarkDirty();
        OnChanged?.Invoke(_definition);
    }

    /// <summary>True if any area references the given tile palette via <see cref="Area.TilePaletteIds"/>.</summary>
    public bool IsPaletteUsed(Guid paletteId)
    {
        return _definition.Areas.Values.Any(a => a.TilePaletteIds.Any(p => p == paletteId));
    }

    /// <summary>
    /// Removes a tile palette, throwing if it does not exist or is still referenced by an area's
    /// <see cref="Area.TilePaletteIds"/> (per <see cref="IsPaletteUsed"/>).
    /// </summary>
    public void RemoveTilePalette(Guid paletteId)
    {
        if (!_definition.TilePalettes.ContainsKey(paletteId))
            throw new KeyNotFoundException($"Tile palette with ID {paletteId} not found.");
        if (IsPaletteUsed(paletteId))
            throw new InvalidOperationException($"Cannot remove tile palette {paletteId} because it is still in use by one or more areas.");
        if (_definition.TilePalettes.Remove(paletteId))
        {
            MarkDirty();
            OnChanged?.Invoke(_definition);
        }
    }

    public TilePalette GetPalette(Guid paletteId)
    {
        if (_definition.TilePalettes.TryGetValue(paletteId, out var palette))
            return palette;
        throw new KeyNotFoundException($"Tile palette with ID {paletteId} not found.");
    }

    public IEnumerable<TilePalette> GetTilePalettes()
    {
        return _definition.TilePalettes.Values;
    }

    /// <summary>
    /// Reads a tilesheet manifest (e.g. wwwroot/tilesets/tilesheet_packed.json) describing one or more
    /// source images ("sheets") and the individual tile rectangles ("tiles") within them, crops each
    /// tile out of its sheet image, and returns it as a new <see cref="Tile"/> with an embedded
    /// base64-encoded PNG, keyed by the tile's generated id. Tiles are not added to any palette by this
    /// method - see <see cref="ImportTileset"/>.
    /// </summary>
    public Dictionary<Guid, Tile> ExtractTilesFromJsonTilesheet(string jsonFilename, Guid paletteId)
    {
        var jsonPath = Path.Combine(_env.WebRootPath, "tilesets", jsonFilename);
        if (!File.Exists(jsonPath)) throw new FileNotFoundException($"JSON file not found: {jsonPath}");
        var json = File.ReadAllText(jsonPath);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Parse sheets array as objects
        var sheets = new List<(string sheetName, int cellSize, int id)>();
        var sheetImages = new Dictionary<int, Image<Rgba32>>();
        if (root.TryGetProperty("sheets", out var sheetsElem))
        {
            foreach (var sheetObj in sheetsElem.EnumerateArray())
            {
                var sheetName = sheetObj.GetProperty("sheetName").GetString() ?? "";
                var cellSize = sheetObj.GetProperty("cellSize").GetInt32();
                var id = sheetObj.GetProperty("id").GetInt32();
                sheets.Add((sheetName, cellSize, id));
                var imagePath = Path.Combine(_env.WebRootPath, "tilesets", sheetName);
                if (!File.Exists(imagePath)) throw new FileNotFoundException($"Image file not found: {imagePath}");
                sheetImages[id] = Image.Load<Rgba32>(imagePath);
            }
        }

        var result = new Dictionary<Guid, Tile>();
        var tilesElem = root.GetProperty("tiles");
        foreach (var tileElem in tilesElem.EnumerateArray())
        {
            string name = tileElem.GetProperty("name").GetString() ?? "tile";
            string description = tileElem.GetProperty("description").GetString() ?? string.Empty;
            int w = tileElem.GetProperty("w").GetInt32();
            int h = tileElem.GetProperty("h").GetInt32();
            int x = tileElem.GetProperty("x").GetInt32();
            int y = tileElem.GetProperty("y").GetInt32();
            int sheetIdx = tileElem.GetProperty("sheet").GetInt32();

            // Find the sheet info
            var sheetInfo = sheets.First(s => s.id == sheetIdx);
            var image = sheetImages[sheetIdx];
            var cellSize = sheetInfo.cellSize;

            var rect = new Rectangle(
                x * cellSize,
                y * cellSize,
                w * cellSize,
                h * cellSize
            );
            using (var tileImg = image.Clone(ctx => ctx.Crop(rect)))
            using (var ms = new MemoryStream())
            {
                tileImg.Save(ms, new PngEncoder());
                var base64 = Convert.ToBase64String(ms.ToArray());
                var base64Url = $"data:image/png;base64,{base64}";
                var tile = new Tile(name, description, sheetInfo.sheetName, base64Url, new Size(w, h), paletteId);
                result.Add(tile.Id, tile);
            }
        }
        foreach (var img in sheetImages.Values) img.Dispose();
        return result;
    }

    /// <summary>
    /// Imports tiles from a tilesheet manifest into a tile palette, creating a new palette unless
    /// <paramref name="paletteId"/> identifies an existing one. Tiles whose id already exists in the
    /// target palette are skipped, so re-importing the same manifest will not duplicate or overwrite tiles.
    /// </summary>
    public TilePalette ImportTileset(string jsonFilename, Guid? paletteId)
    {
        TilePalette palette;
        if (paletteId.HasValue && _definition.TilePalettes.TryGetValue(paletteId.Value, out var existingPalette))
        {
            // Use existing palette
            palette = existingPalette;
        }
        else
        {
            // Create new palette
            var name = Path.GetFileNameWithoutExtension(jsonFilename);
            var description = $"Imported from {jsonFilename}";
            palette = new TilePalette(name, description);
            _definition.TilePalettes[palette.Id] = palette;
        }
        Dictionary<Guid, Tile> tiles = new Dictionary<Guid, Tile>();
        var jsonPath = Path.Combine(_env.WebRootPath, "tilesets", jsonFilename);
        if (File.Exists(jsonPath))
        {
            tiles = ExtractTilesFromJsonTilesheet(jsonFilename, palette.Id);
        }

        foreach (var tile in tiles.Values)
        {
            if (palette.Tiles.ContainsKey(tile.Id))
            {
                // If tile already exists, skip it
                continue;
            }
            palette.Tiles[tile.Id] = tile;
        }
        MarkDirty();
        OnChanged?.Invoke(_definition);
        return palette;
    }

    public void Dispose()
    {
        // No-op: BlazeditorDbContext is owned by DI and disposed at the end of the scope.
    }
}
