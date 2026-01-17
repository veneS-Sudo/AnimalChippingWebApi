using DatabaseLayer.Context;
using DatabaseLayer.Models;
using DatabaseLayer.QueryObjects;
using DatabaseLayer.Validators;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ServiceLayer.Admin.AnimalServices.SearchPagination;
using ServiceLayer.Exceptions;

namespace ServiceLayer.Admin.AnimalServices;

public class AnimalService : IAnimalService
{
    private readonly AppDbContext _context;
    private readonly IGenericValidator<Animal> _animalValidator;
    private readonly IGenericValidator<LocationPoint> _locationPointValidator;
    private readonly IGenericValidator<Account> _accountValidator;
    private readonly ILogger<AnimalService> _logger;

    public AnimalService(AppDbContext context,
        IGenericValidator<Animal> animalValidator,
        IGenericValidator<LocationPoint> locationPointValidator,
        IGenericValidator<Account> accountValidator, ILogger<AnimalService> logger)
    {
        _context = context;
        _animalValidator = animalValidator;
        _locationPointValidator = locationPointValidator;
        _accountValidator = accountValidator;
        _logger = logger;
    }

    public Animal GetAnimalById(long animalId)
    { CheckAnimalIdWithThrowException(animalId);
        
        var animal = _context.Animals
            .AsNoTracking()
            .Include(a => a.ChippingLocationPoint)
            .Include(a => a.VisitedLocations)
            .Include(a => a.AnimalTypes)
            .Single(a => a.AnimalId == animalId);
        _logger.LogInformation("Getting animal by id {AnimalId}", animalId);
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

        _logger.LogInformation(
            "Getting animal by filter options: {ChipperId}, {ChippingLocationId}, {Gender}, {LiveStatus}, {StartDate}, {EndDate}",
            filterOptions.FilterValues.ChipperId,
            filterOptions.FilterValues.ChippingLocationId, filterOptions.FilterValues.Gender,
            filterOptions.FilterValues.LiveStatus, filterOptions.FilterValues.StartDate,
            filterOptions.FilterValues.EndDate);
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
        _logger.LogInformation("Added new animal with id {AnimalId} to database", newAnimal.AnimalId);
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
        _logger.LogInformation(
            "Updated animal with id {AnimalId}; new Data: {AccountId}, {LiveStatus}, {Gender}, {Weight}, {Length}, {Height}",
            animalId, dto.AccountId, dto.LiveStatus, dto.Gender, dto.Weight, dto.Length, dto.Height);
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
        
        _logger.LogInformation("Soft deleted animal with id {AnimalId}", animalId);
        return animal.SoftDeleted;
    }

    private void CheckAnimalIdWithThrowException(long animalId)
    {
        _logger.LogInformation("Checking animal with id {AnimalId} to exists", animalId);
        _animalValidator.CheckWithThrowExceptions(new Animal { AnimalId = animalId }, $"Animal {animalId} not found.");
    }

    private void CheckLocationPointIdWithThrowException(long locationPointId)
    {
        _logger.LogInformation("Checking location point with id {LocationPointId} to exists", locationPointId);
        _locationPointValidator.CheckWithThrowExceptions(
            new LocationPoint { LocationPointId = locationPointId }, $"Location {locationPointId} not found.");
    }

    private void CheckAccountWithThrowException(int accountId)
    {
        _logger.LogInformation("Checking account with {AccountId} to exists", accountId);
        _accountValidator.CheckWithThrowExceptions(new Account() {AccountId = accountId}, $"Account {accountId} not found.");
    }
}