using System.Globalization;
using DatabaseLayer.Models;

namespace AnimalChippingWebApi.Models.VisitedLocation;

public static class VisitedLocationCastExtension
{
    public static VisitedLocationDto ToDto(this AnimalVisitedLocation visitedLocation)
    {
        return new VisitedLocationDto
        {
            VisitedLocationId = visitedLocation.AnimalVisitedLocationId,
            DateTimeOfVisitedLocation = visitedLocation.DateTimeOfVisitLocationPoint.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
            LocationPointId = visitedLocation.LocationPoint.LocationPointId,
        };
    }
}