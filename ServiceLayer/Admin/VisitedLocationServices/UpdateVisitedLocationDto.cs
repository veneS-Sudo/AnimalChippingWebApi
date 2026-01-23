using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ServiceLayer.Admin.VisitedLocationServices;

public class UpdateVisitedLocationDto
{
    [Required]
    [JsonPropertyName("visitedLocationPointId")]
    public long VisitedLocationId { get; set; }
    
    [Required]
    [JsonPropertyName("locationPointId")]
    public long LocationPointId { get; set; }
}