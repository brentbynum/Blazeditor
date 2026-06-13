using Blazeditor.Application.Models;
using EntityModels = Blazeditor.Application.Data.Entities;

namespace Blazeditor.Application.Data;

/// <summary>
/// Translates between the in-memory, dictionary-shaped <see cref="Blazeditor.Application.Models"/> graph
/// used by the editor UI and the list-shaped <see cref="EntityModels"/> graph persisted via EF Core.
/// </summary>
public static class DefinitionMapper
{
    public static EntityModels.Definition ToEntity(Definition definition)
    {
        var entity = new EntityModels.Definition
        {
            Id = definition.Id,
            Name = definition.Name,
            Description = definition.Description,
        };

        entity.TilePalettes = definition.TilePalettes.Values.Select(p => ToEntity(p, entity)).ToList();
        entity.Areas = definition.Areas.Values.Select(a => ToEntity(a, entity, entity.TilePalettes)).ToList();
        entity.Portals = definition.Portals.Select(p => ToEntity(p, entity)).ToList();

        return entity;
    }

    private static EntityModels.TilePalette ToEntity(TilePalette palette, EntityModels.Definition definition)
    {
        var entity = new EntityModels.TilePalette
        {
            Id = palette.Id,
            Name = palette.Name,
            Description = palette.Description,
            DefinitionId = definition.Id,
            Definition = definition,
            CellSize = palette.CellSize,
        };

        entity.Tiles = palette.Tiles.Values.Select(t => ToEntity(t, entity)).ToList();
        return entity;
    }

    private static EntityModels.Tile ToEntity(Tile tile, EntityModels.TilePalette palette)
    {
        return new EntityModels.Tile
        {
            Id = tile.Id,
            Name = tile.Name,
            Description = tile.Description,
            TilePaletteId = palette.Id,
            TilePalette = palette,
            Role = tile.Role,
            Type = tile.Type,
            Image = tile.Image,
            Size = tile.Size,
            FloorProperties = tile.FloorProperties,
            ShimProperties = tile.ShimProperties,
        };
    }

    private static EntityModels.Area ToEntity(Area area, EntityModels.Definition definition, List<EntityModels.TilePalette> palettes)
    {
        var entity = new EntityModels.Area
        {
            Id = area.Id,
            Name = area.Name,
            Description = area.Description,
            DefinitionId = definition.Id,
            Definition = definition,
            Size = area.Size,
            CellSize = area.CellSize,
        };

        entity.TileMaps = area.TileMaps.Values.Select(tm => ToEntity(tm, entity)).ToList();
        entity.TilePalettes = palettes.Where(p => area.TilePaletteIds.Contains(p.Id)).ToList();
        return entity;
    }

    private static EntityModels.TileMap ToEntity(TileMap tileMap, EntityModels.Area area)
    {
        return new EntityModels.TileMap
        {
            Id = tileMap.Id,
            Name = tileMap.Name,
            Description = tileMap.Description,
            AreaId = area.Id,
            Area = area,
            Layer = tileMap.Layer,
            Size = tileMap.Size,
            TilePlacements = tileMap.TilePlacements,
        };
    }

    private static EntityModels.Portal ToEntity(Portal portal, EntityModels.Definition definition)
    {
        return new EntityModels.Portal
        {
            Id = portal.Id,
            Name = portal.Name,
            Description = portal.Description,
            DefinitionId = definition.Id,
            Definition = definition,
            DestinationAreaId = portal.DestinationAreaId,
            LocationAreaId = portal.LocationAreaId,
            Destination = portal.Destination,
            Location = portal.Location,
        };
    }

    public static Definition ToDomain(EntityModels.Definition entity)
    {
        var definition = new Definition
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
        };

        foreach (var palette in entity.TilePalettes)
        {
            definition.TilePalettes[palette.Id] = ToDomain(palette);
        }

        foreach (var area in entity.Areas)
        {
            definition.Areas[area.Id] = ToDomain(area);
        }

        definition.Portals = entity.Portals.Select(ToDomain).ToList();
        return definition;
    }

    private static TilePalette ToDomain(EntityModels.TilePalette entity)
    {
        var palette = new TilePalette(entity.Name, entity.Description ?? string.Empty, entity.CellSize)
        {
            Id = entity.Id,
        };

        foreach (var tile in entity.Tiles)
        {
            palette.Tiles[tile.Id] = ToDomain(tile);
        }

        return palette;
    }

    private static Tile ToDomain(EntityModels.Tile entity)
    {
        return new Tile(entity.Name, entity.Description ?? string.Empty, entity.Type, entity.Image, entity.Size, entity.TilePaletteId)
        {
            Id = entity.Id,
            Role = entity.Role,
            FloorProperties = entity.FloorProperties,
            ShimProperties = entity.ShimProperties,
        };
    }

    private static Area ToDomain(EntityModels.Area entity)
    {
        var area = new Area(entity.Name, entity.Description ?? string.Empty, entity.Size, entity.CellSize)
        {
            Id = entity.Id,
            TilePaletteIds = entity.TilePalettes.Select(p => p.Id).ToList(),
        };

        foreach (var tileMap in entity.TileMaps)
        {
            area.TileMaps[tileMap.Layer] = ToDomain(tileMap);
        }

        return area;
    }

    private static TileMap ToDomain(EntityModels.TileMap entity)
    {
        return new TileMap(entity.Name, entity.Description ?? string.Empty, entity.Layer, entity.Size)
        {
            Id = entity.Id,
            TilePlacements = entity.TilePlacements,
        };
    }

    private static Portal ToDomain(EntityModels.Portal entity)
    {
        return new Portal
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            DestinationAreaId = entity.DestinationAreaId,
            LocationAreaId = entity.LocationAreaId,
            Destination = entity.Destination,
            Location = entity.Location,
        };
    }
}
