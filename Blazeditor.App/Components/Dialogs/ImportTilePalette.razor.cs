using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Blazeditor.App.Components.Dialogs
{
    public partial class ImportTilePalette
    {
        public List<string?> Filenames = new List<string?>();
        public string? SelectedFilename { get; set; }
        private string ModalClass => IsVisible ? "show" : "hide";

        [Parameter] public EventCallback<string> OnConfirm { get; set; }

        private bool IsVisible { get; set; }

        protected override void OnInitialized()
        {
            // Initialize the tileset filenames from the DefinitionManager.
            Filenames = Definition.GetTileImageFilenames();
        }
        public void Show()
        {
            IsVisible = true;
            StateHasChanged();
        }

        private void Confirm()
        {
            OnConfirm.InvokeAsync(SelectedFilename);
            Close();
        }

        private void Close()
        {
            IsVisible = false;
            StateHasChanged();
        }
    }
}