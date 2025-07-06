namespace Blazeditor.Application.Models
{
    public class Definition
    {
        public Dictionary<int, Area> Areas { get; } = [];
        public List<Portal> Portals { get; } = [];
        public Dictionary<int, Tile> Palette { get; } = [];
    }
}