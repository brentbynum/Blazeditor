using Blazeditor.Application.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

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
        public async Task ToggleViewGrid(ChangeEventArgs e)
        {
            bool showGrid = (bool)e.Value;
            await JS.InvokeVoidAsync("tileMapCanvas.toggleGrid", showGrid);
        }

        [JSInvokable]
        public async void OnJsPlaceTile(int tileId, int x, int y, int level)
        {
            var map = TileMaps[level];
            if (map != null)
            {
                map[x, y] = TilePalette.FirstOrDefault(t => t.Id == tileId);
            }
            await JS.InvokeVoidAsync("tileMapCanvas.updateTileMaps", TileMaps);
        }
        [JSInvokable]
        public async void OnJsRemoveTile(int x, int y, int level)
        {
            var map = TileMaps[level];
            if (map != null)
            {
                map[x, y] = null;
            }
            await JS.InvokeVoidAsync("tileMapCanvas.updateTileMaps", TileMaps);
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