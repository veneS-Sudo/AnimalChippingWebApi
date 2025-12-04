namespace AnimalChippingWebApi.Core.Models;

public class Account
{
    public int AccountId { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
    
    public ICollection<Animal> ChippedAnimals { get; set; }
}