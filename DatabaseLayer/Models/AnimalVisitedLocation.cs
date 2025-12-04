namespace AnimalChippingWebApi.Core.Models;

public class AnimalVisitedLocation
{
    public long Id { get; set; }
    public long AnimalId { get; set; }
    public DateTime DateTimeOfVisitLocationPoint { get; set; }
    public required LocationPoint LocationPoint { get; set; }
}