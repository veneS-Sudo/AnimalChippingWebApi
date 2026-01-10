using DatabaseLayer.Models;
using ServiceLayer.Admin.Dto;

namespace AnimalChippingWebApi.Models.Animal;

internal static class AnimalCastExtension
{
    public static AnimalDto ToDto(this DatabaseLayer.Models.Animal animal)
    {
        return new AnimalDto()
        {
            AnimalId = animal.AnimalId,
            AnimalTypes = animal.AnimalTypes.Select(at => at.AnimalTypeId).ToList(),
            Weight = animal.Weight,
            Height = animal.Height,
            Length = animal.Length,
            Gender = animal.Gender.ToString(),
            LiveStatus = animal.LiveStatus.ToString(),
            ChippingDateTime = animal.ChippingDateTime.ToString("yyyy-MM-dd"),
            ChipperId = animal.AccountId,
            ChippingLocationPointId = animal.ChippingLocationPoint.LocationPointId,
            VisitedLocations = animal.VisitedLocations.Select(vl => vl.AnimalVisitedLocationId).ToList(), 
            DeathDateTime = animal.DeathDateTime?.ToString("yyyy-MM-dd"),
        };
    }

    public static AddAnimalDto ToAddAnimalDto(this CreateAnimalDto animal)
    {
        return new AddAnimalDto()
        {
            AccountId = animal.AccountId,
            ChippingLocationPointId = animal.ChippingLocationPointId,
            Weight = animal.Weight,
            Height = animal.Height,
            Length = animal.Length,
            AnimalTypeIds = animal.AnimalTypeIds,
            Gender = Enum.Parse<GenderType>(animal.Gender, true)
        };
    }
    public static UpdateDataAnimalDto ToUpdateDataAnimalDto(this UpdateAnimalDto animal)
    {
        return new UpdateDataAnimalDto()
        {
            AccountId = animal.AccountId,
            ChippingLocationPointId = animal.ChippingLocationPointId,
            Weight = animal.Weight,
            Height = animal.Height,
            Length = animal.Length,
            Gender = Enum.Parse<GenderType>(animal.Gender, true),
            LiveStatus = Enum.Parse<LiveStatusType>(animal.LiveStatus, true),
        };
    }
}