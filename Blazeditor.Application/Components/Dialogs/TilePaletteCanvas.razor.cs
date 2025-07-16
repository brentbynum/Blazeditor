using Blazeditor.Application.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Blazeditor.Application.Components.Dialogs;

public partial class TilePaletteCanvas : IDisposable
{
    private ElementReference canvasRef;
    [Parameter] public Area? Area { get; set; }

    [Parameter] public EventCallback<int> OnTileSelected { get; set; }

    private DotNetObjectReference<TilePaletteCanvas>? dotNetRef;
    [Parameter] public EventCallback<PaletteImportEventArgs> OnPaletteImport { get; set; }
    private ImportTilePalette? popupRef;

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
                await JS.InvokeVoidAsync("tilePaletteCanvas.setDotNetRef", dotNetRef);
            }
            _firstRenderDone = true;
        }
        if (_firstRenderDone && _shouldInitJs && Area?.TilePalette != null && JS != null)
        {
            // Ask JS to calculate and set the required canvas height
            var height = await JS.InvokeAsync<int>("tilePaletteCanvas.calculateRequiredHeight", Area?.TilePalette, Area?.CellSize, 512);
            await JS.InvokeVoidAsync("tilePaletteCanvas.setCanvasHeight", canvasRef, height);
            await JS.InvokeVoidAsync("tilePaletteCanvas.init", canvasRef, Area?.TilePalette, Area?.CellSize);
            _shouldInitJs = false;
        }
    }

    [JSInvokable]
    public async Task OnJsTileSelected(int tileId)
    {
        if (OnTileSelected.HasDelegate)
        {
            await OnTileSelected.InvokeAsync(tileId);
        }
    }

    private async Task HandleInput((string selectedFilename, int cellWidth, int cellHeight) input)
    {

        await OnPaletteImport.InvokeAsync(new PaletteImportEventArgs(input.selectedFilename, new Size(input.cellWidth, input.cellHeight)));
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

    public void Dispose()
    {
        dotNetRef?.Dispose();
    }
}