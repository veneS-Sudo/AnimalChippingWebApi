using Microsoft.AspNetCore.Mvc;

namespace AnimalChippingWebApi.Models;

public class VisitedLocationSearchDto : SearchDateModel
{
    [FromRoute(Name = "animalId")]
    public long AnimalId { get; set; }
}