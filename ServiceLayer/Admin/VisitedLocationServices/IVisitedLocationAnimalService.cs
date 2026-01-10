using DatabaseLayer.Models;
using ServiceLayer.Admin.Dto;

namespace ServiceLayer.Admin.VisitedLocationServices;

public interface IVisitedLocationAnimalService
{
    AnimalVisitedLocation GetAnimalVisitedLocationById(long animalId, long visitedLocationId);
    IQueryable<AnimalVisitedLocation> GetAnimalVisitedLocationsById(long animalId,
        LocationVisitedSortFilterOptions sortFilterOptions);

    AnimalVisitedLocation AddVisitedLocationToAnimal(long animalId, long locationId);
    bool UpdateVisitedLocationToAnimal(long animalId, UpdateVisitedLocationDto dto);
    bool DeleteVisitedLocationFromAnimalData(long animalId, long visitedLocationId);
}