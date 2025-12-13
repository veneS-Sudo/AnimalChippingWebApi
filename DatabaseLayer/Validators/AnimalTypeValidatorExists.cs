using System.ComponentModel.DataAnnotations;
using AnimalChippingWebApi.Core.Models;
using AnimalChippingWebApi.DataBaseLayer.Context;
using Microsoft.EntityFrameworkCore;

namespace DatabaseLayer.Validators;

public class AnimalTypeValidatorExists : IGenericValidator<AnimalType>
{
    private readonly AppDbContext _context;

    public AnimalTypeValidatorExists(AppDbContext context)
    {
        _context = context;
    }

    public bool Check(AnimalType input)
    {
        var type = _context.AnimalTypes
            .AsNoTracking()
            .FirstOrDefault(x => x.AnimalTypeId == input.AnimalTypeId);
        return type is not null;
    }

    public void CheckWithThrowExceptions(AnimalType input, string message)
    {
        if (!Check(input))
            throw new ValidationException(message);
    }
}