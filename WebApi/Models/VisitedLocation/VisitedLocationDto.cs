using System.Text.Json.Serialization;

namespace AnimalChippingWebApi.Models.VisitedLocation;

public class VisitedLocationDto
{
    [JsonPropertyName("id")]
    public long VisitedLocationId { get; set; }
    
    [JsonPropertyName("dateTimeOfVisitLocationPoint")]
    public string DateTimeOfVisitedLocation { get; set; }
    
    [JsonPropertyName("locationPointId")]
    public long LocationPointId { get; set; }
}