using System.Text.Json.Serialization;

namespace Blazeditor.Application.Models;

public class TilePalette : BaseEntity
{
    public TilePalette() { }
    public TilePalette(string name, string description, int cellSize = 64) : base(name, description)
    {
        CellSize = cellSize;
    }
    public Dictionary<Guid, Tile> Tiles { get; set; } = [];
    public int CellSize { get; set; } = 64; // Cell size in pixels for this palette

    [JsonIgnore]
    public bool IsValid => Tiles.Values.Any(t => t.Role == TileRole.Floor) && Tiles.Values.Any(t => t.Role == TileRole.Shim);
}
