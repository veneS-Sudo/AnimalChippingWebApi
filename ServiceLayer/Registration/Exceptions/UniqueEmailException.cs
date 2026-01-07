namespace ServiceLayer.Registration.Exceptions;

public class UniqueEmailException : Exception
{
    public UniqueEmailException(string message) : base(message)
    { }
}