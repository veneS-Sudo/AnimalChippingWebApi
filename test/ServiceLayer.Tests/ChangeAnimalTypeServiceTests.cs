using DatabaseLayer.Context;
using DatabaseLayer.Models;
using DatabaseLayer.Validators;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ServiceLayer.Admin.AnimalServices.AnimalTypeAtAnimal;

namespace ServiceLayer.Tests;

public class ChangeAnimalTypeServiceTests
{
    private DbContextOptions<AppDbContext> Open()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        return options;
    }

    [Fact]
    public void UpdateAnimalTypeAtAnimal_ShouldUpdateAnimalType()
    {
        var options = Open();
        using (var context = new AppDbContext(options))
        {
            context.Database.EnsureCreated();
            var account = new Account()
            {
                AccountId = 1, FirstName = "John", LastName = "Doe", Email = "john.doe@gmail.com", Password = "password"
            };
            var animalTypes = new List<AnimalType>()
            {
                new() { AnimalTypeId = 1, Type = "nail" },
                new() { AnimalTypeId = 2, Type = "small" },
            };
            var point = new AnimalType() { AnimalTypeId = 3, Type = "big" };
            var animal = new Animal
            {
                AnimalId = 1, AccountId = 1, AnimalTypes = animalTypes,
                ChippingLocationPoint = new LocationPoint() { LocationPointId = 1 }
            };
            context.Accounts.Add(account);
            context.AnimalTypes.AddRange(animalTypes);
            context.AnimalTypes.Add(point);
            context.Animals.Add(animal);
            context.SaveChanges();
        }

        using (var context = new AppDbContext(options))
        {
            var service = SetupService(context);
            var animalId = service.UpdateAnimalTypeAtAnimal(1,
                new UpdateAnimalTypeInAnimalDataDto() { NewAnimalTypeId = 3, OldAnimalTypeId = 1 });
            var animalTypes = context.Animals.Include(animal => animal.AnimalTypes).First(x => x.AnimalId == animalId)
                .AnimalTypes;
            Assert.Equal(2, animalTypes.Count);
            Assert.Contains(animalTypes, x => x.AnimalTypeId == 3);
            Assert.Distinct(animalTypes);
            Assert.DoesNotContain(animalTypes, x => x.AnimalTypeId == 1);
        }
    }

    private IChangeAnimalTypeAtAnimalService SetupService(AppDbContext context)
    {
        var animalValidatorMock = new Mock<IGenericValidator<Animal>>();
        var animalTypeValidatorMock = new Mock<IGenericValidator<AnimalType>>();
        var loggerMock = new Mock<ILogger<ChangeAnimalTypeAtAnimalService>>();
        var service = new ChangeAnimalTypeAtAnimalService(context, animalValidatorMock.Object,
            animalTypeValidatorMock.Object, loggerMock.Object);
        return service;
    }
}