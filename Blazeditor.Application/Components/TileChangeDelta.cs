using Blazeditor.Application.Models;
using System.Collections.Generic;

public class TileChangeDelta : IDefinitionDelta
{
    public int AreaId { get; set; }
    public int TileMapLevel { get; set; }
    public List<TileCellChange> Changes { get; set; } = new();

    public void Apply(Definition definition)
    {
        if (!definition.Areas.TryGetValue(AreaId, out var area)) return;
        if (!area.TileMaps.TryGetValue(TileMapLevel, out var tileMap)) return;
        for (int i = 0; i < Changes.Count; i++)
        {
            var change = Changes[i];
            var tile = change.NewTileId.HasValue ? area.TilePalette[change.NewTileId.Value] : null;
            tileMap[change.X, change.Y] = tile;
        }
    }
    public void Revert(Definition definition)
    {
        if (!definition.Areas.TryGetValue(AreaId, out var area)) return;
        if (!area.TileMaps.TryGetValue(TileMapLevel, out var tileMap)) return;
        for (int i = 0; i < Changes.Count; i++)
        {
            var change = Changes[i];
            var tile = change.OldTileId.HasValue ? area.TilePalette[change.OldTileId.Value] : null;
            tileMap[change.X, change.Y] = tile;
        }
    }
}

public class TileCellChange
{
    public int X { get; set; }
    public int Y { get; set; }
    public int? OldTileId { get; set; }
    public int? NewTileId { get; set; }
}