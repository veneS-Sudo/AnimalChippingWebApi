namespace ServiceLayer.Exceptions;

public class EntityByIdNotFoundException : Exception
{
    public EntityByIdNotFoundException(string message)
    : base(message)
    { }
}