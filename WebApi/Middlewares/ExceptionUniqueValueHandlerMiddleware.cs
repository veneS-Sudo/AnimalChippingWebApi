using ServiceLayer.Registration.Exceptions;

namespace AnimalChippingWebApi.Middlewares;

public class ExceptionUniqueValueHandlerMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionUniqueValueHandlerMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (UniqueValueException e)
        {
            await Results.Problem(e.Message, statusCode: 409).ExecuteAsync(context);
        }
    }
}