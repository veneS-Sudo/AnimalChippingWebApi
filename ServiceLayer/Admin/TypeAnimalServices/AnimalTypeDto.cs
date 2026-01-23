using System.Text.Json.Serialization;

namespace ServiceLayer.Admin.TypeAnimalServices;

public class AnimalTypeDto
{
    [JsonPropertyName("id")]
    public long AnimalTypeId { get; set; }
    
    [JsonPropertyName("type")]
    public string Type { get; set; }
}