using System.Net;
using System.Net.Http.Json;
using AnimalChippingWebApi.Models.VisitedLocation;
using DatabaseLayer.Context;
using DatabaseLayer.Models;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace WevApi.Tests.VisitedLocationEndpoints;

public class PostVisitedLocationEndpointTest : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _fixture;
    private AppDbContext _dbContext;
    private IDbContextTransaction _transaction;

    public PostVisitedLocationEndpointTest(CustomWebApplicationFactory fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task PostVisitedLocation_ShouldAddVisitedLocationToAnimal()
    {
        using var scope = _fixture.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _transaction = await _dbContext.Database.BeginTransactionAsync();
        
        var animal = new Animal()
        {
            AccountId = 1, AnimalTypes = new List<AnimalType>(),
            ChippingLocationPoint = new LocationPoint() { Latitude = 1, Longitude = 1 }, SoftDeleted = false,
            LiveStatus = LiveStatusType.Alive
        };
        var location = new LocationPoint() { Latitude = 10, Longitude = 10 };
        _dbContext.Animals.Add(animal);
        _dbContext.Locations.Add(location);
        await _dbContext.SaveChangesAsync();
        using var client = _fixture.CreateClient();

        var response = await client.PostAsync($"/animals/{animal.AnimalId}/locations/{location.LocationPointId}", null);
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<VisitedLocationDto>();
        
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(dto);
        Assert.Equal(location.LocationPointId, dto.LocationPointId);
        
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _dbContext.DisposeAsync();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public async Task PostVisitedLocationWithInvalidAnimalId_ShouldReturn400BadRequest(int animalId)
    {
        using var client = _fixture.CreateClient();

        var response = await client.PostAsync($"/animals/{animalId}/locations/1", null);
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public async Task PostVisitedLocationWithInvalidLocationId_ShouldReturn400BadRequest(int locationId)
    {
        using var client = _fixture.CreateClient();

        var response = await client.PostAsync($"/animals/1/locations/{locationId}", null);
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostVisitedLocationToDeathAnimal_ShouldReturn400BadRequest()
    {
        using var scope = _fixture.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _transaction = await _dbContext.Database.BeginTransactionAsync();
        
        var animal = new Animal()
        {
            AccountId = 1, AnimalTypes = new List<AnimalType>(),
            ChippingLocationPoint = new LocationPoint() { Latitude = 1, Longitude = 1 }, SoftDeleted = false,
            LiveStatus = LiveStatusType.Dead
        };
        var location = new LocationPoint() { Latitude = 10, Longitude = 10 };
        _dbContext.Animals.Add(animal);
        _dbContext.Locations.Add(location);
        await _dbContext.SaveChangesAsync();
        using var client = _fixture.CreateClient();

        var response = await client.PostAsync($"/animals/{animal.AnimalId}/locations/{location.LocationPointId}", null);
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _dbContext.DisposeAsync();
    }
    
    [Fact]
    public async Task PostVisitedLocationEqualsChippingLocation_ShouldReturn400BadRequest()
    {
        using var scope = _fixture.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _transaction = await _dbContext.Database.BeginTransactionAsync();
        
        var location = new LocationPoint() { Latitude = 1, Longitude = 1 };
        var animal = new Animal()
        {
            AccountId = 1, AnimalTypes = new List<AnimalType>(),
            ChippingLocationPoint = location, SoftDeleted = false,
            LiveStatus = LiveStatusType.Dead
        };
        _dbContext.Locations.Add(location);
        _dbContext.Animals.Add(animal);
        await _dbContext.SaveChangesAsync();
        using var client = _fixture.CreateClient();

        var response = await client.PostAsync($"/animals/{animal.AnimalId}/locations/{location.LocationPointId}", null);
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _dbContext.DisposeAsync();
    }
    
    [Fact]
    public async Task PostVisitedLocationEqualsChippingLocationInTheEnd_ShouldAddVisitedLocationToAnimal()
    {
        using var scope = _fixture.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _transaction = await _dbContext.Database.BeginTransactionAsync();
        
        var location = new LocationPoint() { Latitude = 10, Longitude = 10 };
        var chippingLocation = new LocationPoint() { Latitude = 1, Longitude = 1 };
        var animal = new Animal()
        {
            AccountId = 1, AnimalTypes = new List<AnimalType>(),
            ChippingLocationPoint = chippingLocation, SoftDeleted = false,
            LiveStatus = LiveStatusType.Alive, VisitedLocations = new List<AnimalVisitedLocation>() {new() {LocationPoint = location}}
        };
        _dbContext.Animals.Add(animal);
        _dbContext.Locations.Add(location);
        await _dbContext.SaveChangesAsync();
        using var client = _fixture.CreateClient();

        var response = await client.PostAsync($"/animals/{animal.AnimalId}/locations/{chippingLocation.LocationPointId}", null);
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<VisitedLocationDto>();
        
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(dto);
        Assert.Equal(chippingLocation.LocationPointId, dto.LocationPointId);
        
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _dbContext.DisposeAsync();
    }
    
    [Fact]
    public async Task PostAlreadyExistsVisitedLocation_ShouldReturn400BadRequest()
    {
        using var scope = _fixture.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _transaction = await _dbContext.Database.BeginTransactionAsync();
        
        var location = new LocationPoint() { Latitude = 10, Longitude = 10 };
        var chippingLocation = new LocationPoint() { Latitude = 1, Longitude = 1 };
        var animal = new Animal()
        {
            AccountId = 1, AnimalTypes = new List<AnimalType>(),
            ChippingLocationPoint = chippingLocation, SoftDeleted = false,
            LiveStatus = LiveStatusType.Alive, VisitedLocations = new List<AnimalVisitedLocation>() {new() {LocationPoint = location}}
        };
        _dbContext.Animals.Add(animal);
        _dbContext.Locations.Add(location);
        await _dbContext.SaveChangesAsync();
        using var client = _fixture.CreateClient();

        var response = await client.PostAsync($"/animals/{animal.AnimalId}/locations/{location.LocationPointId}", null);
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _dbContext.DisposeAsync();
    }

    [Fact]
    public async Task PostVisitedLocationWithoutAuthorizedUser_ShouldReturn401Unauthorized()
    {
        using var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = null;

        var response = await client.PostAsync("/animals/1/locations/1", null);
        
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostVisitedLocationToNotExistingAnimal_ShouldReturn404NotFound()
    {
        using var client = _fixture.CreateClient();

        var response = await client.PostAsync($"/animals/{int.MaxValue}/locations/1", null);
        
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
    
    [Fact]
    public async Task PostNotExistingVisitedLocation_ShouldReturn404NotFound()
    {
        using var scope = _fixture.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _transaction = await _dbContext.Database.BeginTransactionAsync();
        
        var animal = new Animal()
        {
            AccountId = 1, AnimalTypes = new List<AnimalType>(),
            ChippingLocationPoint = new LocationPoint() { Latitude = 1, Longitude = 1 }, SoftDeleted = false,
            LiveStatus = LiveStatusType.Alive
        };
        _dbContext.Animals.Add(animal);
        await _dbContext.SaveChangesAsync();
        using var client = _fixture.CreateClient();

        var response = await client.PostAsync($"/animals/{animal.AnimalId}/locations/{int.MaxValue}", null);
        
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _dbContext.DisposeAsync();
    }
}