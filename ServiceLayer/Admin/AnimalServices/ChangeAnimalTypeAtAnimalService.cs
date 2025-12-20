using AnimalChippingWebApi.Core.Models;
using AnimalChippingWebApi.DataBaseLayer.Context;
using DatabaseLayer.Validators;
using Microsoft.EntityFrameworkCore;
using ServiceLayer.Admin.Dto;

namespace ServiceLayer.Admin;

public class ChangeAnimalTypeAtAnimalService : IChangeAnimalTypeAtAnimalService
{
    private readonly AppDbContext _context;
    private readonly IGenericValidator<Animal> _animalValidator;
    private readonly IGenericValidator<AnimalType> _animalTypeValidator;

    public ChangeAnimalTypeAtAnimalService(
        AppDbContext context, 
        IGenericValidator<Animal> animalValidator,
        IGenericValidator<AnimalType> animalTypeValidator)
    {
        _context = context;
        _animalValidator = animalValidator;
        _animalTypeValidator = animalTypeValidator;
    }

    public Animal AddAnimalTypeAtAnimal(long animalId, long animalTypeId)
    {
        CheckAnimalIdWithThrowException(animalId);
        CheckAnimalTypeIdWithThrowException(animalTypeId);
        
        var animal = _context.Animals
            .Include(a => a.AnimalTypes)
            .Single(a => a.AnimalId == animalId);
        if (animal.AnimalTypes.Any(x => x.AnimalTypeId == animalTypeId))
            throw new Exception($"Animal type {animalTypeId} already exists");
        
        animal.AnimalTypes.Add(_context.AnimalTypes.Single(x => x.AnimalTypeId == animalTypeId));
        _context.SaveChanges();
        return animal;
    }

    public Animal UpdateAnimalTypeAtAnimal(long animalId, UpdateAnimalTypeInAnimalDataDto inAnimalDataDto)
    {
        CheckAnimalIdWithThrowException(animalId);
        CheckAnimalTypeIdWithThrowException(inAnimalDataDto.OldAnimalTypeId);
        CheckAnimalTypeIdWithThrowException(inAnimalDataDto.NewAnimalTypeId);
        
        var animal = _context.Animals
            .Include(a => a.AnimalTypes)
            .Single(a => a.AnimalId == animalId);
        
        if (animal.AnimalTypes.All(t => t.AnimalTypeId != inAnimalDataDto.OldAnimalTypeId))
            throw new Exception($"The animal type {inAnimalDataDto.OldAnimalTypeId} does not exist at animal {animalId}");
        if (animal.AnimalTypes.Any(t => t.AnimalTypeId == inAnimalDataDto.NewAnimalTypeId))
            throw new Exception($"The animal type {inAnimalDataDto.NewAnimalTypeId} already exists at animal {animalId}");
        
        using (var transaction = _context.Database.BeginTransaction())
        {
            // удаление старого типа и добавление нового
            animal.AnimalTypes.Remove(
                animal.AnimalTypes.Single(t => t.AnimalTypeId == inAnimalDataDto.OldAnimalTypeId));
            animal.AnimalTypes.Add(
                animal.AnimalTypes.Single(t => t.AnimalTypeId == inAnimalDataDto.NewAnimalTypeId));
        }
        _context.SaveChanges();
        return animal;
    }

    public Animal DeleteAnimalTypeAtAnimal(long animalId, long animalTypeId)
    {
        CheckAnimalIdWithThrowException(animalId);
        CheckAnimalTypeIdWithThrowException(animalTypeId);
        
        var animal = _context.Animals
            .Include(a => a.AnimalTypes)
            .Single(a => a.AnimalId == animalId);
        
        if (animal.AnimalTypes.All(t => t.AnimalTypeId != animalTypeId))
            throw new Exception($"The animal type with id {animalTypeId} does not exist");
        
        animal.AnimalTypes.Remove(animal.AnimalTypes.Single(t => t.AnimalTypeId == animalTypeId));
        _context.SaveChanges();
        return animal;
    }

    private void CheckAnimalIdWithThrowException(long animalId)
    {
        _animalValidator.CheckWithThrowExceptions(new Animal { AnimalId = animalId },
            $"Animal {animalId} not found.");
    }

    private void CheckAnimalTypeIdWithThrowException(long animalTypeId)
    {
        _animalTypeValidator.CheckWithThrowExceptions(new AnimalType { AnimalTypeId = animalTypeId },
            $"AnimalType {animalTypeId} not found.");
    }
}