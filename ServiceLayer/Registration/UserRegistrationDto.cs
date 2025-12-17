using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ServiceLayer.Registration;

public class UserRegistrationDto : IValidatableObject
{
    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } 
    
    [Required] 
    [StringLength(100)]
    public string LastName { get; set; }
    
    [Required]
    [EmailAddress]
    public required string Email { get; set; }
    
    [Required]
    [PasswordPropertyText]
    [StringLength(100, MinimumLength = 6)]
    public string Password { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(FirstName))
            yield return new ValidationResult("First name is required", new[] { nameof(FirstName) });
        if (string.IsNullOrWhiteSpace(LastName))
            yield return new ValidationResult("Last name is required", new[] { nameof(LastName) });
        if (string.IsNullOrWhiteSpace(Email))
            yield return new ValidationResult("Email is required", new[] { nameof(Email) });
        if (string.IsNullOrWhiteSpace(Password))
            yield return new ValidationResult("Password is required", new[] { nameof(Password) });
    }
}