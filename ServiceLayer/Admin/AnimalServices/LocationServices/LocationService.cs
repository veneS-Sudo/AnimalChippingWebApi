using AnimalChippingWebApi.Core.Models;
using AnimalChippingWebApi.DataBaseLayer.Context;

namespace ServiceLayer.Admin.LocationServices;

public class LocationService
{
    private readonly AppDbContext _context;

    public LocationService(AppDbContext context)
    {
        _context = context;
    }

    public LocationPoint GetLocationPointById(long id)
    {
       var point = _context.Locations.Single(point => point.LocationPointId == id);
       return point;
    }

    public bool AddLocationPoint(LocationPoint locationPoint)
    {
        var resultSearch = _context.Locations
            .SingleOrDefault(point => point.LocationPointId == locationPoint.LocationPointId);
        if (resultSearch != null)
        {
            if (!resultSearch.SoftDeleted)
                throw new Exception($"Location point with id {locationPoint.LocationPointId} already exists");
            resultSearch.SoftDeleted = false;
        }
        else
            _context.Locations.Add(locationPoint);
        
        _context.SaveChanges();
        return true;
    }

    public bool UpdateLocationPoint(LocationPoint locationPoint)
    {
        var point = _context.Locations.Single(point => point.LocationPointId == locationPoint.LocationPointId);
        point.Latitude = locationPoint.Latitude;
        point.Longitude = locationPoint.Longitude;
        _context.SaveChanges();
        return true;
    }

    public bool SoftDeletedLocationPoint(long id)
    {
        var point = _context.Locations.Single(point => point.LocationPointId == id);
        point.SoftDeleted = true;
        _context.SaveChanges();
        return point.SoftDeleted;
    }
}