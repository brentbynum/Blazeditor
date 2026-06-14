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
    [Parameter] public Guid? SelectedPaletteId { get; set; }
    [Parameter] public string? SearchText { get; set; }
    [Parameter] public EventCallback<Guid> OnTileSelected { get; set; }

    private DotNetObjectReference<TilePaletteCanvas>? dotNetRef;

    private bool _shouldInitJs = false;
    private bool _firstRenderDone = false;
    private bool _initialized = false;

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
                var tiles = TileSearch.Filter(palette.Tiles.Values, SearchText).ToDictionary(t => t.Id, t => t);
                if (!_initialized)
                {
                    // Ask JS to calculate and set the required canvas height and initialize with the area palette and cell size
                    await JS.InvokeVoidAsync("tilePaletteCanvas.init", canvasRef, tiles, palette.CellSize);
                    _initialized = true;
                }
                else
                {
                    await JS.InvokeVoidAsync("tilePaletteCanvas.updateTiles", tiles, palette.CellSize);
                }
                _shouldInitJs = false;
            }
        }
    }

    [JSInvokable]
    public async Task OnJsTileSelected(Guid tileId)
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