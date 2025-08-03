using Blazeditor.Application.Models;
using System.Text.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Png;
using Size = Blazeditor.Application.Models.Size;

namespace Blazeditor.Application.Services;

public class DefinitionManager : IDisposable
{
    private readonly DefinitionSerializerService _serializerService;
    public bool IsDirty { get; private set; } = false;
    private Definition _definition = new Definition();
    public Area? SelectedArea { get; set; }

    public event Action<Definition>? OnChanged;
    // --- Undo/Redo for Tile Edits ---
    private class TileEditUndo
    {
        public int AreaId { get; set; }
        public int TileMapLevel { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public Tile? OldTile { get; set; }
        public Tile? NewTile { get; set; }
    }
    private readonly Stack<TileEditUndo> _tileUndoStack = new();
    private readonly Stack<TileEditUndo> _tileRedoStack = new();

    public DefinitionManager(IHttpContextAccessor httpContextAccessor)
    {
        Console.WriteLine("Creating DefinitionManager.");
        _serializerService = new DefinitionSerializerService(httpContextAccessor);
        LoadDefinitionAsync().GetAwaiter().GetResult();
    }

    public void ExecuteTileEdit(int areaId, int level, int x, int y, Tile? newTile)
    {
        if (!_definition.Areas.TryGetValue(areaId, out var area)) return;
        if (!area.TileMaps.TryGetValue(level, out var map)) return;
        var oldPlacement = map.GetPlacement(x, y);
        var oldTile = oldPlacement != null && oldPlacement.TileId.HasValue && oldPlacement.PaletteId.HasValue &&  GetPalette(oldPlacement.PaletteId.Value).Tiles.TryGetValue(oldPlacement.TileId.Value, out var t) ? t : null;
        var undo = new TileEditUndo
        {
            AreaId = areaId,
            TileMapLevel = level,
            X = x,
            Y = y,
            OldTile = oldTile,
            NewTile = newTile
        };
        _tileUndoStack.Push(undo);
        _tileRedoStack.Clear();
        map.SetPlacement(x, y, newTile?.Id, newTile?.SourcePaletteId);
        MarkDirty();
        OnChanged?.Invoke(_definition);
    }

    public void UndoTileEdit()
    {
        if (_tileUndoStack.Count == 0) return;
        var edit = _tileUndoStack.Pop();
        if (!_definition.Areas.TryGetValue(edit.AreaId, out var area)) return;
        if (!area.TileMaps.TryGetValue(edit.TileMapLevel, out var map)) return;
        map.SetPlacement(edit.X, edit.Y, edit.OldTile?.Id, edit.OldTile?.SourcePaletteId);
        _tileRedoStack.Push(edit);
        MarkDirty();
        OnChanged?.Invoke(_definition);
    }

    public void RedoTileEdit()
    {
        if (_tileRedoStack.Count == 0) return;
        var edit = _tileRedoStack.Pop();
        if (!_definition.Areas.TryGetValue(edit.AreaId, out var area)) return;
        if (!area.TileMaps.TryGetValue(edit.TileMapLevel, out var map)) return;
        map.SetPlacement(edit.X, edit.Y, edit.NewTile?.Id, edit.NewTile?.SourcePaletteId);
        _tileUndoStack.Push(edit);
        MarkDirty();
        OnChanged?.Invoke(_definition);
    }

    public virtual async Task SaveAsync()
    {
        try
        {
            _serializerService.SaveDefinition(_definition);
            IsDirty = false;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[DefinitionManager] Error saving definition: {ex.Message}\n{ex.StackTrace}");
        }
        await Task.CompletedTask;
    }

    public virtual async Task LoadDefinitionAsync()
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
        await Task.CompletedTask;
    }

    private void MarkDirty() => IsDirty = true;

    public IEnumerable<Area> GetAreas()
    {
        return _definition.Areas.Values;
    }

    public Area AddArea(string name, string description, Size areaSize, int cellSize = 64)
    {
        var area = new Area(name, description, areaSize, cellSize);
        _definition.Areas[area.Id] = area;
        MarkDirty();
        OnChanged?.Invoke(_definition);
        return area;
    }

    public Area AddArea(string name, string description, int width, int height, int cellSize = 64)
    {
        var area = new Area(name, description, new Size(width, height), cellSize);
        area.TileMaps[0] = new TileMap("Default", $"{name} map", 0, area.Size);
        _definition.Areas[area.Id] = area;
        MarkDirty();
        OnChanged?.Invoke(_definition);
        return area;
    }

    public void RemoveArea(int areaId)
    {
        _definition.Areas.Remove(areaId);
        MarkDirty();
        OnChanged?.Invoke(_definition);
    }

    public bool IsPaletteUsed(int paletteId)
    {
        return _definition.Areas.Values.Any(a => a.TilePaletteIds.Any(p=> p == paletteId));
    }

    public void RemoveTilePalette(int paletteId)
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

    public TilePalette GetPalette(int paletteId)
    {
        if (_definition.TilePalettes.TryGetValue(paletteId, out var palette))
            return palette;
        throw new KeyNotFoundException($"Tile palette with ID {paletteId} not found.");
    }

    public IEnumerable<TilePalette> GetTilePalettes()
    {
        return _definition.TilePalettes.Values;
    }

    public Dictionary<int, Tile> ExtractTilesFromJsonTilesheet(string jsonFilename, int paletteId)
    {
        if (string.IsNullOrWhiteSpace(jsonFilename)) throw new ArgumentException("Filename cannot be null or empty.", nameof(jsonFilename));
        var jsonPath = Path.Combine("wwwroot", "tilesets", jsonFilename);
        if (!File.Exists(jsonPath)) throw new FileNotFoundException($"JSON file not found: {jsonPath}");
        var json = File.ReadAllText(jsonPath);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var tiles = root.GetProperty("tiles");
        var cellSizeVal = root.GetProperty("cellSize").GetInt32();
        var cellSize = new Size { Width = cellSizeVal, Height = cellSizeVal };
        var sheets = root.GetProperty("sheets").EnumerateArray().Select(s => s.GetString()).ToList() ?? [];
        var result = new Dictionary<int, Tile>();
        // Load all sheet images
        var sheetImages = new List<Image<Rgba32>>();
        foreach (var sheet in sheets)
        {
            var imagePath = Path.Combine("wwwroot", "tilesets", sheet!);
            if (!File.Exists(imagePath)) throw new FileNotFoundException($"Image file not found: {imagePath}");
            sheetImages.Add(Image.Load<Rgba32>(imagePath));
        }
        foreach (var tileElem in tiles.EnumerateArray())
        {
            string name = tileElem.GetProperty("name").GetString() ?? "tile";
            string description = tileElem.GetProperty("description").GetString() ?? string.Empty;
            int w = tileElem.GetProperty("w").GetInt32();
            int h = tileElem.GetProperty("h").GetInt32();
            int x = tileElem.GetProperty("x").GetInt32();
            int y = tileElem.GetProperty("y").GetInt32();
            int sheetIdx = tileElem.TryGetProperty("sheet", out var sheetProp) ? sheetProp.GetInt32() : 0;
            if (sheetIdx < 0 || sheetIdx >= sheetImages.Count) continue;
            var image = sheetImages[sheetIdx];
            var rect = new Rectangle(
                x * cellSize.Width,
                y * cellSize.Height,
                w * cellSize.Width,
                h * cellSize.Height
            );
            using (var tileImg = image.Clone(ctx => ctx.Crop(rect)))
            using (var ms = new MemoryStream())
            {
                tileImg.Save(ms, new PngEncoder());
                var base64 = Convert.ToBase64String(ms.ToArray());
                var base64Url = $"data:image/png;base64,{base64}";
                var tile = new Tile(name, description, sheets[sheetIdx] ?? "sheet", base64Url, new Size(w, h), paletteId);
                result.Add(tile.Id, tile);
            }
        }
        foreach (var img in sheetImages) img.Dispose();
        return result;
    }

    public TilePalette ImportTileset(string jsonFilename, int? paletteId)
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
        Dictionary<int, Tile> tiles = new Dictionary<int, Tile>();
        var jsonPath = Path.Combine("wwwroot", "tilesets", jsonFilename);
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
        // No longer need to dispose LiteDatabase here, handled by serializer service if needed
    }
}
