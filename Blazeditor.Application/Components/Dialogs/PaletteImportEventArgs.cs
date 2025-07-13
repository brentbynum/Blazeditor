using Blazeditor.Application.Models;

namespace Blazeditor.Application.Components.Dialogs
{
    public class PaletteImportEventArgs(string fileName, Size cellSize) : EventArgs
    {
        public string FileName { get; set; } = fileName;
        public Size CellSize { get; set; } = cellSize;
    }
}