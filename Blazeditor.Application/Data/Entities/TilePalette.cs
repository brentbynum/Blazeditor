using Blazeditor.Application.Models;

namespace Blazeditor.Application.Data.Entities;

/// <summary>
/// Persistence-model counterpart of <see cref="Blazeditor.Application.Models.TilePalette"/>.
/// </summary>
public class TilePalette : BaseEntity
{
    public Guid DefinitionId { get; set; }
    public Definition Definition { get; set; } = null!;

    public int CellSize { get; set; } = 64;

    public List<Tile> Tiles { get; set; } = new();

    // Many-to-many inverse of Area.TilePalettes
    public List<Area> Areas { get; set; } = new();
}
