using AnimalChippingWebApi.Models.Search;
using Microsoft.AspNetCore.Mvc;

namespace AnimalChippingWebApi.Models.Animal;

public class AnimalSearchDto : SearchDateModel
{
    [FromQuery(Name = "chipperId")]
    public int? ChipperId { get; set; }
    
    [FromQuery(Name = "chippingLocationId")]
    public long? ChippingLocationId { get; set; }
    
    [FromQuery(Name = "lifeStatus")]
    public string? LifeStatus { get; set; }
    
    [FromQuery(Name = "gender")]
    public string? Gender { get; set; }
}