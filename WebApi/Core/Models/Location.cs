namespace AnimalChippingWebApi.Core.Models;

public record LocationPoint
{
    public int Id { get; set; }
    public required double Longitude { get; set; }
    public required double Latitude { get; set; }
}