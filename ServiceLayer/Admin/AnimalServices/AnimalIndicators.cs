using System.ComponentModel.DataAnnotations;

namespace ServiceLayer.Admin.AnimalServices;

public class AnimalIndicators
{
    [Required]
    public float Height { get; set; }
    
    [Required]
    public float Weight { get; set; }
    
    [Required]
    public float Length { get; set; }
}