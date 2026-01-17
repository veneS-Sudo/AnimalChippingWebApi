using ServiceLayer.Exceptions;

namespace AnimalChippingWebApi.Middlewares;

public class ExceptionUniqueValueHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionUniqueValueHandlerMiddleware> _logger;

    public ExceptionUniqueValueHandlerMiddleware(RequestDelegate next, ILogger<ExceptionUniqueValueHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (UniqueValueException e)
        {
            _logger.LogError(e, e.Message);
            await Results.Problem(e.Message, statusCode: 409).ExecuteAsync(context);
        }
    }
}