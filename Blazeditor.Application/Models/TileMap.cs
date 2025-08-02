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
    private Size _size32; // Size in 32x32 grid units
    public TileMap() : base() { }
    public TileMap(string name, string description, int level, Size sizeInPixels) : base(name, description)
    {
        Level = level;
        // Convert pixel size to 32x32 grid units
        _size32 = new Size(sizeInPixels.Width / 32, sizeInPixels.Height / 32);
        TilePlacements = new TilePlacement?[_size32.Width * _size32.Height];
    }
    public int Level { get; set; }
    // Store tile placements for persistence (32x32 grid)
    public TilePlacement?[] TilePlacements { get; set; } = Array.Empty<TilePlacement?>();

    // Size in 32x32 grid units
    public Size Size => _size32;

    private int GetKey(int x, int y) => x + y * _size32.Width;

    public void Resize(Size newSizeInPixels)
    {
        if (newSizeInPixels.Width <= 0 || newSizeInPixels.Height <= 0)
            throw new ArgumentException("Size must be greater than zero.");
        var newSize32 = new Size(newSizeInPixels.Width / 32, newSizeInPixels.Height / 32);
        var newArray = new TilePlacement?[newSize32.Width * newSize32.Height];
        for (int y = 0; y < Math.Min(_size32.Height, newSize32.Height); y++)
        {
            for (int x = 0; x < Math.Min(_size32.Width, newSize32.Width); x++)
            {
                int oldIdx = GetKey(x, y);
                int newIdx = x + y * newSize32.Width;
                newArray[newIdx] = TilePlacements[oldIdx];
            }
        }
        TilePlacements = newArray;
        _size32 = newSize32;
    }

    // Get tile placement at (x, y) in 32x32 grid units
    public TilePlacement? GetPlacement(int x, int y)
    {
        int idx = GetKey(x, y);
        if (idx < 0 || idx >= TilePlacements.Length) return null;
        return TilePlacements[idx];
    }

    // Set or update tile placement at (x, y) in 32x32 grid units
    public void SetPlacement(int x, int y, int? tileId)
    {
        int idx = GetKey(x, y);
        if (idx < 0 || idx >= TilePlacements.Length) return;
        if (tileId.HasValue)
        {
            TilePlacements[idx] = new TilePlacement { X = x, Y = y, TileId = tileId };
        }
        else
        {
            TilePlacements[idx] = null;
        }
    }
}