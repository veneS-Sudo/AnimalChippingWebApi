namespace AnimalChippingWebApi.Core.Models;

public class AnimalVisitedLocation
{
    public long AVLocationId { get; set; }
    public long AnimalId { get; set; }
    public DateTime DateTimeOfVisitLocationPoint { get; set; }
    public LocationPoint LocationPoint { get; set; }
}