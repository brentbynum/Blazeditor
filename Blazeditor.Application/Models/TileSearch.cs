namespace Blazeditor.Application.Models;

/// <summary>
/// Shared tile search/filter logic used by tile-list UIs (e.g. <c>TilePaletteEditor</c>,
/// <c>AreaEditor</c>). Matches on tile name or description, sorted so that
/// name:startsWith ranks above description:startsWith, above name:contains, above
/// description:contains.
/// </summary>
public static class TileSearch
{
    public static IEnumerable<Tile> Filter(IEnumerable<Tile> tiles, string? search)
    {
        search = search?.Trim() ?? string.Empty;
        if (search.Length == 0) return tiles.OrderBy(t => t.Name);

        return tiles
            .Where(t => Matches(t, search))
            .OrderBy(t => GetMatchRank(t, search))
            .ThenBy(t => t.Name);
    }

    private static bool Matches(Tile tile, string search)
        => Contains(tile.Name, search) || Contains(tile.Description, search);

    private static bool Contains(string? value, string search)
        => !string.IsNullOrEmpty(value) && value.Contains(search, StringComparison.OrdinalIgnoreCase);

    private static bool StartsWith(string? value, string search)
        => !string.IsNullOrEmpty(value) && value.StartsWith(search, StringComparison.OrdinalIgnoreCase);

    // Lower rank sorts first: name starts-with, description starts-with, name contains, description contains.
    private static int GetMatchRank(Tile tile, string search)
    {
        if (StartsWith(tile.Name, search)) return 0;
        if (StartsWith(tile.Description, search)) return 1;
        if (Contains(tile.Name, search)) return 2;
        return 3;
    }
}
