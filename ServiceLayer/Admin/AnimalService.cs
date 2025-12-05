using AnimalChippingWebApi.Core.Models;
using AnimalChippingWebApi.DataBaseLayer.Context;
using Microsoft.EntityFrameworkCore;
using ServiceLayer.Admin.Dto;

namespace ServiceLayer.Admin;

public class AnimalService
{
    private readonly AppDbContext _context;

    public AnimalService(AppDbContext context)
    {
        _context = context;
    }

    public Animal GetAnimal(int id)
    {
        var animal = _context.Animals.AsNoTracking().Single(a => a.AnimalId == id);
        return animal;
    }

    public IQueryable<Animal> GetAnimalsWithoutTracking()
    {
        return _context.Animals.AsNoTracking();
    }
    
    public bool AddNewAnimal(AddAnimalDto dto)
    {
        var lp = _context.Locations.AsNoTracking()
            .Single(l => l.LocationPointId == dto.ChippingLocationPointId);
        var animalTypes = _context.AnimalTypes.AsNoTracking()
            .Where(at => dto.AnimalTypeIds.Any(x => x == at.AnimalTypeId)).ToList();

        var newAnimal = new Animal
        {
            Weight = dto.Weight,
            Length = dto.Length,
            Height = dto.Height,
            Gender = dto.Gender,
            AccountId = dto.AccountId,
            ChippingLocationPoint = lp,
            AnimalTypes = animalTypes
        };
        _context.Animals.Add(newAnimal);    
        _context.SaveChanges();
        return true;
    }
    
    public bool UpdateDataAnimals(long animalId, UpdateDataAnimalDto dto)
    {
        var animal = _context.Animals.Single(a => a.AnimalId == animalId);
        var lp = _context.Locations
            .Single(l => l.LocationPointId == dto.ChippingLocationPointId);
        
        animal.Weight = dto.Weight;
        animal.Length = dto.Length;
        animal.Height = dto.Height;
        animal.Gender = dto.Gender;
        animal.LiveStatus = dto.LiveStatus;
        animal.AccountId = dto.AccountId;
        animal.ChippingLocationPoint = lp;
        
        var result = _context.SaveChanges();
        return result > 0;
    }
    
    public bool SoftDelete(long animalId)
    {
        var animal = _context.Animals.Single(a => a.AnimalId == animalId);
        animal.SoftDeleted = true;
        _context.SaveChanges();
        return animal.SoftDeleted;
    }
}