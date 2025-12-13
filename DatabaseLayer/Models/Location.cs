namespace AnimalChippingWebApi.Core.Models;

public record LocationPoint
{
    public long LocationPointId { get; set; }
    public double Longitude { get; set; }
    public double Latitude { get; set; }
    
    public ICollection<Animal> Animals { get; set; }
    public ICollection<AnimalVisitedLocation> AnimalVisitedLocations { get; set; }
}