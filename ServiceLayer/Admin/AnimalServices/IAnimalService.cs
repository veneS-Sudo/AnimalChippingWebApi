using DatabaseLayer.Models;
using ServiceLayer.Admin.AnimalServices.SearchPagination;

namespace ServiceLayer.Admin.AnimalServices;

public interface IAnimalService
{
    Animal GetAnimalById(long id);
    IQueryable<Animal> GetAnimals(AnimalSortFilterPageOptions options);
    long AddNewAnimal(AddAnimalDto animal);
    bool UpdateDataAnimal(long id, UpdateDataAnimalDto dto);
    bool SoftDelete(long id);
}