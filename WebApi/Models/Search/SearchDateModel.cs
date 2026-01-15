using Microsoft.AspNetCore.Mvc;

namespace AnimalChippingWebApi.Models.Search;

public abstract class SearchDateModel : SearchModel
{
    [FromQuery(Name = "startDateTime")]
    public string? StartDateTime { get; set; }
    
    [FromQuery(Name = "endDateTime")]
    public string? EndDateTime { get; set; }
}