using AnimalChippingWebApi.Core.Models;

namespace ServiceLayer.Admin.LocationServices;

public interface ILocationService
{
    LocationPoint GetLocationPointById(long id);
    bool AddLocationPoint(LocationPoint locationPoint);
    bool UpdateLocationPoint(LocationPoint locationPoint);
    bool DeletedLocationPoint(long id);
}