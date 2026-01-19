using System.Net;
using System.Net.Http.Json;
using AnimalChippingWebApi.Models.Animal;
using DatabaseLayer.Context;
using DatabaseLayer.Models;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace WevApi.Tests.AnimalEndpoints;

public class PostAnimalEndpointTest : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _fixture;
    private AppDbContext _dbContext;
    private IDbContextTransaction _transaction;

    public PostAnimalEndpointTest(CustomWebApplicationFactory fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task PostAnimal_ShouldCreateAnimal()
    {
        using var scope = _fixture.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var location = new LocationPoint() {Latitude = 1, Longitude = 1, LocationPointId = 1};
        var animalType = new AnimalType() { Type = "Bird", AnimalTypeId = 1 };
        _dbContext.Locations.Add(location);
        _dbContext.AnimalTypes.Add(animalType);
        await _dbContext.SaveChangesAsync();
        
        using var client = _fixture.CreateClient();
        var createAnimal = new CreateAnimalDto()
        {
            AccountId = 1, AnimalTypeIds = new List<long>() { animalType.AnimalTypeId },
            ChippingLocationPointId = location.LocationPointId, Gender = "Male", Height = 10, Length = 10, Weight = 10
        };
        var response = await client.PostAsJsonAsync("/animals", createAnimal);
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<AnimalDto>();
        
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(dto);
        Assert.Equal(createAnimal.AccountId, dto.AnimalId);
        Assert.Equal(createAnimal.ChippingLocationPointId, dto.ChippingLocationPointId);
        
        _dbContext.AnimalTypes.RemoveRange(_dbContext.AnimalTypes);
        _dbContext.Locations.RemoveRange(_dbContext.Locations);
        _dbContext.Animals.RemoveRange(_dbContext.Animals);
        await _dbContext.DisposeAsync();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public async Task PostAnimalWithInvalidAnimalTypes_ShouldReturn400BadRequest(long elem)
    {
        using var client = _fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/animals",
            new CreateAnimalDto()
            {
                AccountId = 1, AnimalTypeIds = new List<long>() { elem }, Gender = "MALE", Height = 10, Length = 10,
                Weight = 10, ChippingLocationPointId = 1
            });
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    
    [Fact]
    public async Task PostAnimalWithEmptyAnimalTypes_ShouldReturn400BadRequest()
    {
        using var client = _fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/animals",
            new CreateAnimalDto()
            {
                AccountId = 1, AnimalTypeIds = new List<long>(), Gender = "MALE", Height = 10, Length = 10,
                Weight = 10, ChippingLocationPointId = 1
            });
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(0, 1, 1)]
    [InlineData(1, 0, 1)]
    [InlineData(1, 1, 0)]
    [InlineData(1, 1, -10)]
    [InlineData(1, -10, 1)]
    [InlineData(-10, 1, 1)]
    [InlineData(-10, -1, -10)]
    [InlineData(0, 0, 0)]
    public async Task PostAnimalWithInvalidAnimalIndicators_ShouldReturn400BadRequest(float weight, float height, float length)
    {
        using var client = _fixture.CreateClient();
        
        var response = await client.PostAsJsonAsync("/animals",
            new CreateAnimalDto()
            {
                AccountId = 1, AnimalTypeIds = new List<long>() {1}, Gender = "MALE", Height = height, Length = length,
                Weight = weight, ChippingLocationPointId = 1
            });
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("Helicopter")]
    public async Task PostAnimalWithInvalidGender_ShouldReturn400BadRequest(string gender)
    {
        using var client = _fixture.CreateClient();
        
        var response = await client.PostAsJsonAsync("/animals",
            new CreateAnimalDto()
            {
                AccountId = 1, AnimalTypeIds = new List<long>() {1}, Gender = gender, Height = 10, Length = 10,
                Weight = 10, ChippingLocationPointId = 1
            });
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task PostAnimalWithInvalidChipperId_ShouldReturn400BadRequest(int chipperId)
    {
        using var client = _fixture.CreateClient();
        
        var response = await client.PostAsJsonAsync("/animals",
            new CreateAnimalDto()
            {
                AccountId = chipperId, AnimalTypeIds = new List<long>() {1}, Gender = "Male", Height = 10, Length = 10,
                Weight = 10, ChippingLocationPointId = 1
            });
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task PostAnimalWithInvalidChippingLocationId_ShouldReturn400BadRequest(int chippingLocationId)
    {
        using var client = _fixture.CreateClient();
        
        var response = await client.PostAsJsonAsync("/animals",
            new CreateAnimalDto()
            {
                AccountId = 1, AnimalTypeIds = new List<long>() {1}, Gender = "Male", Height = 10, Length = 10,
                Weight = 10, ChippingLocationPointId = chippingLocationId
            });
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostAnimalWithoutAuth_ShouldReturn401Unauthorized()
    {
        using var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = null;
        
        var response = await client.PostAsJsonAsync("/animals",
            new CreateAnimalDto()
            {
                AccountId = 1, AnimalTypeIds = new List<long>() {1}, Gender = "Male", Height = 10, Length = 10,
                Weight = 10, ChippingLocationPointId = 1
            });
        
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostAnimalWithNotExistingAnimalType_ShouldReturn404NotFound()
    {
        using var scope = _fixture.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _transaction = await _dbContext.Database.BeginTransactionAsync();
        
        _dbContext.Locations.Add(new LocationPoint() {Latitude = 1, Longitude = 1});
        await _dbContext.SaveChangesAsync();
        using var client = _fixture.CreateClient();
        
        var response = await client.PostAsJsonAsync("/animals",
            new CreateAnimalDto()
            {
                AccountId = 1, AnimalTypeIds = new List<long>() {int.MaxValue}, Gender = "Male", Height = 10, Length = 10,
                Weight = 10, ChippingLocationPointId = 1
            });
        
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _dbContext.DisposeAsync();
    }
    
    [Fact]
    public async Task PostAnimalWithNotExistingChipperId_ShouldReturn404NotFound()
    {
        using var scope = _fixture.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _transaction = await _dbContext.Database.BeginTransactionAsync();
        
        _dbContext.AnimalTypes.Add(new AnimalType() { Type = "Bird" });
        _dbContext.Locations.Add(new LocationPoint() {Latitude = 1, Longitude = 1});
        await _dbContext.SaveChangesAsync();
        using var client = _fixture.CreateClient();
        
        var response = await client.PostAsJsonAsync("/animals",
            new CreateAnimalDto()
            {
                AccountId = int.MaxValue, AnimalTypeIds = new List<long>() {1}, Gender = "Male", Height = 10, Length = 10,
                Weight = 10, ChippingLocationPointId = 1
            });
        
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _dbContext.DisposeAsync();
    }
    
    [Fact]
    public async Task PostAnimalWithNotExistingChippingLocationId_ShouldReturn404NotFound()
    {
        using var scope = _fixture.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _transaction = await _dbContext.Database.BeginTransactionAsync();
        
        _dbContext.AnimalTypes.Add(new AnimalType() { Type = "Bird" });
        await _dbContext.SaveChangesAsync();
        using var client = _fixture.CreateClient();
        
        var response = await client.PostAsJsonAsync("/animals",
            new CreateAnimalDto()
            {
                AccountId = 1, AnimalTypeIds = new List<long>() {1}, Gender = "Male", Height = 10, Length = 10,
                Weight = 10, ChippingLocationPointId = int.MaxValue
            });
        
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _dbContext.DisposeAsync();
    }
    
    [Fact]
    public async Task PostAnimalWithSameAnimalType_ShouldReturn409Conflict()
    {
        using var scope = _fixture.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _transaction = await _dbContext.Database.BeginTransactionAsync();
        
        _dbContext.AnimalTypes.Add(new AnimalType() { Type = "Bird" });
        _dbContext.Locations.Add(new LocationPoint() {Latitude = 1, Longitude = 1});
        await _dbContext.SaveChangesAsync();
        using var client = _fixture.CreateClient();
        
        var response = await client.PostAsJsonAsync("/animals",
            new CreateAnimalDto()
            {
                AccountId = 1, AnimalTypeIds = new List<long>() {1, 1, 1}, Gender = "Male", Height = 10, Length = 10,
                Weight = 10, ChippingLocationPointId = 1
            });
        
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _dbContext.DisposeAsync();
    }
}