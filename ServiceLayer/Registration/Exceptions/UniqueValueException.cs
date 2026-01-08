namespace ServiceLayer.Registration.Exceptions;

public class UniqueValueException : Exception
{
    public UniqueValueException(string message) : base(message)
    { }
}