using AnimalChippingWebApi.Core.Models;
using ServiceLayer.Admin.Dto;

namespace ServiceLayer.Admin;

public interface IChangeAnimalTypeAtAnimalService
{
   Animal AddAnimalTypeAtAnimal(long animalId, long animalTypeId);
   Animal UpdateAnimalTypeAtAnimal(long animalId, UpdateAnimalTypeInAnimalDataDto inAnimalDataDto);
   Animal DeleteAnimalTypeAtAnimal(long animalId, long animalTypeId);
}