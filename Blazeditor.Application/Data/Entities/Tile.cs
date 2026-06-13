using Blazeditor.Application.Models;

namespace Blazeditor.Application.Data.Entities;

/// <summary>
/// Persistence-model counterpart of <see cref="Blazeditor.Application.Models.Tile"/>.
/// </summary>
public class Tile : BaseEntity
{
    public Guid TilePaletteId { get; set; }
    public TilePalette TilePalette { get; set; } = null!;

    public TileRole Role { get; set; } = TileRole.None;
    public string Type { get; set; } = string.Empty;

    /// <summary>Relative path to the tile's image file (see file-based asset storage plan).</summary>
    public string Image { get; set; } = string.Empty;

    public Size Size { get; set; } = new(1, 1);

    // Meaningful only when Role == TileRole.Floor; null otherwise
    public FloorProperties? FloorProperties { get; set; }

    // Meaningful only when Role == TileRole.Shim; null otherwise
    public ShimProperties? ShimProperties { get; set; }
}
