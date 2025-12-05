using AnimalChippingWebApi.Core.Models;

namespace ServiceLayer.Admin.Dto;

public class UpdateDataAnimalDto
{
    
    public float Height { get; set; }
    public float Weight { get; set; }
    public float Length { get; set; }
    public GenderType Gender { get; set; }
    public LiveStatusType LiveStatus { get; set; }
    public int AccountId { get; set; }  // the chipper id account
    public long ChippingLocationPointId { get; set; }    // chipping location 
}