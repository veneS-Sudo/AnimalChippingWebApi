using DatabaseLayer.Context;
using DatabaseLayer.Models;
using DatabaseLayer.Validators;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;
using Moq;
using ServiceLayer.Admin.VisitedLocationServices;
using ServiceLayer.Admin.VisitedLocationServices.SearchPagination;

namespace ServiceLayer.Tests;

public class VisitedLocationServiceTests
{
    private DbContextOptions<AppDbContext> Open()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        return options;
    }

    [Theory]
    [InlineData(5, 5,  10)]
    [InlineData(2, 2, 10)]
    [InlineData(9, 9,  10)]
    [InlineData(15, 10, 10)]
    [InlineData(30, 25, 25)]
    [InlineData(5, 5, 1)]
    public void GetAnimalVisitedLocationsById_ShouldReturnExpectedVisitedLocations(int count, int result, int size)
    {
        var options = Open();
        List<AnimalVisitedLocation> visitedLocations;
        using (var context = new AppDbContext(options))
        {
            context.Database.EnsureCreated();
            var account = new Account()
            {
                AccountId = 1, FirstName = "John", LastName = "Doe", Email = "johndoe@gmail.com", Password = "password"
            };
            visitedLocations = Enumerable.Range(1, count).Select(i => new AnimalVisitedLocation()
            {
                AnimalId = 1, AnimalVisitedLocationId = i,
                LocationPoint = new LocationPoint() { Latitude = 1, Longitude = 1 }
            }).ToList();
            var animal = new Animal
            {
                AnimalId = 1, AccountId = account.AccountId, AnimalTypes = new List<AnimalType>() {new() {AnimalTypeId = 1, Type = "nail"}},
                VisitedLocations = visitedLocations, ChippingLocationPoint = new LocationPoint() { Latitude = 1, Longitude = 1 }
            };
            context.Accounts.Add(account);
            context.Animals.Add(animal);
            context.SaveChanges();
        }

        using (var context = new AppDbContext(options))
        {
            var service = SetupService(context);
            var searchOptions = new LocationVisitedSortFilterOptions()
                { PageNum = 1, PageSize = size, VisitedLocationFilterBy = VisitedLocationFilterBy.NoFilter };
            var visitedLocationsResult = service.GetAnimalVisitedLocationsById(1, searchOptions);
            Assert.Equal(result, visitedLocationsResult.Count());
            Assert.Equal<AnimalVisitedLocation>(visitedLocations.Take(result), visitedLocationsResult.AsEnumerable(),
                new ValueComparer<AnimalVisitedLocation>(
                    (x, y) => x.AnimalVisitedLocationId == y.AnimalVisitedLocationId,
                    location => location.AnimalVisitedLocationId.GetHashCode()));
        }
    }

    [Fact]
    public void AddVisitedLocationToAnimal_ShouldThrowInvalidOperationException()
    {
        var options = Open();
        Animal animal;
        using (var context = new AppDbContext(options))
        {
            context.Database.EnsureCreated();
            var account = new Account()
            {
                AccountId = 1, FirstName = "John", LastName = "Doe", Email = "johndoe@gmail.com", Password = "password"
            };
            animal = new Animal
            {
                AnimalId = 1, AccountId = account.AccountId, AnimalTypes = new List<AnimalType>() {new() {AnimalTypeId = 1, Type = "nail"}},
                ChippingLocationPoint = new LocationPoint() { Latitude = 1, Longitude = 1 }, LiveStatus = LiveStatusType.Dead,
                VisitedLocations = new List<AnimalVisitedLocation>()
            };
            context.Accounts.Add(account);
            context.Animals.Add(animal);
            context.SaveChanges();
        }

        using (var context = new AppDbContext(options))
        {
            var service = SetupService(context);
            Assert.Throws<InvalidOperationException>(() => service.AddVisitedLocationToAnimal(1, 1));
        }
    }

    [Fact]
    public void AddExistsVisitedLocationToAnimal_ShouldThrowInvalidOperationException()
    {
        var options = Open();
        using (var context = new AppDbContext(options))
        {
            context.Database.EnsureCreated();
            var account = new Account()
            {
                AccountId = 1, FirstName = "John", LastName = "Doe", Email = "johndoe@gmail.com", Password = "password"
            };
            var animal = new Animal
            {
                AnimalId = 1, AccountId = account.AccountId, AnimalTypes = new List<AnimalType>() {new() {AnimalTypeId = 1, Type = "nail"}},
                ChippingLocationPoint = new LocationPoint() {LocationPointId = 1, Latitude = 1, Longitude = 1 },
                VisitedLocations = new List<AnimalVisitedLocation>()
            };
            context.Accounts.Add(account);
            context.Animals.Add(animal);
            context.SaveChanges();
        }

        using (var context = new AppDbContext(options))
        {
            var service = SetupService(context);
            Assert.Throws<InvalidOperationException>(() => service.AddVisitedLocationToAnimal(1, 1));
        }
    }
    
    [Fact]
    public void UpdateVisitedLocationToAnimal_ShouldThrowInvalidOperationException()
    {
        var options = Open();
        using (var context = new AppDbContext(options))
        {
            context.Database.EnsureCreated();
            var account = new Account()
            {
                AccountId = 1, FirstName = "John", LastName = "Doe", Email = "johndoe@gmail.com", Password = "password"
            };
            var visitedLocation = new List<AnimalVisitedLocation>()
            {
                new()
                {
                    AnimalId = 1, AnimalVisitedLocationId = 1,
                    LocationPoint = new LocationPoint() { LocationPointId = 1 }
                },
                new()
                {
                    AnimalId = 1, AnimalVisitedLocationId = 2,
                    LocationPoint = new LocationPoint() { LocationPointId = 2 }
                },
                new()
                {
                    AnimalId = 1, AnimalVisitedLocationId = 3,
                    LocationPoint = new LocationPoint() { LocationPointId = 3 }
                },
            };
            var animal = new Animal
            {
                AnimalId = 1, AccountId = account.AccountId, AnimalTypes = new List<AnimalType>() {new() {AnimalTypeId = 1, Type = "nail"}},
                ChippingLocationPoint = new LocationPoint() {LocationPointId = 5, Latitude = 1, Longitude = 1 },
                VisitedLocations = visitedLocation,
            };
            context.Accounts.Add(account);
            context.Animals.Add(animal);
            context.SaveChanges();
        }

        using (var context = new AppDbContext(options))
        {
            var service = SetupService(context);
            Assert.Throws<InvalidOperationException>(() =>
                service.UpdateVisitedLocationToAnimal(1,
                    new UpdateVisitedLocationDto() { LocationPointId = 3, VisitedLocationId = 2 }));
        }
    }
    
    [Fact]
    public void DeleteVisitedLocation_ShouldDeleteVisitedLocationAndChippingVisitedLocation()
    {
        var options = Open();
        using (var context = new AppDbContext(options))
        {
            context.Database.EnsureCreated();
            var account = new Account()
            {
                AccountId = 1, FirstName = "John", LastName = "Doe", Email = "johndoe@gmail.com", Password = "password"
            };
            var point = new LocationPoint() { LocationPointId = 5, Latitude = 1, Longitude = 1 };
            var animal = new Animal
            {
                AnimalId = 1, AccountId = account.AccountId, AnimalTypes = new List<AnimalType>() {new() {AnimalTypeId = 1, Type = "nail"}},
                ChippingLocationPoint = point,
                VisitedLocations = new List<AnimalVisitedLocation>()
                {
                    new() {AnimalId = 1, AnimalVisitedLocationId = 1, LocationPoint = new LocationPoint() { LocationPointId = 1 } },
                    new() {AnimalId = 1, AnimalVisitedLocationId = 2, LocationPoint = point },
                },
            };
            context.Accounts.Add(account);
            context.Animals.Add(animal);
            context.SaveChanges();
        }

        using (var context = new AppDbContext(options))
        {
            var service = SetupService(context);
            service.DeleteVisitedLocationFromAnimalData(1, 1);
            Assert.Empty(context.Animals.Include(x => x.VisitedLocations).First(x => x.AnimalId==1).VisitedLocations);
        }
    }
    private IVisitedLocationAnimalService SetupService(AppDbContext context)
    {
        var animalValidatorMock = new Mock<IGenericValidator<Animal>>();
        var locationPointValidatorMock = new Mock<IGenericValidator<LocationPoint>>();
        var visitedLocationValidatorMock = new Mock<IGenericValidator<AnimalVisitedLocation>>();
        var loggerMock = new Mock<ILogger<VisitedLocationAnimalService>>();
        var service = new VisitedLocationAnimalService(context, animalValidatorMock.Object,
            locationPointValidatorMock.Object, visitedLocationValidatorMock.Object, loggerMock.Object);
        return service;
    }
}