using AnimalChippingWebApi.Core.Models;

namespace ServiceLayer.Admin.Dto;

public class AddAnimalDto
{
    
    public float Height { get; set; }
    public float Weight { get; set; }
    public float Length { get; set; }
    public GenderType Gender { get; set; }
    public LiveStatusType LiveStatus { get; set; }
    
    public int AccountId { get; set; }  // the chipper id account
    public DateTime ChippingDateTime { get; set; }
    
    // soft delete
    public bool SoftDeleted { get; set; }
    
    public long ChippingLocationPointId { get; set; }    // chipping location 
    public ICollection<long> AnimalTypeIds { get; set; }

    public AddAnimalDto()
    {
        LiveStatus = LiveStatusType.Alive;
        ChippingDateTime = DateTime.UtcNow;
    }
}