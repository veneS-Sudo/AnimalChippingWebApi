namespace AnimalChippingWebApi.Middlewares;

public class ExceptionInvalidDataHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionInvalidDataHandlerMiddleware> _logger;

    public ExceptionInvalidDataHandlerMiddleware(RequestDelegate next, ILogger<ExceptionInvalidDataHandlerMiddleware> logger)
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
        catch (InvalidOperationException e)
        {
            _logger.LogError(e, e.Message);
            await Results.Problem(e.Message, statusCode: 400).ExecuteAsync(context);
        }
    }
}