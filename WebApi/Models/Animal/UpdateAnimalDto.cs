using System.ComponentModel.DataAnnotations;
using ServiceLayer.Admin.Dto;

namespace AnimalChippingWebApi.Models.Animal;

public class UpdateAnimalDto : AnimalIndicators
{
    [Required]
    public string Gender { get; set; }
    
    [Required]
    public string LiveStatus { get; set; }
    
    [Required]
    public int AccountId { get; set; }  // the chipper id account
    
    [Required]
    public long ChippingLocationPointId { get; set; }    // chipping location 
}