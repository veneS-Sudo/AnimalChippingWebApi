using System.ComponentModel.DataAnnotations;
using DatabaseLayer.Models;

namespace ServiceLayer.Admin.Dto;

public class AnimalIndicators
{
    [Required]
    public float Height { get; set; }
    
    [Required]
    public float Weight { get; set; }
    
    [Required]
    public float Length { get; set; }
}