using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AnimalChippingWebApi.ValidationFilters;

internal class IdValidationFilterFactory
{
    internal static EndpointFilterDelegate ValidatePropertyIdFactory(
        EndpointFilterFactoryContext context,
        EndpointFilterDelegate next)
    {
        ParameterInfo[] parameters = context.MethodInfo.GetParameters();
        var positions = new List<int>();
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].CustomAttributes.Any(x => 
                    x.AttributeType == typeof(AsParametersAttribute) 
                    || x.AttributeType == typeof(FromBodyAttribute)))
            {
                positions.Add(i);
            }
        }
        if (!positions.Any())
            return next;

        return async (invocationContext) =>
        {
            var problems = new Dictionary<string, string[]>();
            foreach (var position in positions)
            {
                var searchModelObj = invocationContext.Arguments[position];
                var propNames = searchModelObj.GetType().GetProperties()
                    .Where(p => p.Name.EndsWith("Id"));
                foreach (var prop in propNames)
                {
                    var propValue = prop.GetValue(searchModelObj);
                    if (propValue is null)
                    {
                        if (prop.CustomAttributes.Any(a => a.AttributeType == typeof(RequiredAttribute)))
                        {
                            problems.Add(prop.Name, new[] { $"{prop.Name} is required" });
                        }
                        continue;
                    }

                    var idValue = Convert.ToInt64(propValue);
                    if (idValue <= 0)
                        problems.Add(prop.Name, new[] { $"{prop.Name} must be greater than zero" });
                }
            }

            if (problems.Any())
                return Results.ValidationProblem(problems);

            return await next(invocationContext);
        };
    }

    internal static EndpointFilterDelegate ValidateParameterIdFactory(
        EndpointFilterFactoryContext context,
        EndpointFilterDelegate next)
    {
        ParameterInfo[] parameters = context.MethodInfo.GetParameters();
        var positions = new List<int>();
        var names = new List<string>();
        for (int i = 0; i < parameters.Length; i++)
        {
            if ((parameters[i].ParameterType == typeof(int) || parameters[i].ParameterType == typeof(long))
                && parameters[i].Name!.EndsWith("Id"))
            {
                positions.Add(i);
                names.Add(parameters[i].Name ?? String.Empty);
            }
        }

        if (!positions.Any())
        {
            return next;
        }

        return async (invocationContext) =>
        {
            var problems = new Dictionary<string, string[]>();
            foreach (var pn in positions.Zip(names))
            {
                long id = invocationContext.Arguments[pn.First]?.GetType() == typeof(int)
                    ? invocationContext.GetArgument<int>(pn.First)
                    : invocationContext.GetArgument<long>(pn.First);
                if (id <= 0)
                    problems.Add(pn.Second,
                        new[] { $"{pn.Second} must be greater than zero" });
            }

            if (problems.Any())
                return Results.ValidationProblem(problems);

            return await next(invocationContext);
        };
    }
}