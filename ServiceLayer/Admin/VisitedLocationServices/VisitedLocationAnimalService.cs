using AnimalChippingWebApi.Core.Models;
using AnimalChippingWebApi.DataBaseLayer.Context;
using DatabaseLayer.QueryObjects;
using Microsoft.EntityFrameworkCore;
using ServiceLayer.Admin.Dto;
using ServiceLayer.Exceptions;

namespace ServiceLayer.Admin;

public class VisitedLocationAnimalService
{
    private readonly AppDbContext _context;

    public VisitedLocationAnimalService(AppDbContext context)
    {
        _context = context;
    }

    public IQueryable<AnimalVisitedLocation> GetAnimalVisitedLocationsById(
        long animalId, 
        LocationVisitedLocationOptions options)
    {
        CheckAnimalIdWithThrowException(animalId);
        
        var query = _context.Animals
            .AsNoTracking()
            .Include(a => a.VisitedLocations)
            .Where(a => a.AnimalId == animalId)
            .SelectMany(a => a.VisitedLocations)
            .FilterLocationBy(options.VisitedLocationFilterBy, options.FilterValue)
            .OrderBy(vl => vl.DateTimeOfVisitLocationPoint);
        
        options.SetupRest(query);
        
        return query.Page(options.PageNum-1, options.PageSize);
    }

    public AnimalVisitedLocation AddVisitedLocationToAnimal(long animalId, long visitedLocationId)
    {
        CheckAnimalIdWithThrowException(animalId);
        CheckLocationPointWithThrowException(visitedLocationId);
        
        var animal = _context.Animals
            .Include(a => a.ChippingLocationPoint)
            .Include(a => a.VisitedLocations)
            .ThenInclude(l => l.LocationPoint)
            .Single(a => a.AnimalId == animalId);
        if (animal.LiveStatus == LiveStatusType.Dead)
            throw new InvalidOperationException("Animal is dead");
        
        var point = _context.Locations
            .Single(p => p.LocationPointId == visitedLocationId);
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
        CheckAnimalIdWithThrowException(dto.VisitedLocationId);
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
        var animal = _context.Animals
            .AsNoTracking()
            .SingleOrDefault(a => a.AnimalId == animalId);
        if (animal == null)
            throw new EntityByIdNotFoundException($"Animal with id {animalId} does not exist");
    }

    private void CheckVisitedLocationPointWithThrowException(long visitedLocationPointId)
    {
        var visitedLocation = _context.AnimalVisitedLocations
            .AsNoTracking()
            .Single(vl => vl.AVLocationId == visitedLocationPointId);
        if (visitedLocation == null)
            throw new EntityByIdNotFoundException($"Visited location point with id {visitedLocationPointId} does not exist");
    }

    private void CheckLocationPointWithThrowException(long locationPointId)
    {
        var locationPoint = _context.Locations
            .AsNoTracking()
            .Single(l => l.LocationPointId == locationPointId);
        if (locationPoint == null)
            throw new EntityByIdNotFoundException($"Location point with id {locationPointId} does not exist");
    }
    
}