using AnimalChippingWebApi.Core.Models;
using ServiceLayer.Admin.Dto;

namespace ServiceLayer.Admin.TypeAnimalServices;

public interface ITypeAnimalService
{
   AnimalType GetAnimalTypeById(long id);
   bool AddAnimalType(AddAnimalTypeDto dto);
   bool UpdateAnimalType(AnimalType animalType);
   bool DeleteAnimalType(long id);
}