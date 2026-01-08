using System.Security.Claims;

namespace AnimalChippingWebApi.ValidationFilters;

public class OwnedAccountValidationFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var accountId = context.GetArgument<int>(0);
        var accountIdentity = context.HttpContext.User.Claims
            .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        if (accountIdentity is not null &&
            !accountIdentity.Value.Equals(accountId.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>{
                {nameof(accountId), new [] {"Trying to update not owned account"}}
            }, statusCode: 403);
        }

        return await next(context);
    }
}