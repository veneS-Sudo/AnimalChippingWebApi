namespace AnimalChippingWebApi.ValidationFilters;

public class IdValidationFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        long id = context.Arguments[0]?.GetType() == typeof(int) ? context.GetArgument<int>(0) : context.GetArgument<long>(0);
        if (id <= 0)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                {"id", new string[] { "Id must be greater than 0" }}
            });
        }
        return await next(context);
    }
}