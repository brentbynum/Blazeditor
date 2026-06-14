using Blazeditor.Application.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using System.Text.Json;

namespace Blazeditor.Application.Components.Dialogs;

public partial class TileMapCanvas : IDisposable
{
    private const int MaxTileElevation = 6; // Maximum elevation for tiles
    private const int MinTileElevation = 0;
    [Parameter] public required Area Area { get; set; } = new();
    [Parameter] public Tile? SelectedTile { get; set; }

    [Parameter] public int ActiveLayer { get; set; }
    private ElementReference canvasRef;
    private DotNetObjectReference<TileMapCanvas>? dotNetRef;
    private string selectedTool = "paint"; // Default tool
    private List<Coordinate> SelectedCells { get; set; } = new();

    private bool _shouldInitJs = false;
    private bool _shouldInitMap = true; // Flag to re-initialize map
    private bool _shouldReloadArea = false; // Flag to load a different area's data into the canvas
    private Guid? _loadedAreaId;

    protected override void OnParametersSet()
    {
        // Set a flag to re-initialize JS after parameters change
        _shouldInitJs = true;

        if (_loadedAreaId.HasValue && _loadedAreaId.Value != Area.Id)
        {
            _shouldReloadArea = true;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (JS != null)
        {
            if (firstRender)
            {

                dotNetRef = DotNetObjectReference.Create(this);
                await JS.InvokeVoidAsync("tileMapCanvas.setDotNetRef", dotNetRef);
            }
            if (_shouldInitMap)
            {
                // TODO: Where should I get the palette tiles from?
                var paletteTiles = Definition.GetTilePalettes().SelectMany(p => p.Tiles).ToDictionary();
                // The map only ever gets init-ed once, and the tilemaps get synched manually on add/remove
                await JS.InvokeVoidAsync("tileMapCanvas.init", canvasRef, Area.TileMaps, Area.CellSize, Area.Size, paletteTiles);
                _shouldInitMap = false; // Reset flag after initialization
                _loadedAreaId = Area.Id;
            }
            else if (_shouldReloadArea)
            {
                var paletteTiles = Definition.GetTilePalettes().SelectMany(p => p.Tiles).ToDictionary();
                await JS.InvokeVoidAsync("tileMapCanvas.loadArea", Area.TileMaps, Area.CellSize, Area.Size, paletteTiles);
                SelectedCells = new();
                _loadedAreaId = Area.Id;
                _shouldReloadArea = false;
            }
            if (_shouldInitJs)
            {
                await JS.InvokeVoidAsync("tileMapCanvas.setSelectedTileId", SelectedTile?.Id);
                await JS.InvokeVoidAsync("tileMapCanvas.setActiveLayer", ActiveLayer);
                _shouldInitJs = false;
            }
        }
    }

    public async Task ClearTileMapsAsync()
    {
        if (Area?.TileMaps != null && Area.TileMaps.Count > 0)
        {
            // Use 32x32 grid units for width/height
            int width = Area.Size.Width;
            int height = Area.Size.Height;
            if (SelectedCells != null && SelectedCells.Count > 0)
            {
                foreach (var cell in SelectedCells)
                {
                    if (Area.TileMaps.TryGetValue(cell.Layer, out var map))
                    {
                        if (cell.X >= 0 && cell.X < width && cell.Y >= 0 && cell.Y < height)
                        {
                            map.SetPlacement(cell.X, cell.Y, null, null, 0);
                        }
                    }
                }
            }
            else
            {
                if (Area.TileMaps.TryGetValue(ActiveLayer, out var tileMap))
                {
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            tileMap.SetPlacement(x, y, null, null, 0);
                        }
                    }
                }
            }

            if (JS != null)
            {
                await JS.InvokeVoidAsync("tileMapCanvas.updateTileMaps", Area.TileMaps);
            }
        }
    }
    public async Task UpdateTilePalette()
    {
        if (Area.TilePaletteIds.Count > 0)
        {
            var paletteTiles = Area.TilePaletteIds.SelectMany(id => Definition.GetPalette(id).Tiles).ToDictionary();
            await JS.InvokeVoidAsync("tileMapCanvas.updateTilePalette", paletteTiles);
        }
    }
    public async Task SetShowGrid(ChangeEventArgs e)
    {
        if (e.Value is bool showGrid && JS != null)
        {
            await JS.InvokeVoidAsync("tileMapCanvas.setShowGrid", showGrid);
        }
    }

    [JSInvokable]
    public async Task OnJsPlaceTile(int x, int y, int layer)
    {
        var areaId = Area.Id;
        if (SelectedTile != null && JS != null)
        {
            Definition.AddTilePaletteToArea(Area, SelectedTile.SourcePaletteId);
            var palette = Definition.GetPalette(SelectedTile.SourcePaletteId);
            if (palette != null && SelectedTile != null && palette.Tiles.TryGetValue(SelectedTile.Id, out Tile? tile))
            {
                if (tile == null)
                    return;

                if (JS != null)
                {
                    var updates = new[] { new { x, y, layer, tileId = SelectedTile.Id, paletteId = palette.Id, elevation = 0 } };
                    await JS.InvokeVoidAsync("tileMapCanvas.updateTilePositions", updates);
                }
                Definition.ExecuteTileEdit(areaId, layer, x, y, tile, 0);
            }
        }

    }
    [JSInvokable]
    public async Task OnJsRaiseTile(int x, int y, int layer)
    {
        if (Area != null && Area.TileMaps != null && Area.TileMaps.TryGetValue(layer, out var map))
        {
            var placement = map.GetPlacement(x, y);
            if (placement != null)
            {
                if (placement.Elevation >= MaxTileElevation)
                    return; // Don't raise beyond max elevation
                placement.Elevation += 1; // Raise the tile by increasing its elevation

                if (JS != null)
                {
                    var updates = new[] { new { x, y, layer, tileId = placement.TileId, paletteId = placement.PaletteId, elevation = placement.Elevation } };
                    await JS.InvokeVoidAsync("tileMapCanvas.updateTilePositions", updates);
                }
            }
        }
    }
    [JSInvokable]
    public async Task OnJsLowerTile(int x, int y, int layer)
    {
        if (Area != null && Area.TileMaps != null && Area.TileMaps.TryGetValue(layer, out var map))
        {
            var placement = map.GetPlacement(x, y);
            if (placement != null)
            {
                if (placement.Elevation <= MinTileElevation)
                    return; // Don't lower below min elevation
                placement.Elevation -= 1; // Lower the tile by decreasing its elevation
                if (JS != null)
                {
                    var updates = new[] { new { x, y, layer, tileId = placement.TileId, paletteId = placement.PaletteId, elevation = placement.Elevation } };
                    await JS.InvokeVoidAsync("tileMapCanvas.updateTilePositions", updates);
                }
            }
        }
    }
    [JSInvokable]
    public async Task OnJsFill(int x, int y, int layer, bool ctrlKey)
    {
        if (Area == null || Area.TilePaletteIds.Count() < 0 || Area.TileMaps == null || SelectedTile == null)
            return;
        Definition.AddTilePaletteToArea(Area, SelectedTile.SourcePaletteId);
        var updates = new List<object>();
        var palette = Definition.GetPalette(SelectedTile.SourcePaletteId);
        if (palette == null || !palette.Tiles.ContainsKey(SelectedTile.Id))
            return;
        // Use 32x32px grid units for mapWidth/mapHeight
        int mapWidth = Area.Size.Width;
        int mapHeight = Area.Size.Height;
        var tile = palette.Tiles[SelectedTile.Id];
        // Tile size in 32x32px grid units
        int tileWidth = tile.Size.Width;
        int tileHeight = tile.Size.Height;
        if (Area.TileMaps != null && Area.TileMaps.TryGetValue(layer, out var map))
        {
            int selMinX, selMaxX, selMinY, selMaxY;
            if (SelectedCells != null && SelectedCells.Count > 0)
            {
                selMinX = SelectedCells.Min(c => c.X);
                selMaxX = SelectedCells.Max(c => c.X);
                selMinY = SelectedCells.Min(c => c.Y);
                selMaxY = SelectedCells.Max(c => c.Y);
            }
            else
            {
                selMinX = 0;
                selMaxX = mapWidth - 1;
                selMinY = 0;
                selMaxY = mapHeight - 1;
            }
            // Calculate fillable area
            int fillWidth = ((selMaxX - selMinX + 1) / tileWidth) * tileWidth;
            int fillHeight = ((selMaxY - selMinY + 1) / tileHeight) * tileHeight;
            for (int fy = selMinY; fy < selMinY + fillHeight; fy += tileHeight)
            {
                for (int fx = selMinX; fx < selMinX + fillWidth; fx += tileWidth)
                {
                    // Check if all cells for this tile placement are within selection
                    bool fits = true;
                    for (int ty = 0; ty < tileHeight && fits; ty++)
                    {
                        for (int tx = 0; tx < tileWidth && fits; tx++)
                        {
                            int cx = fx + tx;
                            int cy = fy + ty;
                            if (cx < 0 || cx >= mapWidth || cy < 0 || cy >= mapHeight)
                            {
                                fits = false;
                                break;
                            }
                            if (SelectedCells != null && SelectedCells.Count > 0)
                            {
                                if (!SelectedCells.Any(c => c.X == cx && c.Y == cy && c.Layer == layer))
                                {
                                    fits = false;
                                    break;
                                }
                            }
                        }
                    }
                    if (!fits) continue;
                    var placement = map.GetPlacement(fx, fy);

                    if (ctrlKey && placement == null)
                        continue;

                    // Place the tile
                    map.SetPlacement(fx, fy, SelectedTile.Id, SelectedTile.SourcePaletteId, 0);
                    updates.Add(new { x = fx, y = fy, layer, tileId = SelectedTile.Id, paletteId = SelectedTile.SourcePaletteId, elevation = 0 });
                }
            }
        }
        if (JS != null)
        {
            await JS.InvokeVoidAsync("tileMapCanvas.updateTilePositions", updates);
        }
    }

    [JSInvokable]
    public async Task OnJsRemoveTile(int x, int y, int layer)
    {
        if (Area != null && Area.TileMaps != null && Area.TileMaps.TryGetValue(layer, out var map))
        {
            var currentPlacement = map.GetPlacement(x, y);
            map.SetPlacement(x, y, null, null, currentPlacement?.Elevation ?? 0);
            if (JS != null)
            {
                var updates = new[] { new { x, y, layer } };
                await JS.InvokeVoidAsync("tileMapCanvas.updateTilePositions", updates);
            }
        }
    }

    [JSInvokable]
    public void OnJsSelectionChanged(JsonElement selectedCells)
    {
        // The JS side sends an array of objects with x, y, layer
        SelectedCells = [];
        foreach (var cell in selectedCells.EnumerateArray())
        {
            int x = cell.GetProperty("x").GetInt32();
            int y = cell.GetProperty("y").GetInt32();
            int layer = cell.TryGetProperty("layer", out var lyr) ? lyr.GetInt32() : 0;
            SelectedCells.Add(new Coordinate(x, y, layer));
        }
        StateHasChanged();
    }

    private async Task SelectTool(string tool)
    {
        if (selectedTool != tool && JS != null)
        {
            selectedTool = tool;
            await JS.InvokeVoidAsync("tileMapCanvas.selectTool", tool);
        }
    }
    public void Dispose()
    {
        dotNetRef?.Dispose();
        dotNetRef = null;
        GC.SuppressFinalize(this);
    }
}