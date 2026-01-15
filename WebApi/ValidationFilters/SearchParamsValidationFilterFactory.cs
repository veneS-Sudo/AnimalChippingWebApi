using System.Globalization;
using System.Reflection;
using AnimalChippingWebApi.Models.Search;

namespace AnimalChippingWebApi.ValidationFilters;

public class SearchParamsValidationFilterFactory
{
    internal static EndpointFilterDelegate ValidateSearchPaginationParamsFactory(
        EndpointFilterFactoryContext context,
        EndpointFilterDelegate next)
    {
        ParameterInfo[] parameters = context.MethodInfo.GetParameters();
        int? searchParamsPosition = null;
        Type? searchParamsType = null;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].ParameterType.BaseType == typeof(SearchModel) ||
                parameters[i].ParameterType.BaseType?.BaseType == typeof(SearchModel))
            {
                searchParamsPosition = i;
                searchParamsType = parameters[i].ParameterType;
                break;
            }
        }

        if (!searchParamsPosition.HasValue || searchParamsType is null)
            return next;

        return async (invocationContext) =>
        {
            var searchModel = invocationContext.GetArgument<SearchModel>(searchParamsPosition.Value);

            if (searchModel.From < 0 || searchModel.Size <= 0)
            {
                var problems = new Dictionary<string, string[]>();
                if (searchModel.From < 0)
                    problems.Add("from", new[] { "'From' must be greater or equal than 0" });
                if (searchModel.Size <= 0)
                    problems.Add("size", new[] { "'Size' must be greater than 0" });
                return Results.ValidationProblem(problems);
            }

            return await next(invocationContext);
        };
    }

    internal static EndpointFilterDelegate ValidateSearchDateParamsFactory(
        EndpointFilterFactoryContext context,
        EndpointFilterDelegate next)
    {
        ParameterInfo[] parameters = context.MethodInfo.GetParameters();
        int? searchParamsPosition = null;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].ParameterType.BaseType == typeof(SearchDateModel))
            {
                searchParamsPosition = i;
                break;
            }
        }
        
        if (!searchParamsPosition.HasValue)
            return next;

        return async (invocationContext) =>
        {
            var searchModel = invocationContext.GetArgument<SearchDateModel>(searchParamsPosition.Value);
            var problems = new Dictionary<string, string[]>();
            if (!DateTime.TryParseExact(searchModel.StartDateTime, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var startDateTimeParsed) && searchModel.StartDateTime is not null)
                problems.Add("startDateTime", new[] { "'StartDateTime' must be in a format ISO-8601" });
            if (!DateTime.TryParseExact(searchModel.EndDateTime, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var endDateTimeParsed) && searchModel.EndDateTime is not null)
                problems.Add("endDateTime", new[] { "'EndDateTime' must be in a format ISO-8601" });
            if (problems.Any())
                return Results.ValidationProblem(problems);

            return await next(invocationContext);
        };
    }
}