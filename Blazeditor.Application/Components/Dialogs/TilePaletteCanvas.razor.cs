using Blazeditor.Application.Models;
using Blazeditor.Application.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Threading.Tasks;

namespace Blazeditor.Application.Components.Dialogs;

public partial class TilePaletteCanvas : IDisposable
{
    [Inject] public DefinitionManager Definition { get; set; } = default!;
    private ElementReference canvasRef;
    [Parameter] public required Area Area { get; set; }
    [Parameter] public int? SelectedPaletteId { get; set; }
    [Parameter] public EventCallback<int> OnTileSelected { get; set; }

    private DotNetObjectReference<TilePaletteCanvas>? dotNetRef;

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
        if (SelectedPaletteId.HasValue)
        {
            var palette = Definition.GetPalette(SelectedPaletteId.Value);
            if (_firstRenderDone && _shouldInitJs && palette != null && JS != null)
            {
                // Ask JS to calculate and set the required canvas height and initialize with the area palette and cell size
                await JS.InvokeVoidAsync("tilePaletteCanvas.init", canvasRef, palette.Tiles, palette.CellSize);
                _shouldInitJs = false;
            }
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

    public void Dispose()
    {
        dotNetRef?.Dispose();
    }
}