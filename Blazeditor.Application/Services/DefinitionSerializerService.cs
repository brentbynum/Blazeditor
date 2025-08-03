using Blazeditor.Application.Models;
using LiteDB;

namespace Blazeditor.Application.Services;

public class DefinitionSerializerService
{
    private readonly LiteDatabase _database;
    public DefinitionSerializerService(IHttpContextAccessor httpContextAccessor) {
        var user = httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "guest";
        _database = new LiteDatabase($"Filename={user}.db; Connection=shared");
    }
    public void SaveArea(Area area)
    {
        var col = _database.GetCollection<Area>("areas");
        col.Upsert(area.Id, area);
    }
    public Area LoadArea(int areaId)
    {
        var col = _database.GetCollection<Area>("areas");
        return col.FindById(areaId) ?? throw new KeyNotFoundException($"Area with ID {areaId} not found.");
    }
    public void SaveDefinition(Definition definition)
    {
        // Save all areas
        var areaCol = _database.GetCollection<Area>("areas");
        areaCol.DeleteAll(); // Clear existing areas
        foreach (var area in definition.Areas.Values)
            areaCol.Upsert(area.Id, area);
        // Save all tile palettes
        var paletteCol = _database.GetCollection<TilePalette>("tilepalettes");
        paletteCol.DeleteAll();
        foreach (var palette in definition.TilePalettes.Values)
            paletteCol.Upsert(palette.Id, palette);
        // Save all portals
        var portalCol = _database.GetCollection<Portal>("portals");
        portalCol.DeleteAll();
        foreach (var portal in definition.Portals)
            portalCol.Upsert(portal.Id, portal);
        _database.Checkpoint(); // Ensure changes are saved to disk
    }
    public Definition LoadDefinition()
    {
        var def = new Definition();
        var areaCol = _database.GetCollection<Area>("areas");
        foreach (var area in areaCol.FindAll())
            def.Areas[area.Id] = area;
        
        var paletteCol = _database.GetCollection<TilePalette>("tilepalettes");
        foreach (var palette in paletteCol.FindAll())
            def.TilePalettes[palette.Id] = palette;
        var portalCol = _database.GetCollection<Portal>("portals");
        foreach (var portal in portalCol.FindAll())
            def.Portals.Add(portal);
        return def;
    }
    public void SaveTilePalette(TilePalette palette)
    {
        var col = _database.GetCollection<TilePalette>("tilepalettes");
        col.Upsert(palette.Id, palette);
    }
    public TilePalette LoadTilePalette(int paletteId)
    {
        var col = _database.GetCollection<TilePalette>("tilepalettes");
        return col.FindById(paletteId) ?? throw new KeyNotFoundException($"TilePalette with ID {paletteId} not found.");
    }
}
