namespace AnimalChippingWebApi.Core.Models;

public record LocationPoint
{
    public long LocationPointId { get; set; }
    public required double Longitude { get; set; }
    public required double Latitude { get; set; }
    public bool SoftDeleted { get; set; }
}