using DatabaseLayer.Models;

namespace ServiceLayer.Admin.LocationServices;

public static class LocationCastExtension
{
    public static LocationPointDto ToDto(this LocationPoint point)
    {
        return new LocationPointDto()
        {
            LocationId = point.LocationPointId,
            Latitude = point.Latitude,
            Longitude = point.Longitude,
        };
    }
}