namespace Blazeditor.Application.Models
{
    public class Area : BaseEntity
    {
        public  List<Tile> TilePalette { get; set; } = [];
        public Dictionary<int, TileMap> TileMaps { get; set; } = [];
    }
}