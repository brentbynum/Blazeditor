using LiteDB;
using System.Text.Json.Serialization;

namespace Blazeditor.Application.Models
{
    public class TilePlacement
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int? TileId { get; set; } // null means empty cell
    }

    public class TileMap : BaseEntity
    {
        private Size _size;
        public TileMap() : base() { }
        public TileMap(string name, string description, int level, Size size) : base(name, description)
        {
            Level = level;
            _size = size;
            TilePlacements = new List<TilePlacement>(size.Width * size.Height);
            Tiles = new Tile?[size.Width * size.Height];
        }
        public int Level { get; set; }
        // Store tile placements for persistence
        public List<TilePlacement> TilePlacements { get; set; } = new List<TilePlacement>();

        private Tile?[] _tiles = Array.Empty<Tile?>();
        [BsonIgnore]
        public Tile?[] Tiles
        {
            get { return _tiles; }
            set { _tiles = value; }
        }
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
            Tiles = new Tile?[newSize.Width * newSize.Height];
            // Optionally: repopulate TilePlacements from Tiles if needed
        }
        // Call this before serialization to update TilePlacements from Tiles
        public void UpdateTilePlacements()
        {
            TilePlacements.Clear();
            for (int y = 0; y < _size.Height; y++)
            {
                for (int x = 0; x < _size.Width; x++)
                {
                    var tile = this[x, y];
                    if (tile != null)
                    {
                        TilePlacements.Add(new TilePlacement
                        {
                            X = x,
                            Y = y,
                            TileId = tile?.Id
                        });
                    }
                }
            }
            Console.WriteLine($"TilePlacements updated: {TilePlacements.Count} placements for map '{Name}' at level {Level}");
        }
        // Call this after deserialization to rebuild Tiles from TilePlacements
        public void RebuildTiles(Dictionary<int, Tile> palette)
        {
            Tiles = new Tile?[_size.Width * _size.Height];
            foreach (var placement in TilePlacements)
            {
                if (placement.TileId.HasValue && palette.TryGetValue(placement.TileId.Value, out var tile))
                {
                    this[placement.X, placement.Y] = tile;
                }
                else
                {
                    this[placement.X, placement.Y] = null;
                }
            }
        }
    }
}