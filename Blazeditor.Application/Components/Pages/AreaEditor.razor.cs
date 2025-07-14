using Blazeditor.Application.Components.Dialogs;
using Blazeditor.Application.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Blazeditor.Application.Components.Pages
{
    public partial class AreaEditor : IDisposable
    {
        [Parameter]
        public int areaId { get; set; }
        public Tile? SelectedTile  { get; set; }
        public int ActiveLevel { get; set; } = 0;
        public Area? Area { get; set; }
        private DotNetObjectReference<AreaEditor>? dotNetRef;
        private int? previousAreaId = null;

        protected override async Task OnParametersSetAsync()
        {
            // Auto-save if navigating to a new area and there are unsaved changes
            if (previousAreaId.HasValue && previousAreaId.Value != areaId && Definition.IsDirty)
            {
                await Definition.SaveAsync();
            }
            previousAreaId = areaId;
            Definition.SelectedArea = Definition.GetAreas().FirstOrDefault(a => a.Id == areaId);
            Area = Definition.SelectedArea;
        }

        public void OnTileSelected(int tileId)
        {
            SelectedTile = Area?.TilePalette[tileId];
            StateHasChanged();
        }
        public void OnLevelSelected(int level)
        {
            ActiveLevel = level;
            StateHasChanged();
        }
        public void OnAddLevel()
        {
            if (Area != null)
            {
                int newLevel = Area.TileMaps.Count > 0 ? Area.TileMaps.Keys.Max() + 1 : 0;
                Area.TileMaps[newLevel] = new TileMap($"Level {newLevel}", $"Tile map for level {newLevel}", newLevel, Area.Size);
                ActiveLevel = newLevel;
                StateHasChanged();
            }
        }
        public void OnRemoveLevel()
        {
            if (Area != null && Area.TileMaps.Count > 1)
            {
                if (Area.TileMaps.TryGetValue(ActiveLevel, out var map))
                {
                    Area.TileMaps.Remove(ActiveLevel);
                    ActiveLevel = Area.TileMaps.Keys.FirstOrDefault();
                    StateHasChanged();
                }
            }
        }

        private async Task OnPaletteImport(PaletteImportEventArgs paletteImportEventArgs)
        {
            if (Area != null)
            {
                await Definition.AddTilePaletteToArea(Area, paletteImportEventArgs.FileName, paletteImportEventArgs.CellSize.Width, paletteImportEventArgs.CellSize.Height);
                Area = Definition.GetAreas().FirstOrDefault(a => a.Id == areaId);
                StateHasChanged();
            }
        }

        [JSInvokable]
        public void OnUndo()
        {
            Definition.UndoTileEdit();
            Area = Definition.GetAreas().FirstOrDefault(a => a.Id == areaId);
            StateHasChanged();
        }
        [JSInvokable]
        public void OnRedo()
        {
            Definition.RedoTileEdit();
            Area = Definition.GetAreas().FirstOrDefault(a => a.Id == areaId);
            StateHasChanged();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                dotNetRef = DotNetObjectReference.Create(this);
                await JS.InvokeVoidAsync("areaEditorKeyboard.init", dotNetRef);
            }
        }
        public void Dispose()
        {
            if (dotNetRef != null)
            {
                JS.InvokeVoidAsync("areaEditorKeyboard.dispose");
                dotNetRef.Dispose();
            }
        }
    }
}