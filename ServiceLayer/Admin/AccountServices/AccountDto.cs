using System.Text.Json.Serialization;

namespace ServiceLayer.Admin.AccountServices;

public class AccountDto
{
    [JsonPropertyName("id")]
    public int AccountId { get; set; }
    
    [JsonPropertyName("firstName")]
    public string FirstName { get; set; }
    
    [JsonPropertyName("lastName")]
    public string LastName { get; set; }
    
    [JsonPropertyName("email")]
    public string Email { get; set; }
}