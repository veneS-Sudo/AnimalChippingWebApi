using AnimalChippingWebApi.Core.Models;
using AnimalChippingWebApi.DataBaseLayer.Context;
using Microsoft.EntityFrameworkCore;
using ServiceLayer.Admin.Dto;

namespace ServiceLayer.Admin.TypeAnimalServices;

public class TypeAnimalService
{
    private readonly AppDbContext _context;

    public TypeAnimalService(AppDbContext context)
    {
        _context = context;
    }

    public AnimalType GetAnimalTypeById(long id)
    {
        var animalType = _context.AnimalTypes.AsNoTracking()
            .Single(at => at.AnimalTypeId == id);
        return animalType;
    }

    public bool AddAnimalType(AddAnimalTypeDto dto)
    {
        if (_context.AnimalTypes.Any(at => at.Type.Equals(dto.Type, StringComparison.CurrentCultureIgnoreCase)))
            throw new Exception($"Type with name {dto.Type} already exists");
        
        var newAnimalType = new AnimalType
        {
            Type = dto.Type,
        };
        _context.AnimalTypes.Add(newAnimalType);
        _context.SaveChanges();
        return true;
    }

    public bool UpdateAnimalType(AnimalType animalType)
    {
        var oldAnimalType = _context.AnimalTypes.Single(at => at.AnimalTypeId == animalType.AnimalTypeId);
        oldAnimalType.Type = animalType.Type;
        _context.SaveChanges();
        return true;
    }

    public bool SoftDeleteAnimalType(long id)
    {
        var animalType = _context.AnimalTypes.Single(at => at.AnimalTypeId == id);
        _context.AnimalTypes.Remove(animalType);
        var result = _context.SaveChanges();
        return result > 0;
    }
}