namespace Blazeditor.Application.Models
{
    public class Portal(Area destinationArea, Area locationArea, int destX, int destY, int locX, int locY) : BaseEntity
    {
        public Area DestinationArea { get; set; } = destinationArea;
        public Area LocationArea { get; set; } = locationArea;
        public Coordinate Destination { get; set; } = new Coordinate(destX, destY);
        public Coordinate Location { get; set; } = new Coordinate(locX, locY);
    }

    public class  PortalLock
    {
        
    }

    public class  PortalKey
    {
        
    }
}