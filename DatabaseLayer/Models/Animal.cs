namespace DatabaseLayer.Models;

public class Animal
{
    public long AnimalId { get; set; }
    public float Height { get; set; }
    public float Weight { get; set; }
    public float Length { get; set; }
    public GenderType Gender { get; set; }
    public LiveStatusType LiveStatus { get; set; }
    
    public int AccountId { get; set; }  // the chipper id account
    public DateTime ChippingDateTime { get; set; }
    public DateTime? DeathDateTime { get; set; }
    
    // soft delete
    public bool SoftDeleted { get; set; }
    
    public LocationPoint ChippingLocationPoint { get; set; }    // chipping location 
    public ICollection<AnimalVisitedLocation> VisitedLocations { get; set; }
    public ICollection<AnimalType> AnimalTypes { get; set; }

    public Animal()
    {
        ChippingDateTime = DateTime.UtcNow;
        LiveStatus = LiveStatusType.Alive;
        Gender = GenderType.Other;
        DeathDateTime = null;
    }

    public void SetDeathStatus()
    {
        LiveStatus = LiveStatusType.Dead;
        if (DeathDateTime == null)
            DeathDateTime = DateTime.UtcNow;
    }
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