using DatabaseLayer.Models;

namespace ServiceLayer.Admin.AnimalServices;

public class AddAnimalDto : AnimalIndicators
{
    public GenderType Gender { get; set; }
    public int AccountId { get; set; }  // the chipper id account
    public long ChippingLocationPointId { get; set; }    // chipping location 
    public ICollection<long> AnimalTypeIds { get; set; }
}