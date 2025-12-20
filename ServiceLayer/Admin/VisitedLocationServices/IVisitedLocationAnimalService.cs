using AnimalChippingWebApi.Core.Models;
using ServiceLayer.Admin.Dto;

namespace ServiceLayer.Admin;

public interface IVisitedLocationAnimalService
{
    IQueryable<AnimalVisitedLocation> GetAnimalVisitedLocationsById(long animalId,
        LocationVisitedSortFilterOptions sortFilterOptions);

    AnimalVisitedLocation AddVisitedLocationToAnimal(long animalId, long locationId);
    AnimalVisitedLocation UpdateVisitedLocationToAnimal(long animalId, UpdateVisitedLocationDto dto);
    bool DeleteVisitedLocationFromAnimalData(long animalId, long visitedLocationId);
}