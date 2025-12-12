namespace DatabaseLayer.Validators;

public interface IGenericValidator<T> where T : class
{
    bool Check(T input);
    void CheckWithThrowExceptions(T input, string message);
}