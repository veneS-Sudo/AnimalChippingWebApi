using DatabaseLayer.Context;
using DatabaseLayer.Models;
using DatabaseLayer.Validators;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ServiceLayer.Exceptions;

namespace ServiceLayer.Admin.AnimalServices.AnimalTypeAtAnimal;

public class ChangeAnimalTypeAtAnimalService : IChangeAnimalTypeAtAnimalService
{
    private readonly AppDbContext _context;
    private readonly IGenericValidator<Animal> _animalValidator;
    private readonly IGenericValidator<AnimalType> _animalTypeValidator;
    private readonly ILogger<ChangeAnimalTypeAtAnimalService> _logger;

    public ChangeAnimalTypeAtAnimalService(
        AppDbContext context, 
        IGenericValidator<Animal> animalValidator,
        IGenericValidator<AnimalType> animalTypeValidator, ILogger<ChangeAnimalTypeAtAnimalService> logger)
    {
        _context = context;
        _animalValidator = animalValidator;
        _animalTypeValidator = animalTypeValidator;
        _logger = logger;
    }

    public long AddAnimalTypeAtAnimal(long animalId, long animalTypeId)
    {
        CheckAnimalIdWithThrowException(animalId);
        CheckAnimalTypeIdWithThrowException(animalTypeId);
        
        var animal = _context.Animals
            .Include(a => a.AnimalTypes)
            .Single(a => a.AnimalId == animalId);
        if (animal.AnimalTypes.Any(x => x.AnimalTypeId == animalTypeId))
            throw new UniqueValueException($"Animal type {animalTypeId} already exists");
        
        animal.AnimalTypes.Add(_context.AnimalTypes.Single(x => x.AnimalTypeId == animalTypeId));
        _context.SaveChanges();
        _logger.LogInformation("Added animal type {AnimalTypeId} to animal {AnimalId}", animalTypeId, animalId);
        return animal.AnimalId;
    }

    public long UpdateAnimalTypeAtAnimal(long animalId, UpdateAnimalTypeInAnimalDataDto inAnimalDataDto)
    {
        CheckAnimalIdWithThrowException(animalId);
        CheckAnimalTypeIdWithThrowException(inAnimalDataDto.OldAnimalTypeId);
        CheckAnimalTypeIdWithThrowException(inAnimalDataDto.NewAnimalTypeId);
        
        var animal = _context.Animals
            .Include(a => a.AnimalTypes)
            .Single(a => a.AnimalId == animalId);
        
        if (animal.AnimalTypes.All(t => t.AnimalTypeId != inAnimalDataDto.OldAnimalTypeId))
            throw new ValidationExistsException($"The animal type {inAnimalDataDto.OldAnimalTypeId} does not exist at animal {animalId}");
        if (animal.AnimalTypes.Any(t => t.AnimalTypeId == inAnimalDataDto.NewAnimalTypeId))
            throw new UniqueValueException($"The animal type {inAnimalDataDto.NewAnimalTypeId} already exists at animal {animalId}");
        
        using (var transaction = _context.Database.BeginTransaction())
        {
            // удаление старого типа и добавление нового
            animal.AnimalTypes.Remove(
                animal.AnimalTypes.Single(t => t.AnimalTypeId == inAnimalDataDto.OldAnimalTypeId));
            animal.AnimalTypes.Add(
                _context.AnimalTypes.Single(t => t.AnimalTypeId == inAnimalDataDto.NewAnimalTypeId));
        }

        _logger.LogInformation("Updated animal {AnimalId} type {AnimalTypeId} to type {AnimalTypeId}", animalId,
            inAnimalDataDto.OldAnimalTypeId, inAnimalDataDto.NewAnimalTypeId);
        _context.SaveChanges();
        return animal.AnimalId;
    }

    public long DeleteAnimalTypeAtAnimal(long animalId, long animalTypeId)
    {
        CheckAnimalIdWithThrowException(animalId);
        CheckAnimalTypeIdWithThrowException(animalTypeId);
        
        var animal = _context.Animals
            .Include(a => a.AnimalTypes)
            .Single(a => a.AnimalId == animalId);
        
        if (animal.AnimalTypes.All(t => t.AnimalTypeId != animalTypeId))
            throw new ValidationExistsException($"The animal type with id {animalTypeId} does not exist");
        if (animal.AnimalTypes.Count == 1)
            throw new InvalidOperationException($"This animal has only one type: {animalTypeId}");
        
        animal.AnimalTypes.Remove(animal.AnimalTypes.Single(t => t.AnimalTypeId == animalTypeId));
        _context.SaveChanges();
        _logger.LogInformation("Deleted animal type {AnimalTypeId} at animal {AnimalId}", animalTypeId, animalId);
        return animal.AnimalId;
    }

    private void CheckAnimalIdWithThrowException(long animalId)
    {
        _logger.LogInformation("Checking if animal with id {AnimalId} to exists", animalId);
        _animalValidator.CheckWithThrowExceptions(new Animal { AnimalId = animalId },
            $"Animal {animalId} not found.");
    }

    private void CheckAnimalTypeIdWithThrowException(long animalTypeId)
    {
        _logger.LogInformation("Checking if animal type with id {AnimalTypeId} to exists", animalTypeId);
        _animalTypeValidator.CheckWithThrowExceptions(new AnimalType { AnimalTypeId = animalTypeId },
            $"AnimalType {animalTypeId} not found.");
    }
}