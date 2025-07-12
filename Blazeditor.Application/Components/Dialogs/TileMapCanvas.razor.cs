using Blazeditor.Application.Models;
using Blazeditor.Application.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Collections.Generic;
using System.Text.Json;

namespace Blazeditor.Application.Components.Dialogs
{
    public partial class TileMapCanvas
    {
        [Parameter] public Area Area { get; set; } = new();
        [Parameter] public int SelectedTileId { get; set; }
        private ElementReference canvasRef;
        private DotNetObjectReference<TileMapCanvas>? dotNetRef;
        private const int CellSize = 64;
        private string selectedTool = "paint"; // Default tool
        private List<Coordinate> SelectedCells { get; set; } = new();

        protected override async Task OnParametersSetAsync()
        {
            // Redraw the canvas when Tiles changes
            if (Area.TileMaps != null && Area.TileMaps.Count > 0 && canvasRef.Context != null)
            {
                await JS.InvokeVoidAsync("tileMapCanvas.init", canvasRef, Area.TileMaps, CellSize, Area.TilePalette);
                await JS.InvokeVoidAsync("tileMapCanvas.setSelectedTileId", SelectedTileId);
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                dotNetRef = DotNetObjectReference.Create(this);
                await JS.InvokeVoidAsync("tileMapCanvas.setDotNetRef", dotNetRef);

            }
        }

        public void ClearTileMaps()
        {
            if (Area.TileMaps != null && Area.TileMaps.Count > 0)
            {
                if (SelectedCells != null && SelectedCells.Count > 0)
                {
                    foreach (var cell in SelectedCells)
                    {
                        if (Area.TileMaps.TryGetValue(cell.Level, out var map))
                        {
                            if (cell.X >= 0 && cell.X < map.Size.Width && cell.Y >= 0 && cell.Y < map.Size.Height)
                            {
                                map[cell.X, cell.Y] = null;
                            }
                        }
                    }
                }
                else
                {
                    foreach (var tileMap in Area.TileMaps.Values)
                    {
                        for (int y = 0; y < tileMap.Size.Height; y++)
                        {
                            for (int x = 0; x < tileMap.Size.Width; x++)
                            {
                                tileMap[x, y] = null;
                            }
                        }
                    }
                }
  
                InvokeAsync(() => {
                    JS.InvokeVoidAsync("tileMapCanvas.updateTileMaps", Area.TileMaps);
                    StateHasChanged();
                });
            }
        }

        public async Task SetShowGrid(ChangeEventArgs e)
        {
            if (e.Value is bool showGrid)
            {
                await JS.InvokeVoidAsync("tileMapCanvas.setShowGrid", showGrid);
            }
        }

        [JSInvokable]
        public void OnJsPlaceTile(int tileId, int x, int y, int level)
        {
            var areaId = Area.Id;
            var tile = Area.TilePalette.FirstOrDefault(t => t.Id == tileId);
            Definition.ExecuteTileEdit(areaId, level, x, y, tile);
            InvokeAsync(() => {
                var updates = new[] { new { x, y, level, tile } };
                JS.InvokeVoidAsync("tileMapCanvas.updateTilePositions", updates);  
                JS.InvokeVoidAsync("tileMapCanvas.profilePaintEnd");
                StateHasChanged();
            });
        }

        [JSInvokable]
        public void OnJsFill(int tileId, int x, int y, int level, bool ctrlKey)
        {
            var updates = new List<object>();
            if (Area.TilePalette.FirstOrDefault(t => t.Id == tileId) == null)
                return;
            if (SelectedCells != null && SelectedCells.Count > 0)
            {
                foreach (var cell in SelectedCells)
                {
                    if (Area.TileMaps.TryGetValue(cell.Level, out var map))
                    {
                        if (cell.X >= 0 && cell.X < map.Size.Width && cell.Y >= 0 && cell.Y < map.Size.Height)
                        {
                            var oldTile = map[cell.X, cell.Y];
                            if (ctrlKey && oldTile != null)
                                continue; // Only fill if cell is empty
                            var tile = Area.TilePalette.FirstOrDefault(t => t.Id == tileId);
                            map[cell.X, cell.Y] = tile;
                            updates.Add(new { x = cell.X, y = cell.Y, level = cell.Level, tile });
                        }
                    }
                }
            }
            InvokeAsync(() => {
                JS.InvokeVoidAsync("tileMapCanvas.updateTilePositions", updates);
                StateHasChanged();
            });
        }

        [JSInvokable]
        public void OnJsRemoveTile(int x, int y, int level)
        {
            if (Area.TileMaps.TryGetValue(level, out var map))
            {
                map[x, y] = null;
                map.Tiles = map.Tiles.ToArray();
                InvokeAsync(() => {
                    var updates = new[] { new { x, y, level } };
                    JS.InvokeVoidAsync("tileMapCanvas.updateTilePositions", updates);
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
            if (selectedTool != tool)
            {
                selectedTool = tool;
                await JS.InvokeVoidAsync("tileMapCanvas.selectTool", tool);
            }
        }
    }
}