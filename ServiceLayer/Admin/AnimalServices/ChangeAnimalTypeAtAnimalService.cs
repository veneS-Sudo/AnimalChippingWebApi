using AnimalChippingWebApi.Core.Models;
using AnimalChippingWebApi.DataBaseLayer.Context;
using Microsoft.EntityFrameworkCore;
using ServiceLayer.Admin.Dto;

namespace ServiceLayer.Admin;

public class ChangeAnimalTypeAtAnimalService
{
    private readonly AppDbContext _context;

    public ChangeAnimalTypeAtAnimalService(AppDbContext context)
    {
        _context = context;
    }

    public Animal AddAnimalTypeAtAnimal(long animalId, long animalTypeId)
    {
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
        var animal = _context.Animals
            .Include(a => a.AnimalTypes)
            .Single(a => a.AnimalId == animalId);
        if (animal.AnimalTypes.All(t => t.AnimalTypeId != inAnimalDataDto.OldAnimalTypeId))
            throw new Exception($"The animal type {inAnimalDataDto.OldAnimalTypeId} does not exist");
        
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
        var animal = _context.Animals
            .Include(a => a.AnimalTypes)
            .Single(a => a.AnimalId == animalId);
        if (animal.AnimalTypes.All(t => t.AnimalTypeId != animalTypeId))
            throw new Exception($"The animal type with id {animalTypeId} does not exist");
        
        animal.AnimalTypes.Remove(animal.AnimalTypes.Single(t => t.AnimalTypeId == animalTypeId));
        _context.SaveChanges();
        return animal;
    }
}