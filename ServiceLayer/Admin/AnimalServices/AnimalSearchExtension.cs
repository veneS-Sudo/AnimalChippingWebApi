using DatabaseLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace ServiceLayer.Admin;

public static class AnimalSearchExtension
{
    public static IQueryable<Animal> FilterAnimalsBy(this IQueryable<Animal> query, 
        AnimalSearchFilterValues? filterValues)
    {
        if (filterValues is null)
            return query;
        
        if (filterValues.StartDate is not null)
            query = query.Where(animal => animal.ChippingDateTime >= filterValues.StartDate);
        if (filterValues.EndDate is not null)
            query = query.Where(animal => animal.ChippingDateTime <= filterValues.EndDate);
        if (filterValues.ChipperId is not null)
            query = query.Where(animal => animal.AccountId == filterValues.ChipperId);
        if (filterValues.ChippingLocationId is not null)
            query = query.Include(animal => animal.ChippingLocationPoint)
                .Where(animal => animal.ChippingLocationPoint.LocationPointId == filterValues.ChippingLocationId);
        if (filterValues.LiveStatus is not null)
            query = query.Where(animal => animal.LiveStatus == filterValues.LiveStatus);
        if (filterValues.Gender is not null)
            query = query.Where(animal => animal.Gender == filterValues.Gender);
        
        return query;
    }
}