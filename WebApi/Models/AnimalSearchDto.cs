using Microsoft.AspNetCore.Mvc;

namespace AnimalChippingWebApi.Models;

public class AnimalSearchDto
{
    private int _from;
    private int _size;
    
    [FromQuery(Name = "startDateTime")]
    public string? StartDateTime { get; set; }
    
    [FromQuery(Name = "endDateTime")]
    public string? EndDateTime { get; set; }
    
    [FromQuery(Name = "chipperId")]
    public int? ChipperId { get; set; }
    
    [FromQuery(Name = "chipperLocationId")]
    public long? ChipperLocationId { get; set; }
    
    [FromQuery(Name = "lifeStatus")]
    public string? LifeStatus { get; set; }
    
    [FromQuery(Name = "gender")]
    public string? Gender { get; set; }

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