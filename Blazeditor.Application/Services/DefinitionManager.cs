using Blazeditor.Application.Models;
using System.Text.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Png;
using LiteDB;

using Size = Blazeditor.Application.Models.Size;
using System.Text.Json.Serialization;

namespace Blazeditor.Application.Services;

public class DefinitionManager : IDisposable
{
    private readonly LiteDatabase _db;
    private readonly string _userKey;
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
        _db = new LiteDatabase("Filename=definitions.db;Connection=shared");
        var user = httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "guest";
        _userKey = user;
        LoadDefinitionAsync().GetAwaiter().GetResult();
    }

    public void ExecuteTileEdit(int areaId, int level, int x, int y, Tile? newTile)
    {
        if (!_definition.Areas.TryGetValue(areaId, out var area)) return;
        if (!area.TileMaps.TryGetValue(level, out var map)) return;
        var oldPlacement = map.GetPlacement(x, y);
        var oldTile = oldPlacement != null && oldPlacement.TileId.HasValue && area.TilePalette.TryGetValue(oldPlacement.TileId.Value, out var t) ? t : null;
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
        map.SetPlacement(x, y, newTile?.Id);
        MarkDirty();
        OnChanged?.Invoke(_definition);
    }

    public void UndoTileEdit()
    {
        if (_tileUndoStack.Count == 0) return;
        var edit = _tileUndoStack.Pop();
        if (!_definition.Areas.TryGetValue(edit.AreaId, out var area)) return;
        if (!area.TileMaps.TryGetValue(edit.TileMapLevel, out var map)) return;
        map.SetPlacement(edit.X, edit.Y, edit.OldTile?.Id);
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
        map.SetPlacement(edit.X, edit.Y, edit.NewTile?.Id);
        _tileUndoStack.Push(edit);
        MarkDirty();
        OnChanged?.Invoke(_definition);
    }

    public virtual async Task SaveAsync()
    {
        try
        {
            var col = _db.GetCollection<Definition>("definitions");
            // No need to update TilePlacements
            col.Upsert(_userKey, _definition);
            _db.Checkpoint();
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
            var col = _db.GetCollection<Definition>("definitions");
            var loaded = col.FindById(_userKey);
            if (loaded != null)
                _definition = loaded;
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

    public List<string?> GetTileImageFilenames()
    {
        // Return all unique image filenames from all tile palettes
        var filenames = new HashSet<string?>();
        foreach (var area in _definition.Areas.Values)
        {
            foreach (var tile in area.TilePalette.Values)
            {
                if (!string.IsNullOrEmpty(tile.Image))
                    filenames.Add(tile.Image);
            }
        }
        return filenames.ToList();
    }

    public Dictionary<int, Tile> ExtractTilesFromImage(string filename)
    {
        if (string.IsNullOrWhiteSpace(filename)) throw new ArgumentException("Filename cannot be null or empty.", nameof(filename));
        var baseName = System.IO.Path.GetFileNameWithoutExtension(filename);
        var imagePath = System.IO.Path.Combine("wwwroot", "tilesets", baseName + ".png");
        var jsonPath = System.IO.Path.Combine("wwwroot", "tilesets", baseName + ".json");

        if (!System.IO.File.Exists(imagePath)) throw new FileNotFoundException($"Image file not found: {imagePath}");
        if (!System.IO.File.Exists(jsonPath)) throw new FileNotFoundException($"JSON file not found: {jsonPath}");

        var json = System.IO.File.ReadAllText(jsonPath);
        var doc = JsonDocument.Parse(json);
        var tiles = doc.RootElement.GetProperty("tiles");
        var cellSizeVal = doc.RootElement.GetProperty("cellSize").GetInt32();
        var cellSize = new Size
        {
            Width = cellSizeVal,
            Height = cellSizeVal
        };

        var result = new Dictionary<int, Tile>();
        using (var image = Image.Load<Rgba32>(imagePath))
        {
            foreach (var tileElem in tiles.EnumerateArray())
            {
                string name = tileElem.GetProperty("name").GetString() ?? "tile";
                string description = tileElem.GetProperty("description").GetString() ?? string.Empty;
                int w = tileElem.GetProperty("w").GetInt32();
                int h = tileElem.GetProperty("h").GetInt32();
                int x = tileElem.GetProperty("x").GetInt32();
                int y = tileElem.GetProperty("y").GetInt32();

                var rect = new SixLabors.ImageSharp.Rectangle(
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
                    var tile = new Tile(name, description, "default", base64Url, new Size(w, h));
                    result.Add(tile.Id, tile);
                }
            }
        }
        return result;
    }

    public Dictionary<int, Tile> ExtractTilesFromJsonTilesheet(string jsonFilename)
    {
        if (string.IsNullOrWhiteSpace(jsonFilename)) throw new ArgumentException("Filename cannot be null or empty.", nameof(jsonFilename));
        var jsonPath = System.IO.Path.Combine("wwwroot", "tilesets", jsonFilename);
        if (!System.IO.File.Exists(jsonPath)) throw new FileNotFoundException($"JSON file not found: {jsonPath}");
        var json = System.IO.File.ReadAllText(jsonPath);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var tiles = root.GetProperty("tiles");
        var cellSizeVal = root.GetProperty("cellSize").GetInt32();
        var cellSize = new Size { Width = cellSizeVal, Height = cellSizeVal };
        var sheets = root.GetProperty("sheets").EnumerateArray().Select(s => s.GetString()).ToList();
        var result = new Dictionary<int, Tile>();
        // Load all sheet images
        var sheetImages = new List<Image<Rgba32>>();
        foreach (var sheet in sheets)
        {
            var imagePath = System.IO.Path.Combine("wwwroot", "tilesets", sheet);
            if (!System.IO.File.Exists(imagePath)) throw new FileNotFoundException($"Image file not found: {imagePath}");
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
            var rect = new SixLabors.ImageSharp.Rectangle(
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
                var tile = new Tile(name, description, sheets[sheetIdx] ?? "sheet", base64Url, new Size(w, h));
                result.Add(tile.Id, tile);
            }
        }
        foreach (var img in sheetImages) img.Dispose();
        return result;
    }

    public async Task<Dictionary<int, Tile>> AddTilePaletteToArea(Area area, string jsonFilename, int cellWidth = 64, int cellHeight = 64)
    {
        Dictionary<int, Tile> tiles = new Dictionary<int, Tile>();
        var jsonPath = System.IO.Path.Combine("wwwroot", "tilesets", jsonFilename);
        if (System.IO.File.Exists(jsonPath))
        {
            tiles = ExtractTilesFromJsonTilesheet(jsonFilename);
        }
        else
        {
            // Fallback: treat as a single tile (should not happen for new workflow)
            var imagePath = System.IO.Path.Combine("wwwroot", "tilesets", jsonFilename);
            if (!System.IO.File.Exists(imagePath))
                throw new FileNotFoundException($"Image file not found: {imagePath}");
            using var image = Image.Load<Rgba32>(imagePath);
            using var ms = new MemoryStream();
            image.Save(ms, new PngEncoder());
            var base64 = $"data:image/png;base64,{Convert.ToBase64String(ms.ToArray())}";
            var tile = new Tile(jsonFilename, $"Imported from {jsonFilename}", "default", base64, new Size(cellWidth, cellHeight));
            tiles = new Dictionary<int, Tile> { { tile.Id, tile } };
        }
        foreach (var tile in tiles.Values)
        {
            if (area.TilePalette.ContainsKey(tile.Id))
            {
                // If tile already exists, skip it
                continue;
            }
            area.TilePalette[tile.Id] = tile;
        }
        await SaveAsync();
        MarkDirty();
        OnChanged?.Invoke(_definition);
        return tiles;
    }

    public void Dispose()
    {
        _db.Dispose();
    }
}
