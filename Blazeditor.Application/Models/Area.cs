namespace Blazeditor.Application.Models
{
    public class Area : BaseEntity
    {
        public Area() : base() { }
        public Area(string name, string description) : base(name, description) { }
        public List<Tile> TilePalette { get; set; } = new();
        public Dictionary<int, TileMap> TileMaps { get; set; } = new();
    }
}