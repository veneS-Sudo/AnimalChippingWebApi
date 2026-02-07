using DatabaseLayer.Context;
using DatabaseLayer.Models;
using DatabaseLayer.Validators;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ServiceLayer.Admin.LocationServices;
using ServiceLayer.Exceptions;

namespace ServiceLayer.Tests;

public class LocationServiceTests
{
    private DbContextOptions<AppDbContext> Open()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        return options;
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(1.000001, 1)]
    [InlineData(1, 0.99999)]
    [InlineData(1.00001, 1)]
    public void AddLocationPoint_ShouldThrowUniqueValueException(double longitude, double latitude)
    {
        var options = Open();
        using (var context = new AppDbContext(options))
        {
            context.Database.EnsureCreated();
            context.Locations.Add(new LocationPoint() {Longitude = 1, Latitude = 1});
            context.SaveChanges();
        }

        using (var context = new AppDbContext(options))
        {
            var service = SetupService(context);
            var point = new LocationPoint() {Longitude = longitude, Latitude = latitude};
            Assert.Throws<UniqueValueException>(() => service.AddLocationPoint(point));
        }
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(1.000001, 1)]
    [InlineData(1, 0.99999)]
    [InlineData(1.00001, 1)]
    public void UpdateLocationPoint_ShouldThrowUniqueValueException(double longitude, double latitude)
    {
        var options = Open();
        using (var context = new AppDbContext(options))
        {
            context.Database.EnsureCreated();
            context.Locations.Add(new LocationPoint() {LocationPointId = 1, Longitude = 1, Latitude = 1});
            context.SaveChanges();
        }

        using (var context = new AppDbContext(options))
        {
            var service = SetupService(context);
            var point = new LocationPoint() {LocationPointId = 1, Longitude = longitude, Latitude = latitude};
            Assert.Throws<UniqueValueException>(() => service.UpdateLocationPoint(point));
        }
    }

    [Fact]
    public void DeleteLocationPoint_ShouldThrowInvalidOperationException()
    {
        var options = Open();
        using (var context = new AppDbContext(options))
        {
            context.Database.EnsureCreated();
            var point = new LocationPoint() { LocationPointId = 1, Longitude = 1, Latitude = 1 };
            var account = new Account() {AccountId = 1, Email = "test@test.com", Password = "test", FirstName = "first", LastName = "last"};
            context.Accounts.Add(account);
            context.Locations.Add(point);
            context.Animals.Add(new Animal()
            {
                AccountId = 1, AnimalTypes = new List<AnimalType>(),
                ChippingLocationPoint = point
            });
            context.SaveChanges();
        }

        using (var context = new AppDbContext(options))
        {
            var service = SetupService(context);
            Assert.Throws<InvalidOperationException>(() => service.DeletedLocationPoint(1));
        }
        
    }
    private ILocationService SetupService(AppDbContext context)
    {
        var validatorMock = new Mock<IGenericValidator<LocationPoint>>();
        var loggerMock = new Mock<ILogger<LocationService>>();
            var service = new LocationService(context, validatorMock.Object, loggerMock.Object);
        return service;
    }
}