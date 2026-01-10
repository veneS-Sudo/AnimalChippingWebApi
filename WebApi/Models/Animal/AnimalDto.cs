namespace AnimalChippingWebApi.Models.Animal;

public class AnimalDto
{
    public long AnimalId { get; set; }
    public float Height { get; set; }
    public float Weight { get; set; }
    public float Length { get; set; }
    public string Gender { get; set; }
    public string LiveStatus { get; set; }
    
    public int ChipperId { get; set; }  // the chipper id account
    public string ChippingDateTime { get; set; }
    public string? DeathDateTime { get; set; }
    
    public long ChippingLocationPointId { get; set; }    // chipping location 
    public ICollection<long>? VisitedLocations { get; set; }
    public ICollection<long> AnimalTypes { get; set; }
}