using Blazeditor.Application.Models;

namespace Blazeditor.Application.Data.Entities;

/// <summary>
/// Persistence-model counterpart of <see cref="Blazeditor.Application.Models.Portal"/>.
/// </summary>
public class Portal : BaseEntity
{
    public Guid DefinitionId { get; set; }
    public Definition Definition { get; set; } = null!;

    /// <summary>References an <see cref="Area"/>; set to null (rather than cascade-deleted) if the area is removed.</summary>
    public Guid? DestinationAreaId { get; set; }

    /// <summary>References an <see cref="Area"/>; set to null (rather than cascade-deleted) if the area is removed.</summary>
    public Guid? LocationAreaId { get; set; }

    public Coordinate Destination { get; set; }
    public Coordinate Location { get; set; }
}
