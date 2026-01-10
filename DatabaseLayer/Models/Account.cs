namespace DatabaseLayer.Models;

public class Account
{
    public int AccountId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    
    public ICollection<Animal> ChippedAnimals { get; set; }
}