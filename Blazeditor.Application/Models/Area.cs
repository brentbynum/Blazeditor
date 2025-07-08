namespace Blazeditor.Application.Models
{
    public class Area(string name, string description) : BaseEntity(name, description)
    {
        public  List<Tile> TilePalette { get; set; } = [];
        public Dictionary<int, TileMap> TileMaps { get; set; } = [];
    }
}