namespace Blazeditor.Application.Models;

public class TilePalette : BaseEntity
{
    public TilePalette() { }
    public TilePalette(string name, string description, int cellSize = 64) : base(name, description)
    {
        CellSize = cellSize;
    }
    public Dictionary<int, Tile> Tiles { get; set; } = [];
    public int CellSize { get; set; } = 64; // Cell size in pixels for this palette
}
