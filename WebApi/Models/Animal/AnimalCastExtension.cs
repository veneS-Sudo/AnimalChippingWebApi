using System.Globalization;
using DatabaseLayer.Models;
using ServiceLayer.Admin.AnimalServices;

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
            Gender = animal.Gender.ToString().ToUpper(),
            LiveStatus = animal.LiveStatus.ToString().ToUpper(),
            ChippingDateTime = animal.ChippingDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
            ChipperId = animal.AccountId,
            ChippingLocationPointId = animal.ChippingLocationPoint.LocationPointId,
            VisitedLocations = animal.VisitedLocations.Select(vl => vl.AnimalVisitedLocationId).ToList(), 
            DeathDateTime = animal.DeathDateTime?.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
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