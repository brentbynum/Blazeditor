namespace Blazeditor.Application.Models
{
    public class Portal : BaseEntity
    {
        public Portal() : base() { }
        public Portal(Area destinationArea, Area locationArea, int destX, int destY, int locX, int locY) : base(destinationArea.Name, locationArea.Name)
        {
            DestinationAreaId = destinationArea.Id;
            LocationAreaId = locationArea.Id;
            Destination = new Coordinate(destX, destY);
            Location = new Coordinate(locX, locY);
        }
        public Guid? DestinationAreaId { get; set; }
        public Guid? LocationAreaId { get; set; }
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