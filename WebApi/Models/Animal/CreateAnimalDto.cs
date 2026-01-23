using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using ServiceLayer.Admin.AnimalServices;

namespace AnimalChippingWebApi.Models.Animal;

public class CreateAnimalDto : AnimalIndicators
{
    [Required]
    [JsonPropertyName("gender")]
    public string Gender { get; set; }
    
    [Required]
    [JsonPropertyName("chipperId")]
    public int AccountId { get; set; }  // the chipper id account
    
    [Required]
    [JsonPropertyName("chippingLocationId")]
    public long ChippingLocationPointId { get; set; }    // chipping location 
    
    [Required]
    [JsonPropertyName("animalTypes")]
    public ICollection<long> AnimalTypeIds { get; set; }
}