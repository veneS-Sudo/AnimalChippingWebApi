using AnimalChippingWebApi.Core.Models;
using ServiceLayer.Admin.Dto;

namespace ServiceLayer.Admin;

public interface IAnimalService
{
    Animal GetAnimalById(long id);
    IQueryable<Animal> GetAnimals(AnimalSortFilterPageOptions options);
    bool AddNewAnimal(AddAnimalDto animal);
    bool UpdateDataAnimal(long id, UpdateDataAnimalDto dto);
    bool SoftDelete(long id);
}