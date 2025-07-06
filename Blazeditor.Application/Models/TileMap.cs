namespace Blazeditor.Application.Models
{
    public class TileMap(int width, int height, int level) : BaseEntity
    {
        public int Level { get; set; } = level;
        public Size Size {  get; set; } = new Size(width, height);
        public Size TileSize { get; set; } = new Size(64, 64);
        public Tile[] Tiles { get; set; } = new Tile[width * height];
        public Tile this[int x, int y]
        {
            get => Tiles[y * Size.Width + x];
            set => Tiles[y * Size.Width + x] = value;
        }
    }
}