namespace Blazeditor.Application.Models;

public class Definition
{
    public Definition() { }
    public Dictionary<int, Area> Areas { get; set; } = new();
    public List<Portal> Portals { get; set; } = new();
    public Dictionary<int, TilePalette> TilePalettes { get; set; } = new();
}