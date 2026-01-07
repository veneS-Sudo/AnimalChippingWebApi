using ServiceLayer.Registration.Exceptions;

namespace AnimalChippingWebApi.Middlewares;

public class ExceptionUniqueEmailHandlerMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionUniqueEmailHandlerMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (UniqueEmailException e)
        {
            await Results.Problem(e.Message, statusCode: 409).ExecuteAsync(context);
        }
    }
}