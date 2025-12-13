using AnimalChippingWebApi.Core.Models;
using AnimalChippingWebApi.DataBaseLayer.Context;
using DatabaseLayer.QueryObjects;
using DatabaseLayer.Validators;
using Microsoft.EntityFrameworkCore;
using ServiceLayer.Admin.Dto;

namespace ServiceLayer.Admin;

public class VisitedLocationAnimalService
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

    public IQueryable<AnimalVisitedLocation> GetAnimalVisitedLocationsById(
        long animalId, 
        LocationVisitedSortFilterOptions sortFilterOptions)
    {
        CheckAnimalIdWithThrowException(animalId);
        
        var query = _context.Animals
            .AsNoTracking()
            .Include(a => a.VisitedLocations)
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
            .ThenInclude(l => l.LocationPoint)
            .Single(a => a.AnimalId == animalId);
        if (animal.LiveStatus == LiveStatusType.Dead)
            throw new InvalidOperationException("Animal is dead");
        
        var point = _context.Locations
            .Single(p => p.LocationPointId == locationId);
        if (point.LocationPointId == animal.ChippingLocationPoint.LocationPointId)
            throw new InvalidOperationException("Location point is already added");
        if (point.LocationPointId == animal.VisitedLocations.Last().LocationPoint.LocationPointId)
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

    public AnimalVisitedLocation UpdateVisitedLocationToAnimal(long animalId, UpdateVisitedLocationDto dto)
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
            .Single(vl => vl.AVLocationId == dto.VisitedLocationId);
        
        if (point.LocationPoint.LocationPointId == dto.LocationPointId)
            throw new InvalidOperationException($"Location point {dto.LocationPointId} already exists");
        if (animal.ChippingLocationPoint.LocationPointId == dto.LocationPointId)
            throw new InvalidOperationException($"Trying set chipping point as visited point {dto.LocationPointId} already exists");
        var visitedLocations = animal.VisitedLocations.ToList();
        var index = visitedLocations.IndexOf(point);
        if ((index > 0 && visitedLocations[index - 1].LocationPoint.LocationPointId == dto.LocationPointId) ||
            (index < visitedLocations.Count - 1 &&
             visitedLocations[index + 1].LocationPoint.LocationPointId == dto.LocationPointId))
            throw new InvalidOperationException($"Trying set same point");
                
        point.LocationPoint = _context.Locations
            .Single(p => p.LocationPointId == dto.LocationPointId);
        
        _context.SaveChanges();
        return point;
    }

    public bool DeleteVisitedLocationFromAnimalData(long animalId, long visitedLocationId)
    {
        CheckAnimalIdWithThrowException(animalId);
        CheckVisitedLocationPointWithThrowException(visitedLocationId);
        
        var animal = _context.Animals
            .Include(a => a.VisitedLocations)
            .Single(a => a.AnimalId == animalId);

        var deleted = false;
        if (animal.VisitedLocations.Any(vl => vl.AVLocationId == visitedLocationId))
            deleted = animal.VisitedLocations.Remove(
                animal.VisitedLocations.Single(x => x.AVLocationId == visitedLocationId));
        else
            throw new Exception($"Animal dont have visited location {visitedLocationId} does not exist");

        return deleted;
    }

    private void CheckAnimalIdWithThrowException(long animalId)
    {
        _animalValidator.CheckWithThrowExceptions(new Animal {AnimalId = animalId}, $"Animal {animalId} not found.");
    }

    private void CheckVisitedLocationPointWithThrowException(long visitedLocationPointId)
    {
        _visitedLocationValidator.CheckWithThrowExceptions(
            new AnimalVisitedLocation() {AVLocationId = visitedLocationPointId},
            $"Visited location point with id {visitedLocationPointId} does not exist");
    }

    private void CheckLocationPointWithThrowException(long locationId)
    {
        _locationPointValidator.CheckWithThrowExceptions(
            new LocationPoint {LocationPointId = locationId},
            $"Location {locationId} not found.");
    }
    
}