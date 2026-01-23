using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ServiceLayer.Admin.AnimalServices;

public class AnimalIndicators
{
    [JsonPropertyName("height")]
    public float Height { get; set; }
    
    [JsonPropertyName("weight")]
    public float Weight { get; set; }
    
    [JsonPropertyName("length")]
    public float Length { get; set; }
}