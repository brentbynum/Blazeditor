using Blazeditor.Application.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Collections.Generic;
using System.Text.Json;

namespace Blazeditor.Application.Components.Dialogs
{
    public partial class TileMapCanvas
    {
        [Parameter] public Dictionary<int, TileMap> TileMaps { get; set; } = new();
        [Parameter] public List<Tile> TilePalette { get; set; } = new();
        [Parameter] public int SelectedTileId { get; set; }
        private ElementReference canvasRef;
        private DotNetObjectReference<TileMapCanvas>? dotNetRef;
        private const int CellSize = 64;
        private string selectedTool = "paint"; // Default tool
        private List<Coordinate> SelectedCells { get; set; } = new();

        protected override async Task OnParametersSetAsync()
        {
            // Redraw the canvas when Tiles changes
            if (TileMaps != null && TileMaps.Count > 0 && canvasRef.Context != null)
            {
                await JS.InvokeVoidAsync("tileMapCanvas.init", canvasRef, TileMaps, CellSize);
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
        public async void ClearTileMaps()
        {
            // Clear the current map level
            if (TileMaps != null && TileMaps.Count > 0)
            {
                foreach (var tileMap in TileMaps.Values)
                {
                    tileMap.Tiles = new Tile[tileMap.Size.Width * tileMap.Size.Height];
                }
                StateHasChanged();
                await JS.InvokeVoidAsync("tileMapCanvas.updateTileMaps", TileMaps);
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
        public async void OnJsPlaceTile(int tileId, int x, int y, int level)
        {
            if (TileMaps.TryGetValue(level, out var map))
            {
                map[x, y] = TilePalette.FirstOrDefault(t => t.Id == tileId);
            }
            await JS.InvokeVoidAsync("tileMapCanvas.updateTileMaps", TileMaps);
        }

        [JSInvokable]
        public async void OnJsFill(int tileId, int x, int y, int level, bool ctrlKey)
        {
            var tile = TilePalette.FirstOrDefault(t => t.Id == tileId);
            if (tile == null)
                return;

            foreach (var cell in SelectedCells)
            {
                if (TileMaps.TryGetValue(cell.Level, out var map))
                {
                    if (cell.X >= 0 && cell.X < map.Size.Width && cell.Y >= 0 && cell.Y < map.Size.Height)
                    {
                        if (ctrlKey)
                        {
                            // Only fill if cell is empty
                            if (map[cell.X, cell.Y] == null)
                                map[cell.X, cell.Y] = tile;
                        }
                        else
                        {
                            // Always fill (replace existing)
                            map[cell.X, cell.Y] = tile;
                        }
                    }
                }
            }
            await JS.InvokeVoidAsync("tileMapCanvas.updateTileMaps", TileMaps);
        }

        [JSInvokable]
        public async void OnJsRemoveTile(int x, int y, int level)
        {
            if (TileMaps.TryGetValue(level, out var map))
            {
                map[x, y] = null;
            }
            await JS.InvokeVoidAsync("tileMapCanvas.updateTileMaps", TileMaps);
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