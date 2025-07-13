namespace Blazeditor.Application.Models
{
    public class Portal : BaseEntity
    {
        public Portal() : base() { }
        public Portal(Area destinationArea, Area locationArea, int destX, int destY, int locX, int locY) : base(destinationArea.Name, locationArea.Name)
        {
            DestinationArea = destinationArea;
            LocationArea = locationArea;
            Destination = new Coordinate(destX, destY);
            Location = new Coordinate(locX, locY);
        }
        public Area? DestinationArea { get; set; }
        public Area? LocationArea { get; set; }
        public Coordinate Destination { get; set; }
        public Coordinate Location { get; set; }
    }

    public class PortalLock
    {
        // Implementation
    }

    public class PortalKey
    {
        // Implementation
    }
}