namespace Blazeditor.App.Models
{
    public class ItemInstance(Item item, int x, int y) : BaseEntity
    {
        public Item Item { get; } = item;
        public Coordinate Location { get; set; } = new Coordinate(x, y);
        public int Quantity { get; set; } = 1;
    }
}
