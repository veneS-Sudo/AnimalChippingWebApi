using AnimalChippingWebApi.DataBaseLayer.Context;
using DatabaseLayer.Models;
using DatabaseLayer.QueryObjects;
using DatabaseLayer.Validators;
using Microsoft.EntityFrameworkCore;
using ServiceLayer.Admin.Dto;
using ServiceLayer.Registration.Exceptions;

namespace ServiceLayer.Admin.AnimalServices;

public class AnimalService : IAnimalService
{
    private readonly AppDbContext _context;
    private readonly IGenericValidator<Animal> _animalValidator;
    private readonly IGenericValidator<LocationPoint> _locationPointValidator;
    private readonly IGenericValidator<Account> _accountValidator;

    public AnimalService(AppDbContext context,
        IGenericValidator<Animal> animalValidator,
        IGenericValidator<LocationPoint> locationPointValidator,
        IGenericValidator<Account> accountValidator)
    {
        _context = context;
        _animalValidator = animalValidator;
        _locationPointValidator = locationPointValidator;
        _accountValidator = accountValidator;
    }

    public Animal GetAnimalById(long animalId)
    { CheckAnimalIdWithThrowException(animalId);
        
        var animal = _context.Animals
            .AsNoTracking()
            .Include(a => a.ChippingLocationPoint)
            .Include(a => a.VisitedLocations)
            .Include(a => a.AnimalTypes)
            .Single(a => a.AnimalId == animalId);
        return animal;
    }

    public IQueryable<Animal> GetAnimals(AnimalSortFilterPageOptions filterOptions)
    {
        var query = _context.Animals
            .AsNoTracking()
            .Include(a => a.ChippingLocationPoint)
            .Include(a => a.VisitedLocations)
            .Include(a => a.AnimalTypes)
            .FilterAnimalsBy(filterOptions.FilterValues)
            .OrderBy(animal => animal.AnimalId);
        
        filterOptions.SetupRest(query);
        
        return query.Page(filterOptions.PageNum-1, filterOptions.PageSize);
    }
    
    public long AddNewAnimal(AddAnimalDto dto)
    {
        if (dto.AnimalTypeIds.Count != dto.AnimalTypeIds.Distinct().Count())            // AnimalTypes содержит дубликаты
            throw new UniqueValueException("Duplicate Animal type ids");
        CheckLocationPointIdWithThrowException(dto.ChippingLocationPointId);
        CheckAccountWithThrowException(dto.AccountId);
        if (dto.AnimalTypeIds.Count == 0)
            throw new InvalidOperationException("No animal type selected");
        
        var lp = _context.Locations
            .Single(l => l.LocationPointId == dto.ChippingLocationPointId);
        var animalTypes = _context.AnimalTypes
            .Where(at => dto.AnimalTypeIds.Any(x => x == at.AnimalTypeId)).ToList();

        if (animalTypes.Count != dto.AnimalTypeIds.Count)
            throw new ValidationExistsException(
                $"Animal types(ids: {dto.AnimalTypeIds.Except(animalTypes.Select(at => at.AnimalTypeId))}) not found");

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
        return newAnimal.AnimalId;
    }
    
    public bool UpdateDataAnimal(long animalId, UpdateDataAnimalDto dto)
    {
        CheckAnimalIdWithThrowException(animalId);
        CheckLocationPointIdWithThrowException(dto.ChippingLocationPointId);
        CheckLocationPointIdWithThrowException(dto.AccountId);
        
        var animal = _context.Animals
            .Include(a => a.VisitedLocations)
            .ThenInclude(vl => vl.LocationPoint)
            .Single(a => a.AnimalId == animalId);
        var lp = _context.Locations
            .Single(l => l.LocationPointId == dto.ChippingLocationPointId);
        
        // Checking
        if (animal.LiveStatus == LiveStatusType.Dead && dto.LiveStatus == LiveStatusType.Alive)
            throw new InvalidOperationException("Animal is dead");
        if (animal.VisitedLocations.Any() && animal.VisitedLocations.First().LocationPoint.LocationPointId == dto.ChippingLocationPointId)
            throw new InvalidOperationException("Chipping location don't equals first visited location point");
        
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
        CheckAnimalIdWithThrowException(animalId);
        
        var animal = _context.Animals
            .Include(a => a.ChippingLocationPoint)
            .Include(a => a.VisitedLocations)
            .ThenInclude(vl => vl.LocationPoint)
            .Single(a => a.AnimalId == animalId);
        
        if (animal.VisitedLocations.Any(vl =>
                vl.LocationPoint.LocationPointId != animal.ChippingLocationPoint.LocationPointId)
            && animal.VisitedLocations.Last().LocationPoint.LocationPointId !=
            animal.ChippingLocationPoint.LocationPointId)
            throw new InvalidOperationException(
                $"Animal {animal.AnimalId} leave chipping point, herewith exists other location points");
        
        animal.SoftDeleted = true;
        _context.SaveChanges();
        
        return animal.SoftDeleted;
    }

    private void CheckAnimalIdWithThrowException(long animalId)
    {
        _animalValidator.CheckWithThrowExceptions(new Animal { AnimalId = animalId }, $"Animal {animalId} not found.");
    }

    private void CheckLocationPointIdWithThrowException(long locationPointId)
    {
        _locationPointValidator.CheckWithThrowExceptions(
            new LocationPoint { LocationPointId = locationPointId }, $"Location {locationPointId} not found.");
    }

    private void CheckAccountWithThrowException(int accountId)
    {
        _accountValidator.CheckWithThrowExceptions(new Account() {AccountId = accountId}, $"Account {accountId} not found.");
    }
}