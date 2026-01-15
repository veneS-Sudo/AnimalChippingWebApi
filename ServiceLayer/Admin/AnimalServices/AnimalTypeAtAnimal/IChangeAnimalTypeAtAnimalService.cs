namespace ServiceLayer.Admin.AnimalServices.AnimalTypeAtAnimal;

public interface IChangeAnimalTypeAtAnimalService
{
   long AddAnimalTypeAtAnimal(long animalId, long animalTypeId);
   long UpdateAnimalTypeAtAnimal(long animalId, UpdateAnimalTypeInAnimalDataDto inAnimalDataDto);
   long DeleteAnimalTypeAtAnimal(long animalId, long animalTypeId);
}