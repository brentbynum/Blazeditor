namespace Blazeditor.Application.Models
{
    public class Tile(string type, string imageBase64, Size size)
    {
        public string Type { get; set; } = type;
        public string Image { get; set; } = imageBase64;
        public Size Size { get; set; } = size;
    }
}