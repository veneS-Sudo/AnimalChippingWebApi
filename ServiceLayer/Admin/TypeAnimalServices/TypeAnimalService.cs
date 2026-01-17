using DatabaseLayer.Context;
using DatabaseLayer.Models;
using DatabaseLayer.Validators;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ServiceLayer.Exceptions;

namespace ServiceLayer.Admin.TypeAnimalServices;

public class TypeAnimalService : ITypeAnimalService
{
    private readonly AppDbContext _context;
    private readonly IGenericValidator<AnimalType> _animalTypeValidator;
    private readonly ILogger<TypeAnimalService> _logger;

    public TypeAnimalService(
        AppDbContext context,
        IGenericValidator<AnimalType> animalTypeValidator, ILogger<TypeAnimalService> logger)
    {
        _context = context;
        _animalTypeValidator = animalTypeValidator;
        _logger = logger;
    }

    public AnimalTypeDto GetAnimalTypeById(long id)
    {
        CheckAnimalTypeExists(id);
        
        var animalType = _context.AnimalTypes
            .AsNoTracking()
            .Single(at => at.AnimalTypeId == id);
        _logger.LogInformation("Getting animal type with id {AnimalTypeId}", id);
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
        _logger.LogInformation("Adding new animal type {AnimalTypeId} {AnimalType}", newAnimalType.AnimalTypeId, newAnimalType.Type);
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
        _logger.LogInformation("Updated location with id {AnimalTypeId}", animalType.AnimalTypeId);
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
        _logger.LogInformation("Deleted location with id {AnimalTypeId}", id);
        return result > 0;
    }

    private void CheckAnimalTypeExists(long id)
    {
        _logger.LogInformation("Checking if animal type with id {AnimalTypeId} to exists", id);
        _animalTypeValidator.CheckWithThrowExceptions(new AnimalType() { AnimalTypeId = id },
            "Animal type not found");
    }
}