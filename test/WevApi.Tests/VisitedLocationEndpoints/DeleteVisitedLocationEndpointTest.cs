using System.Net;
using System.Net.Http.Json;
using AnimalChippingWebApi.Models.VisitedLocation;
using DatabaseLayer.Context;
using DatabaseLayer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace WevApi.Tests.VisitedLocationEndpoints;

public class DeleteVisitedLocationEndpointTest : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _fixture;
    private AppDbContext _dbContext;
    private IDbContextTransaction _transaction;

    public DeleteVisitedLocationEndpointTest(CustomWebApplicationFactory fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task DeleteVisitedLocation_ShouldDeleteLocation()
    {
        using var scope = _fixture.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _transaction = await _dbContext.Database.BeginTransactionAsync();

        var location = new LocationPoint() { Latitude = 10, Longitude = 10 };
        var animal = new Animal()
        {
            AccountId = 1, AnimalTypes = new List<AnimalType>(),
            ChippingLocationPoint = new LocationPoint() { Latitude = 1, Longitude = 1 }, SoftDeleted = false,
            LiveStatus = LiveStatusType.Alive
        };
        _dbContext.Animals.Add(animal);
        _dbContext.Locations.Add(location);
        await _dbContext.SaveChangesAsync();

        using var client = _fixture.CreateClient();

        var response = await client.PostAsync($"/animals/{animal.AnimalId}/locations/{location.LocationPointId}", null);
        response.EnsureSuccessStatusCode();
        var visitedDto = await response.Content.ReadFromJsonAsync<VisitedLocationDto>();
        response = await client.DeleteAsync($"/animals/{animal.AnimalId}/locations/{visitedDto.VisitedLocationId}");
        response.EnsureSuccessStatusCode();
        animal = _dbContext.Animals.Include(a => a.VisitedLocations).First(x => x.AnimalId == animal.AnimalId);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(animal.VisitedLocations);
        Assert.DoesNotContain(animal.VisitedLocations, x => x.AnimalVisitedLocationId == visitedDto.VisitedLocationId);

        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _dbContext.DisposeAsync();
    }

    [Fact]
    public async Task DeleteFirstVisitedLocationAndSecondChippingVisitedLocation_ShouldDeleteLocation()
    {
        using var scope = _fixture.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var location = new LocationPoint() { Latitude = 10, Longitude = 10 };
        var secondLocation = new LocationPoint() { Latitude = 1, Longitude = 1 };
        var animal = new Animal()
        {
            AccountId = 1, AnimalTypes = new List<AnimalType>(),
            ChippingLocationPoint = secondLocation, SoftDeleted = false,
            LiveStatus = LiveStatusType.Alive,
        };
        _dbContext.Animals.Add(animal);
        _dbContext.Locations.AddRange(location, secondLocation);
        await _dbContext.SaveChangesAsync();
        using var client = _fixture.CreateClient();

        var response = await client.PostAsync($"/animals/{animal.AnimalId}/locations/{location.LocationPointId}", null);
        response.EnsureSuccessStatusCode();
        var visitedDto = await response.Content.ReadFromJsonAsync<VisitedLocationDto>();
        response = await client.PostAsync($"/animals/{animal.AnimalId}/locations/{secondLocation.LocationPointId}", null);
        response.EnsureSuccessStatusCode();
        response = await client.DeleteAsync($"/animals/{animal.AnimalId}/locations/{visitedDto.VisitedLocationId}");
        response.EnsureSuccessStatusCode();
        animal = _dbContext.Animals.Include(a => a.VisitedLocations).First(x => x.AnimalId == animal.AnimalId);
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(animal.VisitedLocations);
        
        _dbContext.Locations.RemoveRange(_dbContext.Locations);
        _dbContext.AnimalVisitedLocations.RemoveRange(_dbContext.AnimalVisitedLocations);
        _dbContext.Animals.RemoveRange(_dbContext.Animals);
        await _dbContext.DisposeAsync();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public async Task DeleteVisitedLocationWithInvalidAnimalId_ShouldReturn400BadRequest(int animalId)
    {
        using var client = _fixture.CreateClient();

        var response = await client.DeleteAsync($"/animals/{animalId}/locations/1");
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public async Task DeleteVisitedLocationWithInvalidLocationId_ShouldReturn400BadRequest(int locationId)
    {
        using var client = _fixture.CreateClient();

        var response = await client.DeleteAsync($"/animals/1/locations/{locationId}");
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteVisitedLocationWithoutAuth_ShouldReturn401Unauthorized()
    {
        using var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = null;

        var response = await client.DeleteAsync("/animals/1/locations/1");
        
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteVisitedLocationFromNotExistingAnimal_ShouldReturn404NotFound()
    {
        using var scope = _fixture.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _transaction = await _dbContext.Database.BeginTransactionAsync();
        
        _dbContext.Locations.Add(new LocationPoint() { Latitude = 10, Longitude = 10 });
        await _dbContext.SaveChangesAsync();
        using var client = _fixture.CreateClient();

        var response = await client.DeleteAsync($"/animals/{int.MaxValue}/locations/1");
        
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _dbContext.DisposeAsync();
    }

    [Fact]
    public async Task DeleteNotExistingVisitedLocation_ShouldReturn404NotFound()
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

        var response = await client.DeleteAsync($"/animals/{animal.AnimalId}/locations/{int.MaxValue}");
        
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _dbContext.DisposeAsync();
    }
    
    [Fact]
    public async Task DeleteNotContainedVisitedLocation_ShouldReturn404NotFound()
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
        _dbContext.Locations.Add(location);
        _dbContext.Animals.Add(animal);
        await _dbContext.SaveChangesAsync();
        using var client = _fixture.CreateClient();

        var response = await client.DeleteAsync($"/animals/{animal.AnimalId}/locations/{location.LocationPointId}");
        
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _dbContext.DisposeAsync();
    }
}