using AnimalChippingWebApi.Models.Search;
using Microsoft.AspNetCore.Mvc;

namespace AnimalChippingWebApi.Models.Account; 
public class AccountSearchDto : SearchModel
{
    [FromQuery(Name = "email")]
     public string? Email { get; set; }
     
    [FromQuery(Name = "firstName")]
    public string? FirstName { get; set; }
    
    [FromQuery(Name = "lastName")]
    public string? LastName { get; set; }
    
    
}