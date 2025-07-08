namespace Blazeditor.Application.Models
{
    public struct Coordinate(int x, int y)
    {
        public int X { get; set; } = x;
        public int Y { get; set; } = y;
    }

    public struct Size(int width, int height)
    {
        public int Width { get; set; } = width;
        public int Height { get; set; } = height;
    }

    public struct Layout(int x, int y, int width, int height)
    {
        public Coordinate Location { get; set; } = new Coordinate(x, y);
        public Size Size { get; set; } = new Size(width, height);
    }
}