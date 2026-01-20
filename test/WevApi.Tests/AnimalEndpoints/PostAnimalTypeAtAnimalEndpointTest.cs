using System.Net;
using System.Net.Http.Json;
using AnimalChippingWebApi.Models.Animal;
using DatabaseLayer.Context;
using DatabaseLayer.Models;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace WevApi.Tests.AnimalEndpoints;

public class PostAnimalTypeAtAnimalEndpointTest : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _fixture;
    private AppDbContext _dbContext;
    private IDbContextTransaction _transaction;

    public PostAnimalTypeAtAnimalEndpointTest(CustomWebApplicationFactory fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task PostAnimalTypeAtAnimal_ShouldAddTypeToAnimal()
    {
        using var scope = _fixture.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _transaction = await _dbContext.Database.BeginTransactionAsync();

        var animal = new Animal()
        {
            AccountId = 1, AnimalTypes = new List<AnimalType>(),
            ChippingLocationPoint = new LocationPoint() { Latitude = 1, Longitude = 1 }
        };
        _dbContext.Animals.Add(animal);
        var animalType = new AnimalType() { Type = "Bird" };
        _dbContext.AnimalTypes.Add(animalType);
        await _dbContext.SaveChangesAsync();
        using var client = _fixture.CreateClient();

        var response = await client.PostAsync($"/animals/{animal.AnimalId}/types/{animalType.AnimalTypeId}", null);
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<AnimalDto>();
        
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(dto);
        Assert.Contains(dto.AnimalTypes, x => x == animalType.AnimalTypeId);
        
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _dbContext.DisposeAsync();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public async Task PostAnimalTypeAtAnimalWithInvalidId_ShouldReturn400BadRequest(long id)
    {
        using var client = _fixture.CreateClient();

        var response = await client.PostAsync($"/animals/{id}/types/1", null);
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public async Task PostAnimalTypeAtAnimalWithInvalidTypeId_ShouldReturn400BadRequest(long id)
    {
        using var client = _fixture.CreateClient();

        var response = await client.PostAsync($"/animals/1/types/{id}", null);
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostAnimalTypeAtAnimalWithoutAuth_ShouldReturn401Unauthorized()
    {
        using var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = null;

        var response = await client.PostAsync("/animals/1/types/1", null);
        
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostAnimalTypeAtNotExistingAnimal_ShouldReturn404NotFound()
    {
        using var client = _fixture.CreateClient();

        var response = await client.PostAsync($"/animals/{int.MaxValue}/types/1", null);
        
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostNotExistingAnimalTypeAtAnimal_ShouldReturn404NotFound()
    {
        using var scope = _fixture.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _transaction = await _dbContext.Database.BeginTransactionAsync();
        
        var animal = new Animal()
        {
            AccountId = 1, AnimalTypes = new List<AnimalType>(),
            ChippingLocationPoint = new LocationPoint() { Latitude = 1, Longitude = 1 }
        };
        _dbContext.Animals.Add(animal);
        await _dbContext.SaveChangesAsync();
        using var client = _fixture.CreateClient();

        var response = await client.PostAsync($"/animals/{animal.AnimalId}/types/{int.MaxValue}", null);
        
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _dbContext.DisposeAsync();
    }

    [Fact]
    public async Task PostAlreadyExistingAnimalTypeAtAnimal_ShouldReturn409Conflict()
    {
        using var scope = _fixture.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _transaction = await _dbContext.Database.BeginTransactionAsync();

        var animalType = new AnimalType() { Type = "Bird" };
        _dbContext.AnimalTypes.Add(animalType);
        var animal = new Animal()
        {
            AccountId = 1, AnimalTypes = new List<AnimalType>() { animalType},
            ChippingLocationPoint = new LocationPoint() { Latitude = 1, Longitude = 1 }
        };
        _dbContext.Animals.Add(animal);
        await _dbContext.SaveChangesAsync();
        using var client = _fixture.CreateClient();

        var response = await client.PostAsync($"/animals/{animal.AnimalId}/types/{animalType.AnimalTypeId}", null);
        
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _dbContext.DisposeAsync();
    }
}