namespace AnimalChippingWebApi.Core.Models;

public class Animal
{
    public long Id { get; set; }
    public IList<AnimalType> AnimalTypes { get; set; } = new List<AnimalType>();
    public float Height { get; set; }
    public float Weight { get; set; }
    public float Length { get; set; }
    public GenderType Gender { get; set; }
    public LiveStatusType LiveStatus { get; set; }
    
    public int ChipperId { get; set; }  // the chipper id account
    public DateTime ChippingDateTime { get; set; }
    public long ChippingLocationId { get; set; }    // chipping location 
    public IList<AnimalVisitedLocation> VisitedLocations { get; set; } = new List<AnimalVisitedLocation>();
    public DateTime DeathDateTime { get; set; }
}

public enum GenderType
{
    Male,
    Female,
    Other
}

public enum LiveStatusType
{
    Alive,
    Dead
}