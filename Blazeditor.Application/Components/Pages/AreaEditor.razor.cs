using Blazeditor.Application.Components.Dialogs;
using Blazeditor.Application.Models;
using Blazeditor.Application.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Blazeditor.Application.Components.Pages;

public partial class AreaEditor : IDisposable
{
    [Parameter]
    public int AreaId { get; set; }
    public Tile? SelectedTile { get; set; }
    public int ActiveLayer { get; set; } = 0;
    public required Area Area { get; set; }
    private DotNetObjectReference<AreaEditor>? dotNetRef;
    private int? previousAreaId = null;
    private int? selectedPaletteId;


    public required TileMapCanvas TileMapCanvas { get; set; }
    public required TilePaletteCanvas TilePaletteCanvas { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        // Auto-save if navigating to a new area and there are unsaved changes
        if (previousAreaId.HasValue && previousAreaId.Value != AreaId && Definition.IsDirty)
        {
            await Definition.SaveAsync();
        }
        previousAreaId = AreaId;
        Definition.SelectedArea = Definition.GetAreas().FirstOrDefault(a => a.Id == AreaId);
        Area = Definition.SelectedArea ?? throw new InvalidOperationException($"Area with ID {AreaId} not found.");
    }

    protected override void OnInitialized()
    {
        // Default to first palette in area or null
        selectedPaletteId = Area?.TilePaletteIds.FirstOrDefault();
        Definition.OnChanged += HandleDefinitionChanged;
    }

    private void HandleDefinitionChanged(Definition definition)
    {
        InvokeAsync(StateHasChanged);
    }

    public void OnTileSelected(int tileId)
    {
        if (Area == null || !selectedPaletteId.HasValue)
        {
            SelectedTile = null;
            return;
        }
        SelectedTile = Definition.GetPalette(selectedPaletteId.Value).Tiles[tileId];
        StateHasChanged();
    }
    public void OnLayerSelected(int layer)
    {
        ActiveLayer = layer;
        StateHasChanged();
    }

    private void OnPaletteChanged(ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out int newPaletteId))
        {
            selectedPaletteId = newPaletteId;
            StateHasChanged();
        }
    }
    public async Task OnAddLayer()
    {
        int newLayer = Area.TileMaps.Count > 0 ? Area.TileMaps.Keys.Max() + 1 : 0;
        Area.TileMaps[newLayer] = new TileMap($"Layer {newLayer}", $"Tile map for layer {newLayer}", newLayer, Area.Size);
        ActiveLayer = newLayer;
        await JS.InvokeVoidAsync("tileMapCanvas.updateTileMaps", Area.TileMaps);
        StateHasChanged();
    }
    public async Task OnRemoveLayer()
    {
        if (Area.TileMaps.TryGetValue(ActiveLayer, out _))
        {
            Area.TileMaps.Remove(ActiveLayer);
            ActiveLayer = Area.TileMaps.Keys.FirstOrDefault();
            await JS.InvokeVoidAsync("tileMapCanvas.updateTileMaps", Area.TileMaps);
            StateHasChanged();
        }
    }

    private async Task OnPaletteImport(PaletteImportEventArgs paletteImportEventArgs)
    {
        var palette = Definition.ImportTileset(paletteImportEventArgs.FileName, null);
        await TileMapCanvas.UpdateTilePalette();
        await Definition.SaveAsync();
        StateHasChanged();
    }

    public async Task DeleteSelectedTile()
    {
        if (Area != null && SelectedTile != null && selectedPaletteId.HasValue)
        {
            // Check if the tile is referenced in any TileMap
            bool isReferenced = Area.TileMaps.Values.Any(map =>
                map.TilePlacements.Any(tp => tp != null && tp.TileId == SelectedTile.Id));
            if (isReferenced)
            {
                // Optionally, show a message to the user (could use a toast, dialog, etc.)
                // For now, just return and do nothing
                // TODO: Add user feedback for failed delete
                return;
            }
            Definition.GetPalette(selectedPaletteId.Value).Tiles.Remove(SelectedTile.Id);
            SelectedTile = null;
            await Definition.SaveAsync();
            StateHasChanged();
        }
    }

    [JSInvokable]
    public void OnUndo()
    {
        Definition.UndoTileEdit();
        Area = Definition.GetAreas().FirstOrDefault(a => a.Id == AreaId) ?? throw new InvalidOperationException($"Area not found: {AreaId}");
        StateHasChanged();
    }
    [JSInvokable]
    public void OnRedo()
    {
        Definition.RedoTileEdit();
        Area = Definition.GetAreas().FirstOrDefault(a => a.Id == AreaId) ?? throw new InvalidOperationException($"Area not found: {AreaId}");
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

    public async Task SaveDefinitionAsync()
    {
        await Definition.SaveAsync();
        StateHasChanged();
    }

    public void Dispose()
    {
        Definition.OnChanged -= HandleDefinitionChanged;
        if (dotNetRef != null)
        {
            JS.InvokeVoidAsync("areaEditorKeyboard.dispose");
            dotNetRef.Dispose();
        }
    }
}