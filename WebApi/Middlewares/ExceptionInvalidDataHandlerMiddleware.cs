
namespace AnimalChippingWebApi.Middlewares;

public class ExceptionInvalidDataHandlerMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionInvalidDataHandlerMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (InvalidOperationException e)
        {
            await Results.Problem(e.Message, statusCode: 400).ExecuteAsync(context);
        }
    }
}