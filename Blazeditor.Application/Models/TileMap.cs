using LiteDB;

namespace Blazeditor.Application.Models
{
    public class TileMap : BaseEntity
    {
        public TileMap() : base() { }
        public TileMap(string name, string description, int width, int height, int level) : base(name, description)
        {
            Level = level;
            Size = new Size(width, height);
            TileSize = new Size(64, 64);
            TileNames = new string?[width * height];
        }
        public int Level { get; set; }
        public Size Size { get; set; } = new Size(1, 1);
        public Size TileSize { get; set; } = new Size(64, 64);
        // Store only tile names for persistence
        public string?[] TileNames { get; set; } = new string?[1];
        // Ignore Tiles array for persistence
        [BsonIgnore]
        public Tile?[] Tiles { get; set; } = new Tile?[1];
        [BsonIgnore]
        public Tile? this[int x, int y]
        {
            get
            {
                if (x < 0 || y < 0 || x >= Size.Width || y >= Size.Height)
                    return null;
                return Tiles[y * Size.Width + x];
            }
            set
            {
                if (x < 0 || y < 0 || x >= Size.Width || y >= Size.Height)
                    return;
                Tiles[y * Size.Width + x] = value;
            }
        }
        // Call this after deserialization to rebuild Tiles from palette
        public void RebuildTiles(List<Tile> palette)
        {
            Tiles = new Tile?[Size.Width * Size.Height];
            for (int i = 0; i < TileNames.Length; i++)
            {
                if (TileNames[i] != null)
                {
                    Tiles[i] = palette.FirstOrDefault(t => t.Name == TileNames[i]);
                }
                else
                {
                    Tiles[i] = null;
                }
            }
        }
        // Call this before serialization to update TileNames from Tiles
        public void UpdateTileNames()
        {
            TileNames = Tiles.Select(t => t?.Name).ToArray();
        }
    }
}