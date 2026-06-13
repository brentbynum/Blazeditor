using Blazeditor.Application.Models;

namespace Blazeditor.Application.Data.Entities;

/// <summary>
/// Persistence-model counterpart of <see cref="Blazeditor.Application.Models.Area"/>.
/// </summary>
public class Area : BaseEntity
{
    public Guid DefinitionId { get; set; }
    public Definition Definition { get; set; } = null!;

    public Size Size { get; set; } = new(1, 1);
    public int CellSize { get; set; } = 64;

    public List<TileMap> TileMaps { get; set; } = new();

    // Many-to-many with TilePalette (replaces Area.TilePaletteIds)
    public List<TilePalette> TilePalettes { get; set; } = new();
}
