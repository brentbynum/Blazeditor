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
        private ImportTilePalette? popupRef;
        public Tile? SelectedTile  { get; set; }
        public Area? Area { get; set; }
        private DotNetObjectReference<AreaEditor>? objRef;
        private bool showCreateAreaDialog = false;
        private DotNetObjectReference<AreaEditor>? dotNetRef;
        private int? previousAreaId = null;
        private void ShowCreateAreaDialog() => showCreateAreaDialog = true;
        private void HideCreateAreaDialog() => showCreateAreaDialog = false;

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

        protected override void OnInitialized()
        {
            objRef = DotNetObjectReference.Create(this);
            JS.InvokeVoidAsync("registerKeyboardEvents", objRef);
        }

        private void ShowImportTilePalette()
        {
            Console.WriteLine($"[DEBUG] ShowImportTilePalette called. popupRef is null: {popupRef is null}");
            if (popupRef == null)
            {
                Console.WriteLine("[DEBUG] popupRef is null. ImportTilePalette component may not be rendered yet.");
                return;
            }
            popupRef.Show();
            Console.WriteLine("[DEBUG] popupRef.Show() called.");
        }

        private void HandleInput((string selectedFilename, int cellWidth, int cellHeight) input)
        {
            if (Area != null)
            {
                Definition.AddTilePaletteToArea(Area, input.selectedFilename, input.cellWidth, input.cellHeight);
                Area = Definition.GetAreas().FirstOrDefault(a => a.Id == areaId); 
                StateHasChanged();
            }
        }

        public void OnTileSelected(int tileId)
        {
            SelectedTile = Area?.TilePalette.FirstOrDefault(t => t.Id == tileId);
            StateHasChanged();
        }

        private void HandleCreateArea(CreateAreaDialog.AreaModel model)
        {
            Definition.AddArea(model.Name, model.Description, model.Width, model.Height);
            showCreateAreaDialog = false;
            StateHasChanged();
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
            if (objRef != null)
            {
                JS.InvokeVoidAsync("unregisterKeyboardEvents", objRef);
                objRef.Dispose();
            }
        }
    }
}