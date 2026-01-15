namespace ServiceLayer.Exceptions;

public class UniqueValueException : Exception
{
    public UniqueValueException(string message) : base(message)
    { }
}