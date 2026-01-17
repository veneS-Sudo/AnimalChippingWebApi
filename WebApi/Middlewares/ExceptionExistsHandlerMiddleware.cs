using DatabaseLayer.Validators;

namespace AnimalChippingWebApi.Middlewares;

public class ExceptionExistsHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionExistsHandlerMiddleware> _logger;

    public ExceptionExistsHandlerMiddleware(RequestDelegate next, ILogger<ExceptionExistsHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next.Invoke(context);
        }
        catch (ValidationExistsException e)
        {
            _logger.LogError(e, e.Message);
            await Results.Problem(e.Message, statusCode: 404).ExecuteAsync(context);
        }
    }
}