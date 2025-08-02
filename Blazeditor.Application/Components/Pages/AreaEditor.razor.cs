using Blazeditor.Application.Components.Dialogs;
using Blazeditor.Application.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Blazeditor.Application.Components.Pages;

public partial class AreaEditor : IDisposable
{
    [Parameter]
    public int AreaId { get; set; }
    public Tile? SelectedTile { get; set; }
    public int ActiveLevel { get; set; } = 0;
    public required Area Area { get; set; }
    private DotNetObjectReference<AreaEditor>? dotNetRef;
    private int? previousAreaId = null;

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
        Definition.OnChanged += HandleDefinitionChanged;
    }

    private void HandleDefinitionChanged(Definition definition)
    {
        InvokeAsync(StateHasChanged);
    }

    public void OnTileSelected(int tileId)
    {
        SelectedTile = Area.TilePalette[tileId];
        StateHasChanged();
    }
    public void OnLevelSelected(int level)
    {
        ActiveLevel = level;
        StateHasChanged();
    }
    public async Task OnAddLevel()
    {
        int newLevel = Area.TileMaps.Count > 0 ? Area.TileMaps.Keys.Max() + 1 : 0;
        Area.TileMaps[newLevel] = new TileMap($"Level {newLevel}", $"Tile map for level {newLevel}", newLevel, Area.Size);
        ActiveLevel = newLevel;
        await JS.InvokeVoidAsync("tileMapCanvas.updateTileMaps", Area.TileMaps);
        StateHasChanged();
    }
    public async Task OnRemoveLevel()
    {
        if (Area.TileMaps.TryGetValue(ActiveLevel, out _))
        {
            Area.TileMaps.Remove(ActiveLevel);
            ActiveLevel = Area.TileMaps.Keys.FirstOrDefault();
            await JS.InvokeVoidAsync("tileMapCanvas.updateTileMaps", Area.TileMaps);
            StateHasChanged();
        }
    }

    private async Task OnPaletteImport(PaletteImportEventArgs paletteImportEventArgs)
    {
        await Definition.AddTilePaletteToArea(Area, paletteImportEventArgs.FileName, paletteImportEventArgs.CellSize.Width, paletteImportEventArgs.CellSize.Height);
        Area = Definition.GetAreas().FirstOrDefault(a => a.Id == AreaId) ?? throw new InvalidOperationException($"Area with ID {AreaId} not found.");
        await TileMapCanvas.UpdateTilePalette();
        StateHasChanged();
    }

    public async Task DeleteSelectedTile()
    {
        if (Area != null && SelectedTile != null)
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
            Area.TilePalette.Remove(SelectedTile.Id);
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