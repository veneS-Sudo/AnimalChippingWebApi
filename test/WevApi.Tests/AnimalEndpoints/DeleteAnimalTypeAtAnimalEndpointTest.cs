using System.Net;
using System.Net.Http.Json;
using AnimalChippingWebApi.Models.Animal;
using DatabaseLayer.Context;
using DatabaseLayer.Models;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace WevApi.Tests.AnimalEndpoints;

public class DeleteAnimalTypeAtAnimalEndpointTest : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _fixture;
    private AppDbContext _dbContext;
    private IDbContextTransaction _transaction;

    public DeleteAnimalTypeAtAnimalEndpointTest(CustomWebApplicationFactory fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task DeleteAnimalTypeAtAnimal_ShouldDeleteAnimalType()
    {
        using var scope = _fixture.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _transaction = await _dbContext.Database.BeginTransactionAsync();
        
        var animalType = new AnimalType() { Type = "Bird" };
        var secondAnimalType = new AnimalType() { Type = "Shark" };
        _dbContext.AnimalTypes.AddRange(animalType, secondAnimalType);
        var animal = new Animal()
        {
            AccountId = 1, AnimalTypes = new List<AnimalType>() { animalType, secondAnimalType },
            ChippingLocationPoint = new LocationPoint(), SoftDeleted = false
        };
        _dbContext.Animals.Add(animal);
        await _dbContext.SaveChangesAsync();
        using var client = _fixture.CreateClient();

        var response = await client.DeleteAsync($"/animals/{animal.AnimalId}/types/{animalType.AnimalTypeId}");
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<AnimalDto>();
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(dto);
        Assert.DoesNotContain(dto.AnimalTypes, x => x == animalType.AnimalTypeId);
        
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _dbContext.DisposeAsync();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public async Task DeleteAnimalTypeAtAnimalWithInvalidAnimalId_ShouldReturn400BadRequest(int animalId)
    {
        using var client = _fixture.CreateClient();
        
        var response = await client.DeleteAsync($"/animals/{animalId}/types/1");
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public async Task DeleteAnimalTypeAtAnimalWithInvalidTypeId_ShouldReturn400BadRequest(int animalId)
    {
        using var scope = _fixture.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _transaction = await _dbContext.Database.BeginTransactionAsync();
        
        var animal = new Animal()
        {
            AccountId = 1, AnimalTypes = new List<AnimalType>(),
            ChippingLocationPoint = new LocationPoint(), SoftDeleted = false
        };
        _dbContext.Animals.Add(animal);
        await _dbContext.SaveChangesAsync();
        using var client = _fixture.CreateClient();
        
        var response = await client.DeleteAsync($"/animals/{animal.AnimalId}/types/{animalId}");
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _dbContext.DisposeAsync();
    }

    [Fact]
    public async Task DeleteSingleAnimalTypeAtAnimal_ShouldReturn400BadRequest()
    {
        using var scope = _fixture.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _transaction = await _dbContext.Database.BeginTransactionAsync();
        
        var animalType = new AnimalType() { Type = "Bird" };
        _dbContext.AnimalTypes.Add(animalType);
        var animal = new Animal()
        {
            AccountId = 1, AnimalTypes = new List<AnimalType>() { animalType },
            ChippingLocationPoint = new LocationPoint(), SoftDeleted = false
        };
        _dbContext.Animals.Add(animal);
        await _dbContext.SaveChangesAsync();
        using var client = _fixture.CreateClient();

        var response = await client.DeleteAsync($"/animals/{animal.AnimalId}/types/{animalType.AnimalTypeId}");
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _dbContext.DisposeAsync();
    }

    [Fact]
    public async Task DeleteAnimalTypeAtNotExistingAnimal_ShouldReturn404NotFound()
    {
        using var client = _fixture.CreateClient();
        
        var response = await client.DeleteAsync($"/animals/{int.MaxValue}/types/1");
        
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
    
    [Fact]
    public async Task DeleteNotExistingAnimalTypeAtAnimal_ShouldReturn404NotFound()
    {
        using var scope = _fixture.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _transaction = await _dbContext.Database.BeginTransactionAsync();
        
        var animal = new Animal()
        {
            AccountId = 1, AnimalTypes = new List<AnimalType>(),
            ChippingLocationPoint = new LocationPoint(), SoftDeleted = false
        };
        _dbContext.Animals.Add(animal);
        await _dbContext.SaveChangesAsync();
        using var client = _fixture.CreateClient();

        var response = await client.DeleteAsync($"/animals/{animal.AnimalId}/types/{int.MaxValue}");
        
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _dbContext.DisposeAsync();
    }
    
    [Fact]
    public async Task DeleteNotContainedAnimalTypeAtAnimal_ShouldReturn404NotFound()
    {
        using var scope = _fixture.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _transaction = await _dbContext.Database.BeginTransactionAsync();
        
        var animalType = new AnimalType() { Type = "Bird" };
        _dbContext.AnimalTypes.Add(animalType);
        var animal = new Animal()
        {
            AccountId = 1, AnimalTypes = new List<AnimalType>(),
            ChippingLocationPoint = new LocationPoint(), SoftDeleted = false
        };
        _dbContext.Animals.Add(animal);
        await _dbContext.SaveChangesAsync();
        using var client = _fixture.CreateClient();

        var response = await client.DeleteAsync($"/animals/{animal.AnimalId}/types/{animalType.AnimalTypeId}");
        
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _dbContext.DisposeAsync();
    }
    
}