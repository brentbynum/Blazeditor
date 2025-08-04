using Blazeditor.Application.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using System.Text.Json;

namespace Blazeditor.Application.Components.Dialogs;

public partial class TileMapCanvas : IDisposable
{
    [Parameter] public required Area Area { get; set; } = new();
    [Parameter] public Tile? SelectedTile { get; set; }

    [Parameter] public int ActiveLayer { get; set; }
    private ElementReference canvasRef;
    private DotNetObjectReference<TileMapCanvas>? dotNetRef;
    private string selectedTool = "paint"; // Default tool
    private List<Coordinate> SelectedCells { get; set; } = new();

    private bool _shouldInitJs = false;
    private bool _shouldInitMap = true; // Flag to re-initialize map

    protected override void OnParametersSet()
    {
        // Set a flag to re-initialize JS after parameters change
        _shouldInitJs = true;
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
                var paletteTiles = Area.TilePaletteIds.SelectMany(id => Definition.GetPalette(id).Tiles).ToDictionary();
                // The map only ever gets init-ed once, and the tilemaps get synched manually on add/remove
                await JS.InvokeVoidAsync("tileMapCanvas.init", canvasRef, Area.TileMaps, Area.CellSize, Area.Size, paletteTiles);
                _shouldInitMap = false; // Reset flag after initialization

            }
            if (_shouldInitJs)
            {
                await JS.InvokeVoidAsync("tileMapCanvas.setSelectedTileId", SelectedTile?.Id);                
                await JS.InvokeVoidAsync("tileMapCanvas.setActiveLayer", ActiveLayer);
                _shouldInitJs = false;
            }
        }
    }

    public void ClearTileMaps()
    {
        if (Area?.TileMaps != null && Area.TileMaps.Count > 0)
        {
            // Use 32x32 grid units for width/height
            int width = Area.Size.Width / 32;
            int height = Area.Size.Height / 32;
            if (SelectedCells != null && SelectedCells.Count > 0)
            {
                foreach (var cell in SelectedCells)
                {
                    if (Area.TileMaps.TryGetValue(cell.Layer, out var map))
                    {
                        if (cell.X >= 0 && cell.X < width && cell.Y >= 0 && cell.Y < height)
                        {
                            map.SetPlacement(cell.X, cell.Y, null, null);
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
                            tileMap.SetPlacement(x, y, null, null);
                        }
                    }
                }
            }

            InvokeAsync(() =>
            {
                if (JS != null)
                {
                    var tilePlacementsByLayer = Area.TileMaps.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.TilePlacements.Where(p => p != null).ToList()
                    );
                    JS.InvokeVoidAsync("tileMapCanvas.updateTileMaps", tilePlacementsByLayer);
                }
                StateHasChanged();
            });
        }
    }
    public async Task UpdateTilePalette()
    {
        if (Area.TilePaletteIds.Count() > 0)
        {
            var paletteTiles = Area.TilePaletteIds.Select(id => Definition.GetPalette(id)).SelectMany(p => p.Tiles);
            await JS.InvokeVoidAsync("tileMapCanvas.updateTilePalette", paletteTiles);
            StateHasChanged();
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
    public void OnJsPlaceTile(int x, int y, int layer)
    {
        var areaId = Area.Id;
        if (SelectedTile != null && JS != null)
        {
            if (Area.TilePaletteIds.Contains(SelectedTile.SourcePaletteId) == false)
            {
                Area.TilePaletteIds.Add(SelectedTile.SourcePaletteId);
            }
            var palette = Definition.GetPalette(SelectedTile.SourcePaletteId);
            if (palette != null && SelectedTile != null && palette.Tiles.TryGetValue(SelectedTile.Id, out Tile? tile))
            {
                if (tile == null)
                    return;

                InvokeAsync(() =>
                {
                    if (JS != null)
                    {
                        var updates = new[] { new { x, y, layer, tileId = SelectedTile.Id, paletteId = palette.Id } };
                        JS.InvokeVoidAsync("tileMapCanvas.updateTilePositions", updates);
                    }
                    StateHasChanged();
                });
                Definition.ExecuteTileEdit(areaId, layer, x, y, tile);
            }
        }

    }


    [JSInvokable]
    public void OnJsFill(int x, int y, int layer, bool ctrlKey)
    {
        if (Area == null || Area.TilePaletteIds.Count() > 0 || Area.TileMaps == null || SelectedTile == null)
            return;
        if (Area.TilePaletteIds.Contains(SelectedTile.SourcePaletteId) == false)
        {
            Area.TilePaletteIds.Add(SelectedTile.SourcePaletteId);
        }
        var updates = new List<object>();
        var palette = Definition.GetPalette(SelectedTile.SourcePaletteId);
        if (palette == null || !palette.Tiles.ContainsKey(SelectedTile.Id))
            return;
        // Use 32x32 grid units for mapWidth/mapHeight
        int mapWidth = Area.Size.Width / 32;
        int mapHeight = Area.Size.Height / 32;
        var tile = palette.Tiles[SelectedTile.Id];
        // Tile size in 32x32 grid units
        int tileWidth = tile.Size.Width / 32;
        int tileHeight = tile.Size.Height / 32;
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
                    map.SetPlacement(fx, fy, SelectedTile.Id, SelectedTile.SourcePaletteId);
                    updates.Add(new { x = fx, y = fy, layer, tileId=SelectedTile.Id, paletteId = palette.Id });
                }
            }
        }
        InvokeAsync(() =>
        {
            if (JS != null)
            {
                JS.InvokeVoidAsync("tileMapCanvas.updateTilePositions", updates);
            }
            StateHasChanged();
        });
    }

    [JSInvokable]
    public void OnJsRemoveTile(int x, int y, int layer)
    {
        if (Area != null && Area.TileMaps != null && Area.TileMaps.TryGetValue(layer, out var map))
        {
            map.SetPlacement(x, y, null, null);
            InvokeAsync(() =>
            {
                if (JS != null)
                {
                    var updates = new[] { new { x, y, layer } };
                    JS.InvokeVoidAsync("tileMapCanvas.updateTilePositions", updates);
                }
                StateHasChanged();
            });
        }
    }

    [JSInvokable]
    public void OnJsSelectionChanged(JsonElement selectedCells)
    {
        // The JS side sends an array of objects with x, y, layer
        SelectedCells = new List<Coordinate>();
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
        if (dotNetRef != null)
        {
            dotNetRef.Dispose();
            dotNetRef = null;
        }
        GC.SuppressFinalize(this);
    }
}