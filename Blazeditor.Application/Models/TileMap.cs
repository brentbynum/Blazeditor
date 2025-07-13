using LiteDB;

namespace Blazeditor.Application.Models
{
    public class TileMap : BaseEntity
    {
        private Size _size;
        public TileMap() : base() { }
        public TileMap(string name, string description, int level, Size size) : base(name, description)
        {
            Level = level;
            _size = size;
            TileNames = new string?[size.Width * size.Height]; // Will be resized when Area.Size is set
            Tiles = new Tile?[size.Width * size.Height]; // Will be resized when Area.Size is set
        }
        public int Level { get; set; }
        // Removed Size and TileSize from TileMap; now use Area.Size and Area.TileSize
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
                if (x < 0 || y < 0 || Tiles == null || x >= _size.Width || y * _size.Width + x >= Tiles.Length)
                    return null;
                return Tiles[y * _size.Width + x];
            }
            set
            {
                if (x < 0 || y < 0 || Tiles == null || x >= _size.Width || y * _size.Width + x >= Tiles.Length)
                    return;
                Tiles[y * _size.Width + x] = value;
            }
        }
        public void Resize(Size newSize)
        {
            if (newSize.Width <= 0 || newSize.Height <= 0)
                throw new ArgumentException("Size must be greater than zero.");
            _size = newSize;
            TileNames = new string?[newSize.Width * newSize.Height];
            Tiles = new Tile?[newSize.Width * newSize.Height];
        }
        // Call this after deserialization to rebuild Tiles from palette
        public void RebuildTiles(Dictionary<int, Tile> palette)
        {
            // The consuming code should resize Tiles based on Area.Size
            Tiles = new Tile?[TileNames.Length];
            for (int i = 0; i < TileNames.Length; i++)
            {
                if (TileNames[i] != null)
                {
                    Tiles[i] = palette.Values.FirstOrDefault(t => t.Name == TileNames[i]);
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