using LiteDB;

namespace Blazeditor.Application.Models;

public class Tile : BaseEntity
{
    public Tile() : base() { }
    public Tile(string name, string description, string type, string imageBase64, Size size) : base(name, description)
    {
        Type = type;
        Image = imageBase64;
        Size = size;
    }
    public string Type { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public Size Size { get; set; } = new Size(1, 1);
    public int Elevation { get; set; } = 0; // Elevation property for vertical offset
    [BsonIgnore]
    public TileState PaletteState { get; set; }
   
}

public struct TileState
{
    public Layout Layout { get; set; }
    public bool IsMouseOver { get; set; }
}