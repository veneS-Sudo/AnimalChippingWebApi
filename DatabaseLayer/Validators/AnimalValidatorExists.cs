using DatabaseLayer.Context;
using DatabaseLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace DatabaseLayer.Validators;

public class AnimalValidatorExists : IGenericValidator<Animal>
{
    private readonly AppDbContext _context;

    public AnimalValidatorExists(AppDbContext context)
    {
        _context = context;
    }

    public bool Check(Animal input)
    {
        var animal = _context.Animals
            .AsNoTracking()
            .FirstOrDefault(a => a.AnimalId == input.AnimalId);
        return animal is not null;
    }

    public void CheckWithThrowExceptions(Animal input, string message)
    {
        var resultValidation = Check(input);
        if (!resultValidation)
            throw new ValidationExistsException(message);
    }
}