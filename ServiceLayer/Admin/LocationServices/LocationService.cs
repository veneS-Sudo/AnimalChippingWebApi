using DatabaseLayer.Context;
using DatabaseLayer.Models;
using DatabaseLayer.Validators;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ServiceLayer.Exceptions;

namespace ServiceLayer.Admin.LocationServices;

public class LocationService : ILocationService
{
    private readonly AppDbContext _context;
    private readonly IGenericValidator<LocationPoint> _locationPointValidator;
    private readonly ILogger<LocationService> _logger;

    public LocationService(
        AppDbContext context,
        IGenericValidator<LocationPoint> locationPointValidator, ILogger<LocationService> logger)
    {
        _context = context;
        _locationPointValidator = locationPointValidator;
        _logger = logger;
    }

    public LocationPointDto GetLocationPointById(long id)
    {
        CheckLocationPointIdWithThrowException(id);
        
       var point = _context.Locations
           .AsNoTracking()
           .Single(point => point.LocationPointId == id);
       _logger.LogInformation("Getting location by id {LocationId}", id);
       return point.ToDto();
    }

    public long AddLocationPoint(LocationPoint locationPoint)
    {
        if (_context.Locations
            .AsNoTracking()
            .Any(l =>
                Math.Abs(Math.Round(l.Latitude, 4) - Math.Round(locationPoint.Latitude, 4)) < 0.00001
                && Math.Abs(Math.Round(l.Longitude, 4) - Math.Round(locationPoint.Longitude, 4)) < 0.00001))
            throw new UniqueValueException(
                $"Location with latitude {locationPoint.Latitude} and longitude {locationPoint.Longitude} already exists");
        _context.Locations.Add(locationPoint);
        _context.SaveChanges();
        _logger.LogInformation("Adding location point with id {LocationId} to database", locationPoint.LocationPointId);
        return locationPoint.LocationPointId;
    }

    public bool UpdateLocationPoint(LocationPoint locationPoint)
    {
        CheckLocationPointIdWithThrowException(locationPoint.LocationPointId);
        if (_context.Locations
            .AsNoTracking()
            .Any(l =>
                Math.Abs(Math.Round(l.Latitude, 4) - Math.Round(locationPoint.Latitude, 4)) < 0.00001
                && Math.Abs(Math.Round(l.Longitude, 4) - Math.Round(locationPoint.Longitude, 4)) < 0.00001))
            throw new UniqueValueException(
                $"Location with latitude {locationPoint.Latitude} and longitude {locationPoint.Longitude} already exists");
        
        var point = _context.Locations
            .Single(point => point.LocationPointId == locationPoint.LocationPointId);
        point.Latitude = locationPoint.Latitude;
        point.Longitude = locationPoint.Longitude;
        var result = _context.SaveChanges();
        _logger.LogInformation("Updated location point with id {LocationId}; new data: {Longitude}, {Latitude}",
            locationPoint.LocationPointId, locationPoint.Longitude, locationPoint.Latitude);
        return result > 0;
    }

    public bool DeletedLocationPoint(long id)
    {
        CheckLocationPointIdWithThrowException(id);
        
        var point = _context.Locations
            .Include(l => l.Animals)
            .Include(l => l.AnimalVisitedLocations)
            .Single(point => point.LocationPointId == id);
        if (point.Animals.Count != 0)
            throw new InvalidOperationException($"The location point {id} associated with animals");
        if (point.AnimalVisitedLocations.Count != 0)
            throw new InvalidOperationException($"The location point {id} associated with visited locations");
                
        _context.Locations.Remove(point);
        var  result = _context.SaveChanges();
        _logger.LogInformation("Deleted location point with id {LocationId}", id);
        return result > 0;
    }

    private void CheckLocationPointIdWithThrowException(long id)
    {
        _logger.LogInformation("Checking location point with id {LocationId} to exists", id);
        _locationPointValidator.CheckWithThrowExceptions(new LocationPoint() { LocationPointId = id },
            $"Location point with id {id} not found");
    }
}