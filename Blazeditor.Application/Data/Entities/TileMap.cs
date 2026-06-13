using Blazeditor.Application.Models;

namespace Blazeditor.Application.Data.Entities;

/// <summary>
/// Persistence-model counterpart of <see cref="Blazeditor.Application.Models.TileMap"/>.
/// </summary>
public class TileMap : BaseEntity
{
    public Guid AreaId { get; set; }
    public Area Area { get; set; } = null!;

    public int Layer { get; set; }
    public Size Size { get; set; }

    /// <summary>
    /// Per-cell tile placements (32x32 grid), stored as a jsonb column. Tile/palette ids referenced
    /// here are not FK-enforced.
    /// </summary>
    public TilePlacement?[] TilePlacements { get; set; } = [];
}
