using DatabaseLayer.Models;

namespace ServiceLayer.Admin.Dto;

public class UpdateDataAnimalDto : AnimalIndicators
{
    public GenderType Gender { get; set; }
    public LiveStatusType LiveStatus { get; set; }
    public int AccountId { get; set; }  // the chipper id account
    public long ChippingLocationPointId { get; set; }    // chipping location 
}