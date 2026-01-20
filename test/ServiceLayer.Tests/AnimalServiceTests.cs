using DatabaseLayer.Models;
using DatabaseLayer.Validators;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using ServiceLayer.Admin;
using ServiceLayer.Admin.AnimalServices;
using ServiceLayer.Admin.Dto;
using ServiceLayer.Registration.Exceptions;
using AnimalVisitedLocation = DatabaseLayer.Models.AnimalVisitedLocation;
using AppDbContext = DatabaseLayer.Context.AppDbContext;

namespace ServiceLayer.Tests;

public class AnimalServiceTests
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
    public void GetAnimals_ShouldReturnExpectedCountAnimals(int count, int result, int size)
    {
        var options = Open();
        IEnumerable<Animal> arrange;
        using (var context = new AppDbContext(options))
        {
            context.Database.EnsureCreated();
            var arrangeAccounts = Enumerable.Range(1, count)
                .Select(i => new Account() 
                    { AccountId = i, FirstName = $"FirstName{i}", LastName = $"LastName{i}", Email = $"Email{i}",
                        Password = string.Concat(Enumerable.Repeat(i.ToString(), i))}).ToList();
            context.Accounts.AddRange(arrangeAccounts);
            arrange = Enumerable.Range(1, count)
                .Select(i => new Animal
                {
                    AnimalId = i, AccountId = arrangeAccounts[i-1].AccountId,
                    AnimalTypes = new[] { new AnimalType() { Type = "nail" } },
                    ChippingLocationPoint = new LocationPoint()
                }).ToList();
            context.Animals.AddRange(arrange);
            context.SaveChanges();
        }

        using (var context = new AppDbContext(options))
        {
            var service = SetupService(context);
            var searchOptions = new AnimalSortFilterPageOptions() {PageNum = 1, PageSize = size};
            var animals = service.GetAnimals(searchOptions);
            Assert.Equal(result, animals.Count());
            Assert.Equal(arrange.Take(result), animals, new AnimalEqualityComparer());
        }
    }

    [Fact]
    public void AddNewAnimal_ShouldThrowUniqueValueException()
    {
        var options = Open();
        Animal expected;
        using (var context = new AppDbContext(options))
        {
            context.Database.EnsureCreated();
            var account = new Account()
            {
                AccountId = 1, FirstName = "FirstName", LastName = "LastName", Email = "Email", Password = "password"
            };
            context.Accounts.Add(account);
            expected = new Animal
                {
                    AnimalId = 1, AccountId = 1,
                    AnimalTypes = new[] { new AnimalType() { Type = "nail" } },
                    ChippingLocationPoint = new LocationPoint() {LocationPointId = 1}
                };
            context.Animals.Add(expected);
            context.SaveChanges();
        }

        var newAnimal = new AddAnimalDto()
        {
            AccountId = expected.AccountId,
            AnimalTypeIds = new List<long>()
                { expected.AnimalTypes.First().AnimalTypeId, expected.AnimalTypes.First().AnimalTypeId },
            ChippingLocationPointId = expected.ChippingLocationPoint.LocationPointId
        };

        using (var context = new AppDbContext(options))
        {
            var service = SetupService(context);
            Assert.Throws<UniqueValueException>(() => service.AddNewAnimal(newAnimal));
        }
    }

    [Fact]
    public void AddNewAnimal_ShouldThrowInvalidOperationException()
    {
        var options = Open();
        Animal expected;
        using (var context = new AppDbContext(options))
        {
            context.Database.EnsureCreated();
            var account = new Account()
            {
                AccountId = 1, FirstName = "FirstName", LastName = "LastName", Email = "Email", Password = "password"
            };
            context.Accounts.Add(account);
            expected = new Animal
                {
                    AnimalId = 1, AccountId = 1,
                    AnimalTypes = new[] { new AnimalType() { Type = "nail" } },
                    ChippingLocationPoint = new LocationPoint() {LocationPointId = 1}
                };
            context.Animals.Add(expected);
            context.SaveChanges();
        }

        var newAnimal = new AddAnimalDto()
        {
            AccountId = expected.AccountId,
            AnimalTypeIds = new List<long>(),
            ChippingLocationPointId = expected.ChippingLocationPoint.LocationPointId
        };

        using (var context = new AppDbContext(options))
        {
            var service = SetupService(context);
            Assert.Throws<InvalidOperationException>(() => service.AddNewAnimal(newAnimal));
        }
    }
    
    private IAnimalService SetupService(AppDbContext context)
    {
        var accountValidatorMock = new Mock<IGenericValidator<Account>>();
        var animalValidatorMock = new Mock<IGenericValidator<Animal>>();
        var pointValidatorMock = new Mock<IGenericValidator<LocationPoint>>();
        var service = new AnimalService(context, animalValidatorMock.Object, pointValidatorMock.Object,
            accountValidatorMock.Object);
        return service;
    }

    [Fact]
    public void AddNewAnimal_ShouldThrowValidationExistsException()
    {
        var options = Open();
        Animal expected;
        using (var context = new AppDbContext(options))
        {
            context.Database.EnsureCreated();
            var account = new Account()
            {
                AccountId = 1, FirstName = "FirstName", LastName = "LastName", Email = "Email", Password = "password"
            };
            context.Accounts.Add(account);
            var animalTypes = Enumerable.Range(1, 5).Select(i => new AnimalType() { AnimalTypeId = i, Type = $"{i}" })
                .ToList();
            context.AnimalTypes.AddRange(animalTypes);
            expected = new Animal
                {
                    AnimalId = 1, AccountId = 1,
                    AnimalTypes = animalTypes,
                    ChippingLocationPoint = new LocationPoint() {LocationPointId = 1}
                };
            context.Animals.Add(expected);
            context.SaveChanges();
        }

        var newAnimal = new AddAnimalDto()
        {
            AccountId = expected.AccountId,
            AnimalTypeIds = expected.AnimalTypes.Concat(new[] { new AnimalType() { AnimalTypeId = 8, Type = "8" } })
                .Select(x => x.AnimalTypeId).ToList(),
            ChippingLocationPointId = expected.ChippingLocationPoint.LocationPointId
        };

        using (var context = new AppDbContext(options))
        {
            var service = SetupService(context);
            Assert.Throws<ValidationExistsException>(() => service.AddNewAnimal(newAnimal));
        }
    }

    [Fact]
    public void UpdateDataAnimal_ShouldThrowInvalidOperationException()
    {
        var options = Open();
        Animal expected;
        using (var context = new AppDbContext(options))
        {
            context.Database.EnsureCreated();
            var account = new Account()
            {
                AccountId = 1, FirstName = "FirstName", LastName = "LastName", Email = "Email", Password = "password"
            };
            context.Accounts.Add(account);
            var animalTypes = Enumerable.Range(1, 5).Select(i => new AnimalType() { AnimalTypeId = i, Type = $"{i}" })
                .ToList();
            context.AnimalTypes.AddRange(animalTypes);
            expected = new Animal
                {
                    AnimalId = 1, AccountId = 1,
                    AnimalTypes = animalTypes,
                    ChippingLocationPoint = new LocationPoint() {LocationPointId = 1},
                    LiveStatus = LiveStatusType.Dead
                };
            context.Animals.Add(expected);
            context.SaveChanges();
        }

        var updateAnimal = new UpdateDataAnimalDto()
        {
            AccountId = expected.AccountId, ChippingLocationPointId = expected.ChippingLocationPoint.LocationPointId,
            LiveStatus = LiveStatusType.Alive
        };
        using (var context = new AppDbContext(options))
        {
            var service = SetupService(context);
            Assert.Throws<InvalidOperationException>(() => service.UpdateDataAnimal(expected.AnimalId, updateAnimal));
        }
    }

    [Fact]
    public void UpdateDataAnimalWithVisitedLocation_ShouldThrowInvalidOperationException()
    {
        var options = Open();
        Animal expected;
        using (var context = new AppDbContext(options))
        {
            context.Database.EnsureCreated();
            var account = new Account()
            {
                AccountId = 1, FirstName = "FirstName", LastName = "LastName", Email = "Email", Password = "password"
            };
            context.Accounts.Add(account);
            var animalTypes = Enumerable.Range(1, 5).Select(i => new AnimalType() { AnimalTypeId = i, Type = $"{i}" })
                .ToList();
            context.AnimalTypes.AddRange(animalTypes);

            expected = new Animal
            {
                AnimalId = 1, AccountId = 1,
                AnimalTypes = animalTypes,
                VisitedLocations = new List<AnimalVisitedLocation>()
                {
                    new AnimalVisitedLocation()
                    {
                        AnimalId = 1, AnimalVisitedLocationId = 1, LocationPoint = new LocationPoint() { LocationPointId = 2 }
                    }
                },
                ChippingLocationPoint = new LocationPoint() { LocationPointId = 1 }
            };
            context.Animals.Add(expected);
            context.SaveChanges();
        }

        var updateAnimal = new UpdateDataAnimalDto()
        {
            AccountId = expected.AccountId,
            ChippingLocationPointId = expected.VisitedLocations.First().LocationPoint.LocationPointId,
            LiveStatus = LiveStatusType.Alive
        };
        using (var context = new AppDbContext(options))
        {
            var service = SetupService(context);
            Assert.Throws<InvalidOperationException>(() => service.UpdateDataAnimal(expected.AnimalId, updateAnimal));
        }
    }

    [Fact]
    public void DeleteAnimal_ShouldThrowInvalidOperationException()
    {
        var options = Open();
        Animal expected;
        using (var context = new AppDbContext(options))
        {
            context.Database.EnsureCreated();
            var account = new Account()
            {
                AccountId = 1, FirstName = "FirstName", LastName = "LastName", Email = "Email", Password = "password"
            };
            context.Accounts.Add(account);
            var animalTypes = Enumerable.Range(1, 5).Select(i => new AnimalType() { AnimalTypeId = i, Type = $"{i}" })
                .ToList();
            context.AnimalTypes.AddRange(animalTypes);

            expected = new Animal
            {
                AnimalId = 1, AccountId = 1,
                AnimalTypes = animalTypes,
                VisitedLocations = new List<AnimalVisitedLocation>()
                {
                    new AnimalVisitedLocation()
                    {
                        AnimalId = 1, AnimalVisitedLocationId = 1,
                        LocationPoint = new LocationPoint() { LocationPointId = 2 }
                    }
                },
                ChippingLocationPoint = new LocationPoint() { LocationPointId = 1 }
            };
            context.Animals.Add(expected);
            context.SaveChanges();
        }
        
        using (var context = new AppDbContext(options))
        {
            var service = SetupService(context);
            Assert.Throws<InvalidOperationException>(() => service.SoftDelete(expected.AnimalId));
        }
    }
    internal class AnimalEqualityComparer : IEqualityComparer<Animal>
    {
        public bool Equals(Animal? x, Animal? y)
        {
            if (x is null || y is null)
                return false;
            return x.AnimalId == y.AnimalId;
        }

        public int GetHashCode(Animal obj)
        {
            var hashCode = obj.AnimalId.GetHashCode();
            return hashCode;
        }
    }
}