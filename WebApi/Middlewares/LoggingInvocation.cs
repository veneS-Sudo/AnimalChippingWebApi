using System.Security.Claims;

namespace AnimalChippingWebApi.Middlewares;

public class LoggingInvocation
{
    private readonly RequestDelegate _next;
    private readonly ILogger<LoggingInvocation> _logger;

    public LoggingInvocation(RequestDelegate next, ILogger<LoggingInvocation> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        _logger.LogInformation("Beginning invocation of {Method} for path {Path} by {AccountId}", context.Request.Method,
            context.Request.Path,
            context.User.Claims.SingleOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value ?? "Anonymous");
        await _next(context);
    }
}