namespace DatabaseLayer.Models;

public class AnimalType
{
    public long AnimalTypeId { get; set; }
    public string Type { get; set; }
    
    public ICollection<Animal> Animals { get; set; }
}