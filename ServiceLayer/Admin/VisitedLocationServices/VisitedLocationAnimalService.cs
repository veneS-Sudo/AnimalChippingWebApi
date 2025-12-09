using AnimalChippingWebApi.Core.Models;
using AnimalChippingWebApi.DataBaseLayer.Context;
using Microsoft.EntityFrameworkCore;
using ServiceLayer.Admin.Dto;

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
        var query = _context.Animals
            .AsNoTracking()
            .Include(a => a.VisitedLocations)
            .Where(a => a.AnimalId == animalId)
            .SelectMany(a => a.VisitedLocations)
            .FilterLocationBy(options.VisitedLocationFilterBy, options.FilterValue)
            .OrderBy(vl => vl.DateTimeOfVisitLocationPoint);
        return query;
    }

    public AnimalVisitedLocation AddVisitedLocationToAnimal(long animalId, long visitedLocationId)
    {
        var animal = _context.Animals
            .Include(a => a.VisitedLocations)
            .Single(a => a.AnimalId == animalId);
        var point = _context.Locations
            .Single(p => p.LocationPointId == visitedLocationId);
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
        var animal = _context.Animals
            .Include(a => a.VisitedLocations)
            .Single(a => a.AnimalId == animalId);
        var point = animal.VisitedLocations
            .Single(vl => vl.AVLocationId == dto.VisitedLocationId);
        point.LocationPoint = _context.Locations
            .Single(p => p.LocationPointId == dto.LocationPointId);
        
        _context.SaveChanges();
        return point;
    }

    public bool SoftDeleteVisitedLocation(long animalId, long visitedLocationId)
    {
        var animal = _context.Animals
            .Include(a => a.VisitedLocations)
            .Single(a => a.AnimalId == animalId);
        animal.VisitedLocations.Remove(
            animal.VisitedLocations.Single(l => l.AVLocationId == visitedLocationId));

        return animal.VisitedLocations.All(l => l.AVLocationId != visitedLocationId);
    }
    
}