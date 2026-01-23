using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ServiceLayer.Admin.TypeAnimalServices;

public class CreateUpdateAnimalTypeDto
{
    [Required]
    [JsonPropertyName("type")]
    public string Type { get; set; }
}