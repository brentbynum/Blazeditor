namespace Blazeditor.Application.Models;

public class Definition : BaseEntity
{
    public Definition() : base() { }
    public Dictionary<Guid, Area> Areas { get; set; } = new();
    public List<Portal> Portals { get; set; } = new();
    public Dictionary<Guid, TilePalette> TilePalettes { get; set; } = new();
}