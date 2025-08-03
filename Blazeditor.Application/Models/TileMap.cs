using LiteDB;

namespace Blazeditor.Application.Models;

public class TilePlacement
{
    public int X { get; set; }
    public int Y { get; set; }

    public int Elevation { get; set; } = 0; // Elevation property for vertical offset
    public int? TileId { get; set; } // null means empty cell
    public int? PaletteId { get; set; } // null means no specific palette, shouldn't happen in a tile map placement because you need a palette to make a plcament
}

public class TileMap : BaseEntity
{
    private Size _size32; // Size in 32x32 grid units
    public TileMap() : base() { }
    public TileMap(string name, string description, int layer, Size size) : base(name, description)
    {
        Layer = layer;
        // Convert pixel size to 32x32 grid units
        _size32 = size;
        TilePlacements = new TilePlacement?[_size32.Width * _size32.Height];
    }
    public int Layer { get; set; }
    // Store tile placements for persistence (32x32 grid)
    public TilePlacement?[] TilePlacements { get; set; } = [];


    // Size in 32x32 grid units
    public Size Size => _size32;

    private int GetKey(int x, int y) => x + y * _size32.Width;

    public void Resize(Size newSize32)
    {
        var newArray = new TilePlacement?[newSize32.Width * newSize32.Height];
        _size32 = newSize32;
        for (int y = 0; y < newSize32.Height; y++)
        {
            for (int x = 0; x < newSize32.Width; x++)
            {
                int oldIdx = GetKey(x, y);
                int newIdx = x + y * newSize32.Width;
                newArray[newIdx] = oldIdx < TilePlacements.Length ? TilePlacements[oldIdx] : null;
            }
        }
        TilePlacements = newArray;
        
    }

    // Get tile placement at (x, y) in 32x32 grid units
    public TilePlacement? GetPlacement(int x, int y)
    {
        int idx = GetKey(x, y);
        if (idx < 0 || idx >= TilePlacements.Length) return null;
        return TilePlacements[idx];
    }

    // Set or update tile placement at (x, y) in 32x32 grid units
    public void SetPlacement(int x, int y, int? tileId, int? paletteId)
    {
        int idx = GetKey(x, y);
        if (idx < 0 || idx >= TilePlacements.Length) return;
        if (tileId.HasValue)
        {
            TilePlacements[idx] = new TilePlacement { X = x, Y = y, TileId = tileId, PaletteId = paletteId };
        }
        else
        {
            TilePlacements[idx] = null;
        }
    }
}