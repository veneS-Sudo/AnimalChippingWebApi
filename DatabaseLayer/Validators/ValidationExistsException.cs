using System.Drawing;

namespace DatabaseLayer.Validators;

public class ValidationExistsException : Exception
{
    public ValidationExistsException(string message) : base(message)
    { }
}