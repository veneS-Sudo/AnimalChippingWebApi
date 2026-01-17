using DatabaseLayer.Context;
using DatabaseLayer.Models;
using DatabaseLayer.QueryObjects;
using DatabaseLayer.Validators;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ServiceLayer.Admin.VisitedLocationServices.SearchPagination;

namespace ServiceLayer.Admin.VisitedLocationServices;

public class VisitedLocationAnimalService : IVisitedLocationAnimalService
{
    private readonly AppDbContext _context;
    private readonly IGenericValidator<Animal> _animalValidator;
    private readonly IGenericValidator<LocationPoint> _locationPointValidator;
    private readonly IGenericValidator<AnimalVisitedLocation> _visitedLocationValidator;
    private readonly ILogger<VisitedLocationAnimalService> _logger;

    public VisitedLocationAnimalService(
        AppDbContext context, 
        IGenericValidator<Animal> validator, 
        IGenericValidator<LocationPoint> locationPointValidator,
        IGenericValidator<AnimalVisitedLocation> visitedLocationValidator, ILogger<VisitedLocationAnimalService> logger)
    {
        _context = context;
        _animalValidator = validator;
        _locationPointValidator = locationPointValidator;
        _visitedLocationValidator = visitedLocationValidator;
        _logger = logger;
    }

    public AnimalVisitedLocation GetAnimalVisitedLocationById(long animalId, long visitedLocationId)
    {
        CheckAnimalIdWithThrowException(animalId);
        CheckVisitedLocationPointWithThrowException(visitedLocationId);

        var visitedLocation = _context.Animals
            .AsNoTracking()
            .Include(a => a.VisitedLocations)
            .ThenInclude(vl => vl.LocationPoint)
            .First(a => a.AnimalId == animalId).VisitedLocations
            .Single(vl => vl.AnimalVisitedLocationId == visitedLocationId);
        _logger.LogInformation("Getting visited location with id {VisitedLocationId} by animal {AnimalId}",
            visitedLocationId, animalId);
        return visitedLocation;
    }
    
    public IQueryable<AnimalVisitedLocation> GetAnimalVisitedLocationsById(
        long animalId, 
        LocationVisitedSortFilterOptions sortFilterOptions)
    {
        CheckAnimalIdWithThrowException(animalId);
        
        var query = _context.Animals
            .AsNoTracking()
            .Include(a => a.VisitedLocations)
            .ThenInclude(vl => vl.LocationPoint)
            .Where(a => a.AnimalId == animalId)
            .SelectMany(a => a.VisitedLocations)
            .FilterLocationBy(sortFilterOptions.VisitedLocationFilterBy, sortFilterOptions.FilterValue)
            .OrderBy(vl => vl.DateTimeOfVisitLocationPoint);
        
        sortFilterOptions.SetupRest(query);

        _logger.LogInformation(
            "Getting visited locations by animal {AnimalId} with search options: {SearchFilterValue}", animalId,
            sortFilterOptions.FilterValue);
        return query.Page(sortFilterOptions.PageNum-1, sortFilterOptions.PageSize);
    }

    public AnimalVisitedLocation AddVisitedLocationToAnimal(long animalId, long locationId)
    {
        CheckAnimalIdWithThrowException(animalId);
        CheckLocationPointWithThrowException(locationId);
        
        var animal = _context.Animals
            .Include(a => a.ChippingLocationPoint)
            .Include(a => a.VisitedLocations)
            .Single(a => a.AnimalId == animalId);
        _context.Entry(animal).Collection(a => a.VisitedLocations).Load();
        foreach (var visitedLocationEntry in animal.VisitedLocations)
        {
            _context.Entry(visitedLocationEntry).Reference(vl => vl.LocationPoint).Load();
        }
        if (animal.LiveStatus == LiveStatusType.Dead)
            throw new InvalidOperationException("Animal is dead");
        
        var point = _context.Locations
            .Single(p => p.LocationPointId == locationId);
        if (!animal.VisitedLocations.Any() && point.LocationPointId == animal.ChippingLocationPoint.LocationPointId)
            throw new InvalidOperationException("Location point is already added");
        if (point.LocationPointId == animal.VisitedLocations.LastOrDefault()?.LocationPoint.LocationPointId)
            throw new InvalidOperationException("Visited location point is already added");
        
        var visitedLocation = new AnimalVisitedLocation
        {
            AnimalId = animalId,
            LocationPoint = point,
            DateTimeOfVisitLocationPoint = DateTime.UtcNow
        };
        animal.VisitedLocations.Add(visitedLocation);
        _context.SaveChanges();
        _logger.LogInformation("Added location point with id {LocationId} to animal {AnimalId}", locationId, animalId);
        return visitedLocation;
    }

    public bool UpdateVisitedLocationToAnimal(long animalId, UpdateVisitedLocationDto dto)
    {
        CheckAnimalIdWithThrowException(animalId);
        CheckVisitedLocationPointWithThrowException(dto.VisitedLocationId);
        CheckLocationPointWithThrowException(dto.LocationPointId);
        
        var animal = _context.Animals
            .Include(a =>a.ChippingLocationPoint)
            .Include(a => a.VisitedLocations)
            .ThenInclude(p => p.LocationPoint)
            .Single(a => a.AnimalId == animalId);
        
        var point = animal.VisitedLocations
            .SingleOrDefault(vl => vl.AnimalVisitedLocationId == dto.VisitedLocationId);
        if (point is null)
            throw new ValidationExistsException($"The animal haven't visited location with id {dto.VisitedLocationId}");
        
        if (point.LocationPoint.LocationPointId == dto.LocationPointId)
            throw new InvalidOperationException($"Location point {dto.LocationPointId} already exists");
        if (animal.VisitedLocations.First().AnimalVisitedLocationId == dto.VisitedLocationId
            && animal.ChippingLocationPoint.LocationPointId == dto.LocationPointId)
            throw new InvalidOperationException(
                $"Trying set chipping point as first visited point {dto.LocationPointId} already exists");
        var visitedLocations = animal.VisitedLocations.ToList();
        var index = visitedLocations.IndexOf(point);
        if ((index > 0 && visitedLocations[index - 1].LocationPoint.LocationPointId == dto.LocationPointId) ||
            (index < visitedLocations.Count - 1 &&
             visitedLocations[index + 1].LocationPoint.LocationPointId == dto.LocationPointId))
            throw new InvalidOperationException($"Trying set same point");
                
        point.LocationPoint = _context.Locations
            .Single(p => p.LocationPointId == dto.LocationPointId);
        
        var result = _context.SaveChanges();
        _logger.LogInformation("Updated visited location with id {VisitedLocation} at animal {AnimalId}", dto.VisitedLocationId, animalId);
        return result > 0;
    }

    public bool DeleteVisitedLocationFromAnimalData(long animalId, long visitedLocationId)
    {
        CheckAnimalIdWithThrowException(animalId);
        CheckVisitedLocationPointWithThrowException(visitedLocationId);
        
        var animal = _context.Animals
            .Include(a => a.ChippingLocationPoint)
            .Include(a => a.VisitedLocations)
            .ThenInclude(vl => vl.LocationPoint)
            .Single(a => a.AnimalId == animalId);

        var deleted = false;
        if (animal.VisitedLocations.Any(vl => vl.AnimalVisitedLocationId == visitedLocationId))
            deleted = animal.VisitedLocations.Remove(
                animal.VisitedLocations.Single(x => x.AnimalVisitedLocationId == visitedLocationId));
        else
            throw new ValidationExistsException($"Animal dont have visited location {visitedLocationId} does not exist");

        if (animal.VisitedLocations.Any() 
            && animal.VisitedLocations.First().LocationPoint.LocationPointId == animal.ChippingLocationPoint.LocationPointId)
            animal.VisitedLocations.Remove(animal.VisitedLocations.First());
            
        _context.SaveChanges();
        _logger.LogInformation("Delete visited location with id {VisitedLocationId} at animal {AnimalId}", visitedLocationId, animalId);
        return deleted;
    }

    private void CheckAnimalIdWithThrowException(long animalId)
    {
        _logger.LogInformation("Checking animal with id {AnimalId}", animalId);
        _animalValidator.CheckWithThrowExceptions(new Animal {AnimalId = animalId}, $"Animal {animalId} not found.");
    }

    private void CheckVisitedLocationPointWithThrowException(long visitedLocationPointId)
    {
        _logger.LogInformation("Checking visited location point with id {VisitedLocationId}", visitedLocationPointId);
        _visitedLocationValidator.CheckWithThrowExceptions(
            new AnimalVisitedLocation() {AnimalVisitedLocationId = visitedLocationPointId},
            $"Visited location point with id {visitedLocationPointId} does not exist");
    }

    private void CheckLocationPointWithThrowException(long locationId)
    {
        _logger.LogInformation("Checking location point with id {LocationId}", locationId);
        _locationPointValidator.CheckWithThrowExceptions(
            new LocationPoint {LocationPointId = locationId},
            $"Location {locationId} not found.");
    }
    
}