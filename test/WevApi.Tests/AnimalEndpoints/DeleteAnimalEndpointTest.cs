using System.Net;
using DatabaseLayer.Context;
using DatabaseLayer.Models;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace WevApi.Tests.AnimalEndpoints;

public class DeleteAnimalEndpointTest : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _fixture;
    private AppDbContext _dbContext;
    private IDbContextTransaction _transaction;

    public DeleteAnimalEndpointTest(CustomWebApplicationFactory fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task DeleteAnimal_ShouldDeleteAnimal()
    {
        using var scope = _fixture.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _transaction = await _dbContext.Database.BeginTransactionAsync();

        var animal = new Animal()
            { AccountId = 1, AnimalTypes = new List<AnimalType>(), ChippingLocationPoint = new LocationPoint(), SoftDeleted = false};
        _dbContext.Animals.Add(animal);
        await _dbContext.SaveChangesAsync();
        using var client = _fixture.CreateClient();
        
        var response = await client.DeleteAsync($"/animals/{animal.AnimalId}");
        response.EnsureSuccessStatusCode();
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(_dbContext.Animals.Any(x => x.AnimalId == animal.AnimalId));
        
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _dbContext.DisposeAsync();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public async Task DeleteAnimalWithInvalidId_ShouldReturn400BadRequest(int invalidId)
    {
        using var client = _fixture.CreateClient();
        
        var response = await client.DeleteAsync($"/animals/{invalidId}");
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteAnimalWithVisitedLocation_ShouldReturn400BadRequest()
    {
        using var scope = _fixture.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _transaction = await _dbContext.Database.BeginTransactionAsync();

        var visitedLocation = new List<AnimalVisitedLocation>()
        {
            new () {LocationPoint = new LocationPoint() {Latitude = 1, Longitude = 1}},
            new () {LocationPoint = new LocationPoint() {Latitude = 1, Longitude = 1}}
        };
        _dbContext.AnimalVisitedLocations.AddRange(visitedLocation);
        var animal = new Animal()
        {
            AccountId = 1, AnimalTypes = new List<AnimalType>(),
            ChippingLocationPoint = new LocationPoint() { Latitude = 10, Longitude = 10 }, SoftDeleted = false,
            VisitedLocations = visitedLocation
        };
        _dbContext.Animals.Add(animal);
        await _dbContext.SaveChangesAsync();
        using var client = _fixture.CreateClient();
        
        var response = await client.DeleteAsync($"/animals/{animal.AnimalId}");
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _dbContext.DisposeAsync();
    }
    
    [Fact]
    public async Task DeleteAnimalWithCorrectVisitedLocation_ShouldDeleteAnimal()
    {
        using var scope = _fixture.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _transaction = await _dbContext.Database.BeginTransactionAsync();

        var point = new LocationPoint() {Latitude = 10, Longitude = 10};
        _dbContext.Locations.Add(point);
        var visitedLocation = new List<AnimalVisitedLocation>()
        {
            new () {LocationPoint = new LocationPoint() {Latitude = 1, Longitude = 1}},
            new () {LocationPoint = new LocationPoint() {Latitude = 1, Longitude = 1}},
            new () {LocationPoint = point}
        };
        _dbContext.AnimalVisitedLocations.AddRange(visitedLocation);
        var animal = new Animal()
        {
            AccountId = 1, AnimalTypes = new List<AnimalType>(),
            ChippingLocationPoint = point, SoftDeleted = false,
            VisitedLocations = visitedLocation
        };
        _dbContext.Animals.Add(animal);
        await _dbContext.SaveChangesAsync();
        using var client = _fixture.CreateClient();
        
        var response = await client.DeleteAsync($"/animals/{animal.AnimalId}");
        response.EnsureSuccessStatusCode();
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _dbContext.DisposeAsync();
    }

    [Fact]
    public async Task DeleteAnimalWithoutAuth_ShouldReturn401Unauthorized()
    {
        using var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = null;
        
        var response = await client.DeleteAsync("/animals/1");
        
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteNotExistingAnimal_ShouldReturn404NotFound()
    {
        using var client = _fixture.CreateClient();
        
        var response = await client.DeleteAsync($"/animals/{int.MaxValue}");
        
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}