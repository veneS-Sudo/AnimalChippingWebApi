using AnimalChippingWebApi.Core.Models;
using AnimalChippingWebApi.DataBaseLayer.Context;
using DatabaseLayer.Validators;
using Microsoft.EntityFrameworkCore;
using ServiceLayer.Admin.Dto;
using ServiceLayer.Registration.Exceptions;

namespace ServiceLayer.Admin.TypeAnimalServices;

public class TypeAnimalService : ITypeAnimalService
{
    private readonly AppDbContext _context;
    private readonly IGenericValidator<AnimalType> _animalTypeValidator;

    public TypeAnimalService(
        AppDbContext context,
        IGenericValidator<AnimalType> animalTypeValidator)
    {
        _context = context;
        _animalTypeValidator = animalTypeValidator;
    }

    public AnimalTypeDto GetAnimalTypeById(long id)
    {
        CheckAnimalTypeExists(id);
        
        var animalType = _context.AnimalTypes
            .AsNoTracking()
            .Single(at => at.AnimalTypeId == id);
        return animalType.ToDto();
    }

    public long AddAnimalType(CreateUpdateAnimalTypeDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Type))
            throw new InvalidOperationException($"Type {dto.Type} is required");
        if (_context.AnimalTypes.Any(at => EF.Functions.Like(dto.Type, at.Type)))
            throw new UniqueValueException($"Type with name {dto.Type} already exists");
        
        var newAnimalType = new AnimalType
        {
            Type = dto.Type,
        };
        _context.AnimalTypes.Add(newAnimalType);
        _context.SaveChanges();
        return newAnimalType.AnimalTypeId;
    }

    public bool UpdateAnimalType(AnimalType animalType)
    {
        CheckAnimalTypeExists(animalType.AnimalTypeId);
        if (string.IsNullOrWhiteSpace(animalType.Type))
            throw new InvalidOperationException($"Type {animalType.Type} is required");
        if (_context.AnimalTypes.Any(at => at.AnimalTypeId != animalType.AnimalTypeId &&
                                           EF.Functions.Like(animalType.Type, at.Type)))
            throw new UniqueValueException($"Type with name {animalType.Type} already exists");
        
        var oldAnimalType = _context.AnimalTypes
            .Single(at => at.AnimalTypeId == animalType.AnimalTypeId);
        oldAnimalType.Type = animalType.Type;
        var result = _context.SaveChanges();
        return result > 0;
    }

    public bool DeleteAnimalType(long id)
    {
        CheckAnimalTypeExists(id);
        if (_context.AnimalTypes
            .AsNoTracking()
            .Include(t => t.Animals)
            .Single(at => at.AnimalTypeId == id)
            .Animals.Any())
            throw new InvalidOperationException($"There are animals with a type with {id}");
        
        var animalType = _context.AnimalTypes.Single(at => at.AnimalTypeId == id);
        _context.AnimalTypes.Remove(animalType);
        var result = _context.SaveChanges();
        return result > 0;
    }

    private void CheckAnimalTypeExists(long id)
    {
        _animalTypeValidator.CheckWithThrowExceptions(new AnimalType() { AnimalTypeId = id },
            "Animal type not found");
    }
}