using AnimalChippingWebApi.DataBaseLayer.Context;
using DatabaseLayer.Models;
using DatabaseLayer.Validators;
using Microsoft.EntityFrameworkCore;
using ServiceLayer.Registration.Exceptions;

namespace ServiceLayer.Admin.AnimalServices.LocationServices;

public class LocationService : ILocationService
{
    private readonly AppDbContext _context;
    private readonly IGenericValidator<LocationPoint> _locationPointValidator;

    public LocationService(
        AppDbContext context,
        IGenericValidator<LocationPoint> locationPointValidator)
    {
        _context = context;
        _locationPointValidator = locationPointValidator;
    }

    public LocationPointDto GetLocationPointById(long id)
    {
        CheckLocationPointIdWithThrowException(id);
        
       var point = _context.Locations
           .AsNoTracking()
           .Single(point => point.LocationPointId == id);
       return point.ToDto();
    }

    public long AddLocationPoint(LocationPoint locationPoint)
    {
        if (_context.Locations
            .AsNoTracking()
            .Any(l =>
                Math.Abs(Math.Round(l.Latitude, 4) - Math.Round(locationPoint.Latitude, 4)) < 0.001
                && Math.Abs(Math.Round(l.Longitude, 4) - Math.Round(locationPoint.Longitude, 4)) < 0.001))
            throw new UniqueValueException(
                $"Location with latitude {locationPoint.Latitude} and longitude {locationPoint.Longitude} already exists");
        _context.Locations.Add(locationPoint);
        _context.SaveChanges();
        return locationPoint.LocationPointId;
    }

    public bool UpdateLocationPoint(LocationPoint locationPoint)
    {
        CheckLocationPointIdWithThrowException(locationPoint.LocationPointId);
        if (_context.Locations
            .AsNoTracking()
            .Any(l =>
                Math.Abs(Math.Round(l.Latitude, 4) - Math.Round(locationPoint.Latitude, 4)) < 0.001
                && Math.Abs(Math.Round(l.Longitude, 4) - Math.Round(locationPoint.Longitude, 4)) < 0.001))
            throw new UniqueValueException(
                $"Location with latitude {locationPoint.Latitude} and longitude {locationPoint.Longitude} already exists");
        
        var point = _context.Locations
            .Single(point => point.LocationPointId == locationPoint.LocationPointId);
        point.Latitude = locationPoint.Latitude;
        point.Longitude = locationPoint.Longitude;
        var result = _context.SaveChanges();
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
        return result > 0;
    }

    private void CheckLocationPointIdWithThrowException(long id)
    {
        _locationPointValidator.CheckWithThrowExceptions(new LocationPoint() { LocationPointId = id },
            $"Location point with id {id} not found");
    }
}