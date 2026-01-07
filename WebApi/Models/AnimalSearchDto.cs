using Microsoft.AspNetCore.Mvc;

namespace AnimalChippingWebApi.Models;

public class AnimalSearchDto : SearchDateModel
{
    [FromQuery(Name = "chipperId")]
    public int? ChipperId { get; set; }
    
    [FromQuery(Name = "chipperLocationId")]
    public long? ChipperLocationId { get; set; }
    
    [FromQuery(Name = "lifeStatus")]
    public string? LifeStatus { get; set; }
    
    [FromQuery(Name = "gender")]
    public string? Gender { get; set; }
}