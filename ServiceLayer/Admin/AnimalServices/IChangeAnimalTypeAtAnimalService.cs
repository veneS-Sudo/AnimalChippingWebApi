using DatabaseLayer.Models;
using ServiceLayer.Admin.Dto;

namespace ServiceLayer.Admin;

public interface IChangeAnimalTypeAtAnimalService
{
   long AddAnimalTypeAtAnimal(long animalId, long animalTypeId);
   long UpdateAnimalTypeAtAnimal(long animalId, UpdateAnimalTypeInAnimalDataDto inAnimalDataDto);
   long DeleteAnimalTypeAtAnimal(long animalId, long animalTypeId);
}