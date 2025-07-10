using Blazeditor.Application.Components.Dialogs;
using Blazeditor.Application.Models;
using Microsoft.AspNetCore.Components;

namespace Blazeditor.Application.Components.Pages
{
    public partial class AreaEditor
    {
        [Parameter]
        public int areaId { get; set; }
        private ImportTilePalette? popupRef;
        public Tile? SelectedTile  { get; set; }
        public Area? Area { get; set; }
        protected override void OnParametersSet()
        {
            Definition.SelectedArea = Definition.GetAreas().FirstOrDefault(a => a.Id == areaId);
            Area = Definition.SelectedArea;
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

        private void HandleInput(string input)
        {
            if (Area != null)
            {
                Definition.AddTilePaletteToArea(Area, input);
                StateHasChanged();
            }
        }

        public void OnTileSelected(int tileId)
        {
            SelectedTile = Area?.TilePalette.FirstOrDefault(t => t.Id == tileId);
            StateHasChanged();
        }
    }
}