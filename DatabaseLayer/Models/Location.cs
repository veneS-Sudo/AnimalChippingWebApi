namespace AnimalChippingWebApi.Core.Models;

public record LocationPoint
{
    public int LocationPointId { get; set; }
    public required double Longitude { get; set; }
    public required double Latitude { get; set; }
    public bool SoftDeleted { get; set; }
}