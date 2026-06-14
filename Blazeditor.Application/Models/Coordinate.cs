namespace Blazeditor.Application.Models;

public struct Coordinate(int x, int y, int layer = 0)
{
    public int X { get; set; } = x;
    public int Y { get; set; } = y;
    public int Layer { get; set; } = layer; // Default layer is 0
}

public struct Size(int width, int height)
{
    public int Width { get; set; } = width;
    public int Height { get; set; } = height;
}

public struct Layout(int x, int y, int level, int width, int height)
{
    public Coordinate Location { get; set; } = new Coordinate(x, y, level);
    public Size Size { get; set; } = new Size(width, height);
}

/// <summary>
/// All map/tile placement and <see cref="Tile.Size"/> values are expressed in units of this
/// fixed grid cell size (in pixels), regardless of the resolution of the source tile images.
/// </summary>
public static class GridConstants
{
    public const int CellSize = 32;
}