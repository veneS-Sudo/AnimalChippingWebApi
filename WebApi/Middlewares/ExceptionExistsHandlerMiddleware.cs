using DatabaseLayer.Validators;

namespace AnimalChippingWebApi.Middlewares;

public class ExceptionExistsHandlerMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionExistsHandlerMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next.Invoke(context);
        }
        catch (ValidationExistsException e)
        {
            await Results.Problem(e.Message, statusCode: 404).ExecuteAsync(context);
        }
    }
}