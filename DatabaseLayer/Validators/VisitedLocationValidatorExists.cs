using AnimalChippingWebApi.Core.Models;
using AnimalChippingWebApi.DataBaseLayer.Context;
using Microsoft.EntityFrameworkCore;

namespace DatabaseLayer.Validators;

public class VisitedLocationValidatorExists : IGenericValidator<AnimalVisitedLocation>
{
    private readonly AppDbContext _context;

    public VisitedLocationValidatorExists(AppDbContext context)
    {
        _context = context;
    }

    public bool Check(AnimalVisitedLocation input)
    {
        var vl = _context.AnimalVisitedLocations
            .AsNoTracking()
            .FirstOrDefault(x => x.AnimalId == input.AnimalId);
        return vl is not null;
    }

    public void CheckWithThrowExceptions(AnimalVisitedLocation input, string message)
    {
        if (!Check(input))
            throw new ValidationCheckException(message);
    }
}