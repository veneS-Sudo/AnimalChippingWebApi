using AnimalChippingWebApi.Core.Models;

namespace ServiceLayer.Admin.TypeAnimalServices;

internal static class AnimalTypeCastExtension
{
    public static AnimalTypeDto ToDto(this AnimalType animalType)
    {
        return new AnimalTypeDto()
        {
            AnimalTypeId = animalType.AnimalTypeId,
            Type = animalType.Type
        };
    }
}