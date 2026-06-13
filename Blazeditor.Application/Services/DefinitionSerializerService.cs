using Blazeditor.Application.Data;
using Microsoft.EntityFrameworkCore;

namespace Blazeditor.Application.Services;

/// <summary>
/// Loads and persists the <see cref="Models.Definition"/> graph using <see cref="BlazeditorDbContext"/>,
/// translating to/from the EF entity model via <see cref="DefinitionMapper"/>.
/// </summary>
public class DefinitionSerializerService(BlazeditorDbContext db)
{
    /// <summary>
    /// Loads the first persisted <see cref="Models.Definition"/> (with all related areas, tile palettes,
    /// tiles, tile maps and portals), or a new, empty, not-yet-persisted definition if none exists.
    /// </summary>
    public Models.Definition LoadDefinition()
    {
        var entity = db.Definitions
            .Include(d => d.Areas).ThenInclude(a => a.TileMaps)
            .Include(d => d.Areas).ThenInclude(a => a.TilePalettes)
            .Include(d => d.TilePalettes).ThenInclude(p => p.Tiles)
            .Include(d => d.Portals)
            .OrderBy(d => d.Id)
            .FirstOrDefault();

        return entity is null ? new Models.Definition() : DefinitionMapper.ToDomain(entity);
    }

    /// <summary>
    /// Replaces the persisted <see cref="Models.Definition"/> (and all its areas, tile palettes, tiles,
    /// tile maps and portals) with the contents of <paramref name="definition"/>.
    /// </summary>
    public async Task SaveDefinitionAsync(Models.Definition definition)
    {
        using var transaction = await db.Database.BeginTransactionAsync();

        var existing = await db.Definitions.FindAsync(definition.Id);
        if (existing is not null)
        {
            db.Definitions.Remove(existing);
            await db.SaveChangesAsync();
        }

        db.Definitions.Add(DefinitionMapper.ToEntity(definition));
        await db.SaveChangesAsync();

        await transaction.CommitAsync();
    }
}
