using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ServiceLayer.Admin.AnimalServices.AnimalTypeAtAnimal;

public class UpdateAnimalTypeInAnimalDataDto
{
    [Required]
    [JsonPropertyName("oldTypeId")]
    public long OldAnimalTypeId { get; set; }
    
    [Required]
    [JsonPropertyName("newTypeId")]
    public long NewAnimalTypeId { get; set; }
}