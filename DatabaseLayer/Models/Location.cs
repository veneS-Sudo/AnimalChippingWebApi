using System.ComponentModel.DataAnnotations;

namespace DatabaseLayer.Models;

public record LocationPoint
{
    public long LocationPointId { get; set; }
    
    [Range(-180d, 180d)]
    public double Longitude { get; set; }
    
    [Range(-90d, 90d)]
    public double Latitude { get; set; }
    
    public ICollection<Animal> Animals { get; set; }
    public ICollection<AnimalVisitedLocation> AnimalVisitedLocations { get; set; }
}