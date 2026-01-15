using ServiceLayer.Admin.AnimalServices;

namespace AnimalChippingWebApi.ValidationFilters.AnimalFilters;

public class BaseAnimalIndicatorFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var animal = context.GetArgument<AnimalIndicators>(0);
        var problems = new Dictionary<string, string[]>();
        if (animal.Weight <= 0)
            problems.Add(nameof(animal.Weight), new[] { "The weight must be greater than 0" });
        if (animal.Height <= 0)
            problems.Add(nameof(animal.Height), new[] { "The height must be greater than 0" });
        if (animal.Length <= 0)
            problems.Add(nameof(animal.Length), new[] { "The length must be greater than 0" });
        if (problems.Any())
            return  Results.ValidationProblem(problems);
        
        return await next(context);
    }
}