using Blazeditor.Application.Models;
using Blazeditor.Application.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Collections.Generic;
using System.Text.Json;

namespace Blazeditor.Application.Components.Dialogs
{
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
                await JS.InvokeVoidAsync("tileMapCanvas.setSelectedTileId", SelectedTileId);
                await JS.InvokeVoidAsync("tileMapCanvas.setActiveLevel", ActiveLevel);
                await JS.InvokeVoidAsync("tileMapCanvas.init", canvasRef, Area.TileMaps, Area.CellSize, Area.Size, Area.TilePalette);
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
                                map[cell.X, cell.Y] = null;
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
                                tileMap[x, y] = null;
                            }
                        }
                    }
                }

                InvokeAsync(() =>
                {
                    if (JS != null)
                    {
                        JS.InvokeVoidAsync("tileMapCanvas.updateTileMaps", Area.TileMaps);
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
            if (areaId.HasValue && Area?.TilePalette != null && Area.TilePalette.ContainsKey(tileId))
            {
                var tile = Area.TilePalette[tileId];
                if (tile == null)
                    return;
                Definition.ExecuteTileEdit(areaId.Value, level, x, y, tile);
                InvokeAsync(() =>
                {
                    if (JS != null)
                    {
                        var updates = new[] { new { x, y, level, tile } };
                        JS.InvokeVoidAsync("tileMapCanvas.updateTilePositions", updates);
                    }
                    StateHasChanged();
                });
                
            }
        }

        [JSInvokable]
        public void OnJsFill(int tileId, int x, int y, int level, bool ctrlKey)
        {
            var updates = new List<object>();
            if (Area?.TilePalette == null || !Area.TilePalette.ContainsKey(tileId))
                return;
            int width = Area.Size.Width;
            int height = Area.Size.Height;
            if (Area.TileMaps != null && Area.TileMaps.TryGetValue(level, out var map))
            {
                if (SelectedCells != null && SelectedCells.Count > 0)
                {
                    foreach (var cell in SelectedCells)
                    {
                        if (cell.X >= 0 && cell.X < width && cell.Y >= 0 && cell.Y < height)
                        {
                            var oldTile = map[cell.X, cell.Y];
                            if (ctrlKey && oldTile != null)
                                continue; // Only fill if cell is empty
                            var tile = Area.TilePalette[tileId];
                            map[cell.X, cell.Y] = tile;
                            updates.Add(new { x = cell.X, y = cell.Y, level, tile });
                        }
                    }
                }
                else
                {
                    if (x >= 0 && x < width && y >= 0 && y < height)
                    {
                        for (var ty = 0; ty < height; ty++)
                        {
                            for (var tx = 0; tx < width; tx++)
                            {
                                var oldTile = map[tx, ty];
                                if (ctrlKey && oldTile != null)
                                    return; // Only fill if cell is empty
                                if (oldTile?.Id != tileId)
                                {
                                    var tile = Area.TilePalette[tileId];
                                    map[tx, ty] = tile;
                                    updates.Add(new { tx, ty, level, tileId });
                                }
                            }
                        }
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
                map[x, y] = null;
                map.Tiles = map.Tiles.ToArray();
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
}