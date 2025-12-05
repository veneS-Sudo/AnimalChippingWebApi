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

    public Animal AddAnimalTypeAtAnimal(long animalId, AnimalType animalType)
    {
        var animal = _context.Animals
            .Include(a => a.AnimalTypes)
            .Single(a => a.AnimalId == animalId);
        if (animal.AnimalTypes.All(x => x.AnimalTypeId != animalType.AnimalTypeId))
        {
            animal.AnimalTypes.Add(animalType);
            _context.SaveChanges();
        }
        return animal;
    }

    public Animal UpdateAnimalTypeAtAnimal(long animalId, UpdateAnimalTypeDto dto)
    {
        var animal = _context.Animals
            .Include(a => a.AnimalTypes)
            .Single(a => a.AnimalId == animalId);
        if (animal.AnimalTypes.All(t => t.AnimalTypeId != dto.OldAnimalType.AnimalTypeId))
            throw new Exception($"The animal type {dto.OldAnimalType.AnimalTypeId} does not exist");
        
        using (var transaction = _context.Database.BeginTransaction())
        {
            animal.AnimalTypes.Remove(
                animal.AnimalTypes.Single(t => t.AnimalTypeId == dto.OldAnimalType.AnimalTypeId));
            animal.AnimalTypes.Add(dto.NewAnimalType);
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