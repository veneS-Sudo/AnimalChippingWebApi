using System.Text.Json.Serialization;

namespace AnimalChippingWebApi.Models.Animal;

public class AnimalDto
{
    [JsonPropertyName("id")]
    public long AnimalId { get; set; }
    
    [JsonPropertyName("height")]
    public float Height { get; set; }
    
    [JsonPropertyName("weight")]
    public float Weight { get; set; }
    
    [JsonPropertyName("length")]
    public float Length { get; set; }
    
    [JsonPropertyName("gender")]
    public string Gender { get; set; }
    
    [JsonPropertyName("lifeStatus")]
    public string LiveStatus { get; set; }
    
    [JsonPropertyName("chipperId")]
    public int ChipperId { get; set; }  // the chipper id account
    
    [JsonPropertyName("chippingDateTime")]
    public string ChippingDateTime { get; set; }
    
    [JsonPropertyName("deathDateTime")]
    public string? DeathDateTime { get; set; }
    
    [JsonPropertyName("chippingLocationId")]
    public long ChippingLocationPointId { get; set; }    // chipping location 
    
    [JsonPropertyName("visitedLocations")]
    public ICollection<long>? VisitedLocations { get; set; }
    
    [JsonPropertyName("animalTypes")]
    public ICollection<long> AnimalTypes { get; set; }
}