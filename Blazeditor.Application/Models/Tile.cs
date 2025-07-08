namespace Blazeditor.Application.Models
{
    public class Tile(string name, string description, string type, string imageBase64, Size size) : BaseEntity(name, description)
    {
        public string Type { get; set; } = type;
        public string Image { get; set; } = imageBase64;
        public Size Size { get; set; } = size;

        // represents the tile's layout and state in the palette
        public TileState PaletteState { get; set; }
    }

    public struct TileState
    {
        public Layout Layout { get; set; }
        public bool IsMouseOver { get; set; }
    }
}