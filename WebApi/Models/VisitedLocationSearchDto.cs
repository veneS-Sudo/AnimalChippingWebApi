using Microsoft.AspNetCore.Mvc;

namespace AnimalChippingWebApi.Models;

public class VisitedLocationSearchDto
{
    private int _from;
    private int _size;
    
    [FromRoute(Name = "animalId")]
    public long AnimalId { get; set; }
    
    [FromQuery(Name = "startDateTime")]
    public string? StartDateTime { get; set; }
    
    [FromQuery(Name = "endDateTime")]
    public string? EndDateTime { get; set; }
    
    [FromQuery(Name = "from")]
    public int? From
    {
        get => _from;
        set => _from = value ?? 0;
    }

    [FromQuery(Name = "size")]
    public int? Size
    {
        get => _size;
        set => _size = value ?? 10;
    }
}