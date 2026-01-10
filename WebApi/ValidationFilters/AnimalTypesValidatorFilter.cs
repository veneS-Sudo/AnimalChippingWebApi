using AnimalChippingWebApi.Models.Animal;

namespace AnimalChippingWebApi.ValidationFilters;

public class AnimalTypesValidatorFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var dto = context.GetArgument<CreateAnimalDto>(0);
        if (dto.AnimalTypeIds.Any(at => at <= 0) || dto.AnimalTypeIds.Count <= 0)
            return Results.ValidationProblem(new Dictionary<string, string[]>()
            {
                { nameof(dto.AnimalTypeIds), new[] { "animal type ids must be greater than 0 and not empty." } }
            });
        return await next(context);
    }
}