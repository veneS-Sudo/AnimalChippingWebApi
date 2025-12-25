using AnimalChippingWebApi.Core.Models;
using AnimalChippingWebApi.DataBaseLayer.Context;
using Microsoft.EntityFrameworkCore;

namespace DatabaseLayer.Validators;

public class LocationPointValidatorExists : IGenericValidator<LocationPoint>
{
    private readonly AppDbContext _context;

    public LocationPointValidatorExists(AppDbContext context)
    {
        _context = context;
    }

    public bool Check(LocationPoint input)
    {
        var point = _context.Locations
            .AsNoTracking()
            .FirstOrDefault(p => p.LocationPointId == input.LocationPointId);
        return point is not null;
    }

    public void CheckWithThrowExceptions(LocationPoint input, string message)
    {
        if (!Check(input))
            throw new ValidationExistsException(message);
    }
}