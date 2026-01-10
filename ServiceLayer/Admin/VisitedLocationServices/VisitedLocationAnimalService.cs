using AnimalChippingWebApi.DataBaseLayer.Context;
using DatabaseLayer.Models;
using DatabaseLayer.QueryObjects;
using DatabaseLayer.Validators;
using Microsoft.EntityFrameworkCore;
using ServiceLayer.Admin.Dto;

namespace ServiceLayer.Admin.VisitedLocationServices;

public class VisitedLocationAnimalService : IVisitedLocationAnimalService
{
    private readonly AppDbContext _context;
    private readonly IGenericValidator<Animal> _animalValidator;
    private readonly IGenericValidator<LocationPoint> _locationPointValidator;
    private readonly IGenericValidator<AnimalVisitedLocation> _visitedLocationValidator;

    public VisitedLocationAnimalService(
        AppDbContext context, 
        IGenericValidator<Animal> validator, 
        IGenericValidator<LocationPoint> locationPointValidator,
        IGenericValidator<AnimalVisitedLocation> visitedLocationValidator)
    {
        _context = context;
        _animalValidator = validator;
        _locationPointValidator = locationPointValidator;
        _visitedLocationValidator = visitedLocationValidator;
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
        return deleted;
    }

    private void CheckAnimalIdWithThrowException(long animalId)
    {
        _animalValidator.CheckWithThrowExceptions(new Animal {AnimalId = animalId}, $"Animal {animalId} not found.");
    }

    private void CheckVisitedLocationPointWithThrowException(long visitedLocationPointId)
    {
        _visitedLocationValidator.CheckWithThrowExceptions(
            new AnimalVisitedLocation() {AnimalVisitedLocationId = visitedLocationPointId},
            $"Visited location point with id {visitedLocationPointId} does not exist");
    }

    private void CheckLocationPointWithThrowException(long locationId)
    {
        _locationPointValidator.CheckWithThrowExceptions(
            new LocationPoint {LocationPointId = locationId},
            $"Location {locationId} not found.");
    }
    
}