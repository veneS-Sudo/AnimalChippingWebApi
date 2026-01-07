using System.Reflection;

namespace AnimalChippingWebApi.ValidationFilters;

public class IdValidationFilterFactory
{
    internal static EndpointFilterDelegate ValidateIdFactory(
        EndpointFilterFactoryContext context,
        EndpointFilterDelegate next)
    {
        ParameterInfo[] parameters = context.MethodInfo.GetParameters();
        int? position = null;
        Type? type = null;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].Name?.Contains("Search") ?? false)
            {
                position = i;
                type = parameters[i].ParameterType;
                break;
            }
        }
        if (!position.HasValue || type is null)
            return next;

        return async (invocationContext) =>
        {
            var searchModelObj = invocationContext.Arguments[position.Value];
            var propNames = searchModelObj.GetType().GetProperties()
                .Where(p => p.Name.EndsWith("Id"));
            var problems = new Dictionary<string, string[]>();
            foreach (var prop in propNames)
            {
                var propValue = prop.GetValue(searchModelObj);
                if (propValue is null)
                    continue;
                
                var idValue = Convert.ToInt64(propValue);
                if (idValue <= 0)
                    problems.Add(prop.Name, new[] { $"{prop.Name} must be greater than zero" });
            }

            if (problems.Any())
                return Results.ValidationProblem(problems);

            return await next(invocationContext);
        };
    }
}