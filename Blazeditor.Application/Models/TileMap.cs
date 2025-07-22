using LiteDB;

namespace Blazeditor.Application.Models;

public class TilePlacement
{
    public int X { get; set; }
    public int Y { get; set; }
    public int? TileId { get; set; } // null means empty cell
}

public class TileMap : BaseEntity
{
    private Size _size;
    public TileMap() : base() { }
    public TileMap(string name, string description, int level, Size size) : base(name, description)
    {
        Level = level;
        _size = size;
        TilePlacements = new Dictionary<int, TilePlacement>(size.Width * size.Height);
    }
    public int Level { get; set; }
    // Store tile placements for persistence
    public Dictionary<int, TilePlacement> TilePlacements { get; set; } = new();

    public Size Size => _size;

    private int GetKey(int x, int y) => x + y * _size.Width;

    public void Resize(Size newSize)
    {
        if (newSize.Width <= 0 || newSize.Height <= 0)
            throw new ArgumentException("Size must be greater than zero.");
        _size = newSize;
        // Remove placements outside new bounds
        var keysToRemove = TilePlacements.Keys.Where(k =>
            {
                int x = k % newSize.Width;
                int y = k / newSize.Width;
                return x >= newSize.Width || y >= newSize.Height;
            }).ToList();
        foreach (var key in keysToRemove)
            TilePlacements.Remove(key);
    }

    // Get tile placement at (x, y)
    public TilePlacement? GetPlacement(int x, int y)
    {
        TilePlacements.TryGetValue(GetKey(x, y), out var placement);
        return placement;
    }

    // Set or update tile placement at (x, y)
    public void SetPlacement(int x, int y, int? tileId)
    {
        int key = GetKey(x, y);
        if (TilePlacements.TryGetValue(key, out var placement))
        {
            placement.TileId = tileId;
        }
        else if (tileId.HasValue)
        {
            TilePlacements[key] = new TilePlacement { X = x, Y = y, TileId = tileId };
        }
        else
        {
            // Remove placement if setting to null
            TilePlacements.Remove(key);
        }
    }
}