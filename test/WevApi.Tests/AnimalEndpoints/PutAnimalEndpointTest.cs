using System.Net;
using System.Net.Http.Json;
using AnimalChippingWebApi.Models.Animal;
using DatabaseLayer.Context;
using DatabaseLayer.Models;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace WevApi.Tests.AnimalEndpoints;

public class PutAnimalEndpointTest : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _fixture;
    private AppDbContext _dbContext;
    private IDbContextTransaction _transaction;

    public PutAnimalEndpointTest(CustomWebApplicationFactory fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task PutAnimal_ShouldUpdateAnimal()
    {
        using var scope = _fixture.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _transaction = await _dbContext.Database.BeginTransactionAsync();

        var animal = new Animal()
            { AccountId = 1, AnimalTypes = new List<AnimalType>(), ChippingLocationPoint = new LocationPoint() };
        var location = new LocationPoint() { Latitude = 1, Longitude = 1 };
        _dbContext.Animals.Add(animal);
        _dbContext.Locations.Add(location);
        await _dbContext.SaveChangesAsync();
        var updateAnimal = new UpdateAnimalDto()
        {
            AccountId = 1, ChippingLocationPointId = location.LocationPointId, Height = 10, Weight = 10, Length = 10,
            LiveStatus = "Alive", Gender = "Female"
        };
        using var client = _fixture.CreateClient();

        var response = await client.PutAsJsonAsync($"/animals/{animal.AnimalId}", updateAnimal);
        response.EnsureSuccessStatusCode();
        var resultDto = await response.Content.ReadFromJsonAsync<AnimalDto>();
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(resultDto);
        Assert.Equal(animal.AnimalId, resultDto.AnimalId);
        Assert.Equal(updateAnimal.Weight, resultDto.Weight);
        Assert.Equal(updateAnimal.Height, resultDto.Height);
        Assert.Equal(updateAnimal.Length, resultDto.Length);
        
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _dbContext.DisposeAsync();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10000)]
    public async Task PutAnimalWithInvalidAnimalId_ShouldReturn400BadRequest(long animalId)
    {
        using var client = _fixture.CreateClient();
        
        var response = await client.PutAsJsonAsync($"/animals/{animalId}", new UpdateAnimalDto());
        
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
    public async Task PutAnimalWithInvalidAnimalIndicators_ShouldReturn400BadRequest(float weight, float height, float length)
    {
        using var scope = _fixture.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _transaction = await _dbContext.Database.BeginTransactionAsync();

        var point = new LocationPoint() { Latitude = 1, Longitude = 1 };
        _dbContext.Locations.Add(point);
        _dbContext.Animals.Add(new Animal()
        {
            Weight = 1, Height = 1, Length = 1, AnimalTypes = new List<AnimalType>(),
            ChippingLocationPoint = point, AccountId = 1
        });
        await _dbContext.SaveChangesAsync();
        using var client = _fixture.CreateClient();

        var response = await client.PutAsJsonAsync($"/animals/1", new UpdateAnimalDto()
        {
            AccountId = 1, Height = height, Weight = weight, Length = length, LiveStatus = "Alive", Gender = "Female",
            ChippingLocationPointId = point.LocationPointId
        });
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _dbContext.DisposeAsync();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("Helicopter")]
    public async Task PutAnimalWithInvalidGender_ShouldReturn400BadRequest(string gender)
    {
        using var scope = _fixture.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _transaction = await _dbContext.Database.BeginTransactionAsync();

        var point = new LocationPoint() { Latitude = 1, Longitude = 1 };
        _dbContext.Locations.Add(point);
        _dbContext.Animals.Add(new Animal()
        {
            Weight = 1, Height = 1, Length = 1, AnimalTypes = new List<AnimalType>(),
            ChippingLocationPoint = point, AccountId = 1
        });
        using var client = _fixture.CreateClient();
        
        var response = await client.PutAsJsonAsync("/animals/1",
            new UpdateAnimalDto()
            {
                AccountId = 1, Gender = gender, Height = 10, Length = 10,
                Weight = 10, ChippingLocationPointId = 1, LiveStatus = "Alive"
            });
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _dbContext.DisposeAsync();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task PutAnimalWithInvalidChipperId_ShouldReturn400BadRequest(int chipperId)
    {
        using var client = _fixture.CreateClient();
        
        var response = await client.PutAsJsonAsync("/animals/1",
            new UpdateAnimalDto()
            {
                AccountId = chipperId, Gender = "Male", Height = 10, Length = 10,
                Weight = 10, ChippingLocationPointId = 1
            });
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task PutAnimalWithInvalidChippingLocationId_ShouldReturn400BadRequest(int chippingLocationId)
    {
        using var client = _fixture.CreateClient();
        
        var response = await client.PutAsJsonAsync("/animals/1",
            new UpdateAnimalDto()
            {
                AccountId = 1, Gender = "Male", Height = 10, Length = 10,
                Weight = 10, ChippingLocationPointId = chippingLocationId
            });
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PutAnimalLifeStatusToLiveFromDeath_ShouldReturn400BadRequest()
    {
        using var scope = _fixture.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _transaction = await _dbContext.Database.BeginTransactionAsync();
        
        var point = new LocationPoint() { Latitude = 1, Longitude = 1 };
        var animal = new Animal()
        {
            Weight = 1, Height = 1, Length = 1, AnimalTypes = new List<AnimalType>(),
            ChippingLocationPoint = point, AccountId = 1, LiveStatus = LiveStatusType.Dead
        };
        _dbContext.Locations.Add(point);
        _dbContext.Animals.Add(animal);
        await _dbContext.SaveChangesAsync();
        using var client = _fixture.CreateClient();
        
        var response = await client.PutAsJsonAsync($"/animals/{animal.AnimalId}",
            new UpdateAnimalDto()
            {
                AccountId = 1, Gender = "Male", Height = 10, Length = 10,
                Weight = 10, ChippingLocationPointId = point.LocationPointId, LiveStatus = "Alive"
            });
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _dbContext.DisposeAsync();
    }

    [Fact]
    public async Task PutAnimalChippingLocationToFirstVisitedLocation_ShouldReturn400BadRequest()
    {
        using var scope = _fixture.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _transaction = await _dbContext.Database.BeginTransactionAsync();
        
        var point = new LocationPoint() { Latitude = 1, Longitude = 1 };
        var vlp = new LocationPoint() { Latitude = 2, Longitude = 2 };
        var vl = new AnimalVisitedLocation() { LocationPoint = vlp };
        var animal = new Animal()
        {
            Weight = 1, Height = 1, Length = 1, AnimalTypes = new List<AnimalType>(),
            ChippingLocationPoint = point, AccountId = 1, LiveStatus = LiveStatusType.Dead,
            VisitedLocations = new List<AnimalVisitedLocation>() { vl }
        };
        _dbContext.Locations.AddRange(point, vlp);
        _dbContext.AnimalVisitedLocations.Add(vl);
        _dbContext.Animals.Add(animal);
        await _dbContext.SaveChangesAsync();
        using var client = _fixture.CreateClient();
        
        var response = await client.PutAsJsonAsync($"/animals/{animal.AnimalId}",
            new UpdateAnimalDto()
            {
                AccountId = 1, Gender = "Male", Height = 10, Length = 10,
                Weight = 10, ChippingLocationPointId = animal.VisitedLocations.First().LocationPoint.LocationPointId, LiveStatus = "Alive"
            });
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _dbContext.DisposeAsync();
    }
    
    [Fact]
    public async Task PutAnimalWithoutAuth_ShouldReturn401Unauthorized()
    {
        using var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = null;
        
        var response = await client.PutAsJsonAsync("/animals/1",
            new UpdateAnimalDto()
            {
                AccountId = 1, Gender = "Male", Height = 10, Length = 10,
                Weight = 10, ChippingLocationPointId = 1
            });
        
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
    
    [Fact]
    public async Task PutAnimalWithNotExistingChipperId_ShouldReturn404NotFound()
    {
        using var scope = _fixture.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _transaction = await _dbContext.Database.BeginTransactionAsync();
        
        var point = new LocationPoint() { Latitude = 1, Longitude = 1 };
        var animal = new Animal()
        {
            Weight = 1, Height = 1, Length = 1, AnimalTypes = new List<AnimalType>(),
            ChippingLocationPoint = point, AccountId = 1
        };
        _dbContext.Locations.Add(point);
        _dbContext.Animals.Add(animal);
        await _dbContext.SaveChangesAsync();
        using var client = _fixture.CreateClient();
        
        var response = await client.PutAsJsonAsync($"/animals/{animal.AnimalId}",
            new UpdateAnimalDto()
            {
                AccountId = int.MaxValue, Gender = "Male", Height = 10, Length = 10,
                Weight = 10, ChippingLocationPointId = point.LocationPointId, LiveStatus = "Alive"
            });
        
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _dbContext.DisposeAsync();
    }
    
    [Fact]
    public async Task PutAnimalWithNotExistingChippingLocationId_ShouldReturn404NotFound()
    {
        using var scope = _fixture.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _transaction = await _dbContext.Database.BeginTransactionAsync();
        
        var point = new LocationPoint() { Latitude = 1, Longitude = 1 };
        var animal = new Animal()
        {
            Weight = 1, Height = 1, Length = 1, AnimalTypes = new List<AnimalType>(),
            ChippingLocationPoint = point, AccountId = 1
        };
        _dbContext.Locations.Add(point);
        _dbContext.Animals.Add(animal);
        await _dbContext.SaveChangesAsync();
        using var client = _fixture.CreateClient();
        
        var response = await client.PutAsJsonAsync($"/animals/{animal.AnimalId}",
            new UpdateAnimalDto()
            {
                AccountId = 1, Gender = "Male", Height = 10, Length = 10,
                Weight = 10, ChippingLocationPointId = int.MaxValue, LiveStatus = "Alive"
            });
        
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _dbContext.DisposeAsync();
    }
    
    [Fact]
    public async Task PutAnimalWithNotExistingAnimalId_ShouldReturn404NotFound()
    {
        using var scope = _fixture.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _transaction = await _dbContext.Database.BeginTransactionAsync();
        
        var point = new LocationPoint() { Latitude = 1, Longitude = 1 };
        var animal = new Animal()
        {
            Weight = 1, Height = 1, Length = 1, AnimalTypes = new List<AnimalType>(),
            ChippingLocationPoint = point, AccountId = 1
        };
        _dbContext.Locations.Add(point);
        _dbContext.Animals.Add(animal);
        await _dbContext.SaveChangesAsync();
        using var client = _fixture.CreateClient();
        
        var response = await client.PutAsJsonAsync($"/animals/{int.MaxValue}",
            new UpdateAnimalDto()
            {
                AccountId = 1, Gender = "Male", Height = 10, Length = 10,
                Weight = 10, ChippingLocationPointId = point.LocationPointId, LiveStatus = "Alive"
            });
        
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _dbContext.DisposeAsync();
    }
}