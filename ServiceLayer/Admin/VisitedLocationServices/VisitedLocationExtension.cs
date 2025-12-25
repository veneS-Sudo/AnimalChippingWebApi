using System.Globalization;
using AnimalChippingWebApi.Core.Models;

namespace ServiceLayer.Admin;

public static class VisitedLocationExtension
{
    public static IQueryable<AnimalVisitedLocation> FilterLocationBy(this IQueryable<AnimalVisitedLocation> source, 
        VisitedLocationFilterBy filterBy,
        string filterValue)
    {
        if (string.IsNullOrWhiteSpace(filterValue))
            return source;
        
        switch (filterBy)
        {
            case VisitedLocationFilterBy.NoFilter:
                return source;
            case VisitedLocationFilterBy.ByDate:
                var values = filterValue.Split(',')
                    .Select(x => DateTime.ParseExact(x, "yyy-MM-dd", CultureInfo.InvariantCulture)).ToList();     // Разбиение фильтра по дате
                var leftBound = values.Min();
                var rightBound = values.Max();
                return source.Where(l => l.DateTimeOfVisitLocationPoint >= leftBound 
                                  && l.DateTimeOfVisitLocationPoint <= rightBound);
            default:
                throw new ArgumentOutOfRangeException(nameof(filterBy), filterBy, null);
        }
    }
}