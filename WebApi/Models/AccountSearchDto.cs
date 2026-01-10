using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace AnimalChippingWebApi.Models; 
public class AccountSearchDto : SearchModel
{
    [FromQuery(Name = "email")]
     public string? Email { get; set; }
     
    [FromQuery(Name = "firstName")]
    public string? FirstName { get; set; }
    
    [FromQuery(Name = "lastName")]
    public string? LastName { get; set; }
    
    
}