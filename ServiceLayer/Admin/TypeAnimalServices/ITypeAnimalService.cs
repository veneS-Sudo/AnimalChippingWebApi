using DatabaseLayer.Models;
using ServiceLayer.Admin.Dto;

namespace ServiceLayer.Admin.TypeAnimalServices;

public interface ITypeAnimalService
{
   AnimalTypeDto GetAnimalTypeById(long id);
   long AddAnimalType(CreateUpdateAnimalTypeDto dto);
   bool UpdateAnimalType(AnimalType animalType);
   bool DeleteAnimalType(long id);
}