using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using ServiceLayer.Admin.AnimalServices;

namespace AnimalChippingWebApi.Models.Animal;

public class UpdateAnimalDto : AnimalIndicators
{
    [JsonPropertyName("gender")]
    public string Gender { get; set; }
    
    [JsonPropertyName("lifeStatus")]
    public string LiveStatus { get; set; }
    
    [JsonPropertyName("chipperId")]
    public int AccountId { get; set; }  // the chipper id account
    
    [JsonPropertyName("chippingLocationId")]
    public long ChippingLocationPointId { get; set; }    // chipping location 
}