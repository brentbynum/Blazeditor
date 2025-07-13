namespace Blazeditor.Application.Models
{
    public class Area : BaseEntity
    {
        public Area() : base() { }
        public Area(string name, string description, Size? size, int? cellSize) : base(name, description)
        {
            Size = size ?? new Size(1, 1);
            CellSize = cellSize ?? 64;
        }
        public Dictionary<int, Tile> TilePalette { get; set; } = new();
        public Dictionary<int, TileMap> TileMaps { get; set; } = new();
        private Size _size = new(1, 1); // Default size
        public Size Size
        {
            
            get => _size;
            set
            {
                _size = value;
                // Resize TileMaps if necessary
                foreach (var map in TileMaps.Values)
                {
                    map.Resize(value);
                }
            }

        }


        public int CellSize { get; set; } = 64; // Cell size in pixels
    }
}