using Blazeditor.Application.Models;

namespace Blazeditor.Application.Data.Entities;

/// <summary>
/// Persistence-model counterpart of <see cref="Blazeditor.Application.Models.Definition"/>.
/// Root entity owning the areas, tile palettes and portals that make up a single definition.
/// </summary>
public class Definition : BaseEntity
{
    public List<Area> Areas { get; set; } = new();
    public List<TilePalette> TilePalettes { get; set; } = new();
    public List<Portal> Portals { get; set; } = new();
}
