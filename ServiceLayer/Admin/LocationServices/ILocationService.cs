using DatabaseLayer.Models;

namespace ServiceLayer.Admin.LocationServices;

public interface ILocationService
{
    LocationPointDto GetLocationPointById(long id);
    long AddLocationPoint(LocationPoint locationPoint);
    bool UpdateLocationPoint(LocationPoint locationPoint);
    bool DeletedLocationPoint(long id);
}