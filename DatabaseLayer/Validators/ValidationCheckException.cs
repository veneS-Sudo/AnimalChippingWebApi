using System.Drawing;

namespace DatabaseLayer.Validators;

public class ValidationCheckException : Exception
{
    public ValidationCheckException(string message) : base(message)
    { }
}