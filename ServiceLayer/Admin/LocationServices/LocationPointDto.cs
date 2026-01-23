using System.Text.Json.Serialization;

namespace ServiceLayer.Admin.LocationServices;

public class LocationPointDto
{
    [JsonPropertyName("id")]
    public long LocationId { get; set; }
    
    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }
    
    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }
}