using Blazeditor.Application.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using System.Text.Json;

namespace Blazeditor.Application.Components.Dialogs;

public partial class TileMapCanvas : IDisposable
{
    [Parameter] public Area? Area { get; set; } = new();
    [Parameter] public int SelectedTileId { get; set; }
    [Parameter] public int ActiveLevel { get; set; }
    private ElementReference canvasRef;
    private DotNetObjectReference<TileMapCanvas>? dotNetRef;
    private string selectedTool = "paint"; // Default tool
    private List<Coordinate> SelectedCells { get; set; } = new();

    private bool _shouldInitJs = false;
    private bool _shouldInitMap = true; // Flag to re-initialize map
    private bool _firstRenderDone = false;

    protected override void OnParametersSet()
    {
        // Set a flag to re-initialize JS after parameters change
        _shouldInitJs = true;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            if (JS != null)
            {
                dotNetRef = DotNetObjectReference.Create(this);
                await JS.InvokeVoidAsync("tileMapCanvas.setDotNetRef", dotNetRef);
            }
            _firstRenderDone = true;
        }
        if (_firstRenderDone && _shouldInitJs && Area != null && Area.TileMaps != null && Area.TilePalette != null && JS != null)
        {
            if (_shouldInitMap)
            {
                // The map only ever gets init-ed once, and the tilemaps get synched manually on add/remove
                await JS.InvokeVoidAsync("tileMapCanvas.init", canvasRef, Area.TileMaps, Area.CellSize, Area.Size, Area.TilePalette);
                _shouldInitMap = false; // Reset flag after initialization
            }
            await JS.InvokeVoidAsync("tileMapCanvas.setSelectedTileId", SelectedTileId);
            await JS.InvokeVoidAsync("tileMapCanvas.setActiveLevel", ActiveLevel);
            _shouldInitJs = false;
        }
    }

    public void ClearTileMaps()
    {
        if (Area?.TileMaps != null && Area.TileMaps.Count > 0)
        {
            int width = Area.Size.Width;
            int height = Area.Size.Height;
            if (SelectedCells != null && SelectedCells.Count > 0)
            {
                foreach (var cell in SelectedCells)
                {
                    if (Area.TileMaps.TryGetValue(cell.Level, out var map))
                    {
                        if (cell.X >= 0 && cell.X < width && cell.Y >= 0 && cell.Y < height)
                        {
                            map.SetPlacement(cell.X, cell.Y, null);
                        }
                    }
                }
            }
            else
            {
                if (Area.TileMaps.TryGetValue(ActiveLevel, out var tileMap))
                {
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            tileMap.SetPlacement(x, y, null);
                        }
                    }
                }
            }

            InvokeAsync(() =>
            {
                if (JS != null)
                {
                    var tilePlacementsByLevel = Area.TileMaps.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.TilePlacements.Values.ToList()
                    );
                    JS.InvokeVoidAsync("tileMapCanvas.updateTileMaps", tilePlacementsByLevel);
                }
                StateHasChanged();
            });
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
    public void OnJsPlaceTile(int tileId, int x, int y, int level)
    {
        var areaId = Area?.Id;
        if (areaId.HasValue && Area?.TilePalette != null && Area.TilePalette.TryGetValue(tileId, out Tile? tile))
        {
            if (tile == null)
                return;
            
            InvokeAsync(() =>
            {
                if (JS != null)
                {
                    var updates = new[] { new { x, y, level, tileId } };
                    JS.InvokeVoidAsync("tileMapCanvas.updateTilePositions", updates);
                }
                StateHasChanged();
            });
            Definition.ExecuteTileEdit(areaId.Value, level, x, y, tile);
        }
    }


    [JSInvokable]
    public void OnJsFill(int tileId, int x, int y, int level, bool ctrlKey)
    {
        var updates = new List<object>();
        if (Area?.TilePalette == null || !Area.TilePalette.ContainsKey(tileId))
            return;
        int mapWidth = Area.Size.Width;
        int mapHeight = Area.Size.Height;
        var tile = Area.TilePalette[tileId];
        int tileWidth = tile.Size.Width;
        int tileHeight = tile.Size.Height;
        if (Area.TileMaps != null && Area.TileMaps.TryGetValue(level, out var map))
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
                                if (!SelectedCells.Any(c => c.X == cx && c.Y == cy && c.Level == level))
                                {
                                    fits = false;
                                    break;
                                }
                            }
                        }
                    }
                    if (!fits) continue;
                    // Only fill if empty if ctrlKey is pressed
                    bool skip = false;
                    for (int ty = 0; ty < tileHeight && !skip; ty++)
                    {
                        for (int tx = 0; tx < tileWidth && !skip; tx++)
                        {
                            var placement = map.GetPlacement(fx + tx, fy + ty);
                            if (ctrlKey && placement != null && placement.TileId.HasValue)
                                skip = true;
                        }
                    }
                    if (skip) continue;
                    // Place the tile
                    for (int ty = 0; ty < tileHeight; ty++)
                    {
                        for (int tx = 0; tx < tileWidth; tx++)
                        {
                            map.SetPlacement(fx + tx, fy + ty, tileId);
                        }
                    }
                    updates.Add(new { x = fx, y = fy, level, tileId });
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
    public void OnJsRemoveTile(int x, int y, int level)
    {
        if (Area != null && Area.TileMaps != null && Area.TileMaps.TryGetValue(level, out var map))
        {
            map.SetPlacement(x, y, null);
            InvokeAsync(() =>
            {
                if (JS != null)
                {
                    var updates = new[] { new { x, y, level } };
                    JS.InvokeVoidAsync("tileMapCanvas.updateTilePositions", updates);
                }
                StateHasChanged();
            });
        }
    }

    [JSInvokable]
    public void OnJsSelectionChanged(JsonElement selectedCells)
    {
        // The JS side sends an array of objects with x, y, level
        SelectedCells = new List<Coordinate>();
        foreach (var cell in selectedCells.EnumerateArray())
        {
            int x = cell.GetProperty("x").GetInt32();
            int y = cell.GetProperty("y").GetInt32();
            int level = cell.TryGetProperty("level", out var lvl) ? lvl.GetInt32() : 0;
            SelectedCells.Add(new Coordinate(x, y, level));
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