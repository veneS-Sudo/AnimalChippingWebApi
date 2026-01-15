using AnimalChippingWebApi.Models.Search;
using Microsoft.AspNetCore.Mvc;

namespace AnimalChippingWebApi.Models.VisitedLocation;

public class VisitedLocationSearchDto : SearchDateModel
{
    [FromRoute(Name = "animalId")]
    public long AnimalId { get; set; }
}