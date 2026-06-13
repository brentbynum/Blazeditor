namespace Blazeditor.Application.Models;

public enum TileRole
{
    None = 0,
    Floor = 1,
    Shim = 2,
}

public enum ShimType
{
    None = 0,
    Run, //These need to be (TileSize + 64) x 32. They get rotated & mirrored to match the shim direction being rendered
    CapMask, // These need to be 32x32. They get rotates & mirrored to match the shim
    OverhangMask // These need to be (TileSize + 64) x 32. They get rotated & mirrored to match the shim
}

public class Tile : BaseEntity
{
    public Tile() : base() { }
    public Tile(string name, string description, string type, string imageBase64, Size size, Guid sourcePaletteId) : base(name, description)
    {
        Type = type;
        Image = imageBase64;
        Size = size;
        SourcePaletteId = sourcePaletteId;
    }
    public TileRole Role { get; set; } = TileRole.None;
    public string Type { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public Size Size { get; set; } = new Size(1, 1);

    public Guid SourcePaletteId { get; set; } = Guid.Empty; // ID of the source palette this tile belongs to

    // Meaningful only when Role == TileRole.Floor; null otherwise
    public FloorProperties? FloorProperties { get; set; }

    // Meaningful only when Role == TileRole.Shim; null otherwise
    public ShimProperties? ShimProperties { get; set; }

    public TileState PaletteState { get; set; }
}

public class FloorProperties
{
    public float Impedance { get; set; }
}

public class ShimProperties
{
    public ShimType ShimType { get; set; } = ShimType.None;
}

public struct TileState
{
    public Layout Layout { get; set; }
    public bool IsMouseOver { get; set; }
}