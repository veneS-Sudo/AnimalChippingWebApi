using System.Net;
using System.Net.Http.Json;
using AnimalChippingWebApi.Models.VisitedLocation;
using DatabaseLayer.Context;
using DatabaseLayer.Models;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.Admin.Dto;

namespace WevApi.Tests.VisitedLocationEndpoints;

public class PutVisitedLocationEndpointTest : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _fixture;
    private AppDbContext _dbContext;
    private IDbContextTransaction _transaction;

    public PutVisitedLocationEndpointTest(CustomWebApplicationFactory fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task PutVisitedLocation_ShouldUpdateVisitedLocation()
    {
        using var scope = _fixture.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _transaction = await _dbContext.Database.BeginTransactionAsync();
        
        var location = new LocationPoint() { Latitude = 10, Longitude = 10 };
        var secondLocation = new LocationPoint() { Latitude = 20, Longitude = 20 };
        var animal = new Animal()
        {
            AccountId = 1, AnimalTypes = new List<AnimalType>(),
            ChippingLocationPoint = new LocationPoint() { Latitude = 1, Longitude = 1 }, SoftDeleted = false,
            LiveStatus = LiveStatusType.Alive, VisitedLocations = new List<AnimalVisitedLocation>() {new() {LocationPoint = location}}
        };
        _dbContext.Animals.Add(animal);
        _dbContext.Locations.AddRange(location, secondLocation);
        await _dbContext.SaveChangesAsync();
        using var client = _fixture.CreateClient();
        var updateVisitedLocation = new UpdateVisitedLocationDto()
        {
            LocationPointId = secondLocation.LocationPointId,
            VisitedLocationId = animal.VisitedLocations.First().AnimalVisitedLocationId
        };

        var response = await client.PutAsJsonAsync($"/animals/{animal.AnimalId}/locations", updateVisitedLocation);
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<VisitedLocationDto>();
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(dto);
        Assert.Equal(secondLocation.LocationPointId, dto.LocationPointId);
        
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _dbContext.DisposeAsync();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public async Task PutVisitedLocationWithInvalidAnimalId_ShouldReturn400BadRequest(int animalId)
    {
        using var client = _fixture.CreateClient();
        var updateVisitedLocation = new UpdateVisitedLocationDto()
        {
            LocationPointId = 1,
            VisitedLocationId = 1
        };
        var response = await client.PutAsJsonAsync($"/animals/{animalId}/locations", updateVisitedLocation);
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public async Task PutVisitedLocationWithInvalidLocationPointId_ShouldReturn400BadRequest(int locationPointId)
    {
        using var scope = _fixture.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _transaction = await _dbContext.Database.BeginTransactionAsync();
        
        var location = new LocationPoint() { Latitude = 10, Longitude = 10 };
        var secondLocation = new LocationPoint() { Latitude = 20, Longitude = 20 };
        var animal = new Animal()
        {
            AccountId = 1, AnimalTypes = new List<AnimalType>(),
            ChippingLocationPoint = new LocationPoint() { Latitude = 1, Longitude = 1 }, SoftDeleted = false,
            LiveStatus = LiveStatusType.Alive, VisitedLocations = new List<AnimalVisitedLocation>() {new() {LocationPoint = location}}
        };
        _dbContext.Animals.Add(animal);
        _dbContext.Locations.AddRange(location, secondLocation);
        await _dbContext.SaveChangesAsync();
        using var client = _fixture.CreateClient();
        var updateVisitedLocation = new UpdateVisitedLocationDto()
        {
            LocationPointId = locationPointId,
            VisitedLocationId = animal.VisitedLocations.First().AnimalVisitedLocationId
        };
        var response = await client.PutAsJsonAsync($"/animals/{animal.AnimalId}/locations", updateVisitedLocation);
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _dbContext.DisposeAsync();
    }
    
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public async Task PutVisitedLocationWithInvalidVisitedLocationId_ShouldReturn400BadRequest(int visitedLocationId)
    {
        using var scope = _fixture.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _transaction = await _dbContext.Database.BeginTransactionAsync();
        
        var location = new LocationPoint() { Latitude = 10, Longitude = 10 };
        var secondLocation = new LocationPoint() { Latitude = 20, Longitude = 20 };
        var animal = new Animal()
        {
            AccountId = 1, AnimalTypes = new List<AnimalType>(),
            ChippingLocationPoint = new LocationPoint() { Latitude = 1, Longitude = 1 }, SoftDeleted = false,
            LiveStatus = LiveStatusType.Alive, VisitedLocations = new List<AnimalVisitedLocation>() {new() {LocationPoint = location}}
        };
        _dbContext.Animals.Add(animal);
        _dbContext.Locations.AddRange(location, secondLocation);
        await _dbContext.SaveChangesAsync();
        using var client = _fixture.CreateClient();
        var updateVisitedLocation = new UpdateVisitedLocationDto()
        {
            LocationPointId = location.LocationPointId,
            VisitedLocationId = visitedLocationId
        };
        var response = await client.PutAsJsonAsync($"/animals/{animal.AnimalId}/locations", updateVisitedLocation);
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _dbContext.DisposeAsync();
    }

    [Fact]
    public async Task PutFirstVisitedLocationToChippingLocation_ShouldReturn400BadRequest()
    {
        using var scope = _fixture.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _transaction = await _dbContext.Database.BeginTransactionAsync();
        
        var location = new LocationPoint() { Latitude = 10, Longitude = 10 };
        var animal = new Animal()
        {
            AccountId = 1, AnimalTypes = new List<AnimalType>(),
            ChippingLocationPoint = new LocationPoint() { Latitude = 1, Longitude = 1 }, SoftDeleted = false,
            LiveStatus = LiveStatusType.Alive, VisitedLocations = new List<AnimalVisitedLocation>() {new() {LocationPoint = location}}
        };
        _dbContext.Animals.Add(animal);
        _dbContext.Locations.Add(location);
        await _dbContext.SaveChangesAsync();
        using var client = _fixture.CreateClient();
        var updateVisitedLocation = new UpdateVisitedLocationDto()
        {
            LocationPointId = animal.ChippingLocationPoint.LocationPointId,
            VisitedLocationId = animal.VisitedLocations.First().AnimalVisitedLocationId
        };

        var response = await client.PutAsJsonAsync($"/animals/{animal.AnimalId}/locations", updateVisitedLocation);
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _dbContext.DisposeAsync();
    }
    
    [Fact]
    public async Task PutVisitedLocationToSame_ShouldReturn400BadRequest()
    {
        using var scope = _fixture.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _transaction = await _dbContext.Database.BeginTransactionAsync();
        
        var location = new LocationPoint() { Latitude = 10, Longitude = 10 };
        var animal = new Animal()
        {
            AccountId = 1, AnimalTypes = new List<AnimalType>(),
            ChippingLocationPoint = new LocationPoint() { Latitude = 1, Longitude = 1 }, SoftDeleted = false,
            LiveStatus = LiveStatusType.Alive, VisitedLocations = new List<AnimalVisitedLocation>() {new() {LocationPoint = location}}
        };
        _dbContext.Animals.Add(animal);
        _dbContext.Locations.Add(location);
        await _dbContext.SaveChangesAsync();
        using var client = _fixture.CreateClient();
        var updateVisitedLocation = new UpdateVisitedLocationDto()
        {
            LocationPointId = location.LocationPointId,
            VisitedLocationId = animal.VisitedLocations.First().AnimalVisitedLocationId
        };

        var response = await client.PutAsJsonAsync($"/animals/{animal.AnimalId}/locations", updateVisitedLocation);
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _dbContext.DisposeAsync();
    }
    
    [Fact]
    public async Task PutVisitedLocationToPrevious_ShouldReturn400BadRequest()
    {
        using var scope = _fixture.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _transaction = await _dbContext.Database.BeginTransactionAsync();
        
        var location = new LocationPoint() { Latitude = 10, Longitude = 10 };
        var secondLocation = new LocationPoint() { Latitude = 20, Longitude = 10 };
        var animal = new Animal()
        {
            AccountId = 1, AnimalTypes = new List<AnimalType>(),
            ChippingLocationPoint = new LocationPoint() { Latitude = 1, Longitude = 1 }, SoftDeleted = false,
            LiveStatus = LiveStatusType.Alive,
            VisitedLocations = new List<AnimalVisitedLocation>()
                { new() { LocationPoint = location }, new() { LocationPoint = secondLocation } }
        };
        _dbContext.Animals.Add(animal);
        _dbContext.Locations.AddRange(location, secondLocation);
        await _dbContext.SaveChangesAsync();
        using var client = _fixture.CreateClient();
        var updateVisitedLocation = new UpdateVisitedLocationDto()
        {
            LocationPointId = location.LocationPointId,
            VisitedLocationId = animal.VisitedLocations.Last().AnimalVisitedLocationId
        };

        var response = await client.PutAsJsonAsync($"/animals/{animal.AnimalId}/locations", updateVisitedLocation);
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _dbContext.DisposeAsync();
    }
    
    [Fact]
    public async Task PutVisitedLocationToNext_ShouldReturn400BadRequest()
    {
        using var scope = _fixture.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _transaction = await _dbContext.Database.BeginTransactionAsync();
        
        var location = new LocationPoint() { Latitude = 10, Longitude = 10 };
        var secondLocation = new LocationPoint() { Latitude = 20, Longitude = 10 };
        var animal = new Animal()
        {
            AccountId = 1, AnimalTypes = new List<AnimalType>(),
            ChippingLocationPoint = new LocationPoint() { Latitude = 1, Longitude = 1 }, SoftDeleted = false,
            LiveStatus = LiveStatusType.Alive,
            VisitedLocations = new List<AnimalVisitedLocation>()
                { new() { LocationPoint = location }, new() { LocationPoint = secondLocation } }
        };
        _dbContext.Animals.Add(animal);
        _dbContext.Locations.AddRange(location, secondLocation);
        await _dbContext.SaveChangesAsync();
        using var client = _fixture.CreateClient();
        var updateVisitedLocation = new UpdateVisitedLocationDto()
        {
            LocationPointId = secondLocation.LocationPointId,
            VisitedLocationId = animal.VisitedLocations.First().AnimalVisitedLocationId
        };

        var response = await client.PutAsJsonAsync($"/animals/{animal.AnimalId}/locations", updateVisitedLocation);
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _dbContext.DisposeAsync();
    }

    [Fact]
    public async Task PutVisitedLocationWithoutAuth_ShouldReturn401Unauthorized()
    {
        using var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = null;
        
        var response = await client.PutAsJsonAsync("/animals/1/locations", new UpdateVisitedLocationDto());
        
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PutVisitedLocationOfNotExistingAnimal_ShouldReturn404NotFound()
    {
        using var client = _fixture.CreateClient();

        var response = await client.PutAsJsonAsync($"/animals/{int.MaxValue}/locations",
            new UpdateVisitedLocationDto() { VisitedLocationId = 1, LocationPointId = 1 });
        
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
    
    [Fact]
    public async Task PutNotExistingVisitedLocation_ShouldReturn404NotFound()
    {
        using var scope = _fixture.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _transaction = await _dbContext.Database.BeginTransactionAsync();
        
        var location = new LocationPoint() { Latitude = 10, Longitude = 10 };
        var secondLocation = new LocationPoint() { Latitude = 20, Longitude = 10 };
        var animal = new Animal()
        {
            AccountId = 1, AnimalTypes = new List<AnimalType>(),
            ChippingLocationPoint = new LocationPoint() { Latitude = 1, Longitude = 1 }, SoftDeleted = false,
            LiveStatus = LiveStatusType.Alive,
            VisitedLocations = new List<AnimalVisitedLocation>()
                { new() { LocationPoint = location }, new() { LocationPoint = secondLocation } }
        };
        _dbContext.Animals.Add(animal);
        _dbContext.Locations.AddRange(location, secondLocation);
        await _dbContext.SaveChangesAsync();
        using var client = _fixture.CreateClient();
        var updateVisitedLocation = new UpdateVisitedLocationDto()
        {
            LocationPointId = secondLocation.LocationPointId,
            VisitedLocationId = int.MaxValue
        };

        var response = await client.PutAsJsonAsync($"/animals/{animal.AnimalId}/locations", updateVisitedLocation);
        
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _dbContext.DisposeAsync();
    }
    [Fact]
    public async Task PutVisitedLocationToNotExistingLocation_ShouldReturn404NotFound()
    {
        using var scope = _fixture.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _transaction = await _dbContext.Database.BeginTransactionAsync();
        
        var location = new LocationPoint() { Latitude = 10, Longitude = 10 };
        var secondLocation = new LocationPoint() { Latitude = 20, Longitude = 10 };
        var animal = new Animal()
        {
            AccountId = 1, AnimalTypes = new List<AnimalType>(),
            ChippingLocationPoint = new LocationPoint() { Latitude = 1, Longitude = 1 }, SoftDeleted = false,
            LiveStatus = LiveStatusType.Alive,
            VisitedLocations = new List<AnimalVisitedLocation>()
                { new() { LocationPoint = location }, new() { LocationPoint = secondLocation } }
        };
        _dbContext.Animals.Add(animal);
        _dbContext.Locations.AddRange(location, secondLocation);
        await _dbContext.SaveChangesAsync();
        using var client = _fixture.CreateClient();
        var updateVisitedLocation = new UpdateVisitedLocationDto()
        {
            LocationPointId = int.MaxValue,
            VisitedLocationId = animal.VisitedLocations.First().AnimalVisitedLocationId
        };

        var response = await client.PutAsJsonAsync($"/animals/{animal.AnimalId}/locations", updateVisitedLocation);
        
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _dbContext.DisposeAsync();
    }
}