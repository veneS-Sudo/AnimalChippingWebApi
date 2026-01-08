using System.ComponentModel.DataAnnotations;

namespace AnimalChippingWebApi.Models.Location;

public class LocationCreateUpdateDto
{
    [Range(-180d, 180d)]
    public double Longitude { get; set; }
    
    [Range(-90d, 90d)]
    public double Latitude { get; set; }
}