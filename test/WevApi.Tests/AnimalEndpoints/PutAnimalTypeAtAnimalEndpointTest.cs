using System.Net;
using System.Net.Http.Json;
using AnimalChippingWebApi.Models.Animal;
using DatabaseLayer.Context;
using DatabaseLayer.Models;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.Admin.Dto;

namespace WevApi.Tests.AnimalEndpoints;

public class PutAnimalTypeAtAnimalEndpointTest : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _fixture;
    private AppDbContext _dbContext;
    private IDbContextTransaction _transaction;

    public PutAnimalTypeAtAnimalEndpointTest(CustomWebApplicationFactory fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task PutAnimalTypeAtAnimal_ShouldUpdateAnimalTypeAtAnimal()
    {
        using var scope = _fixture.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var animalType = new AnimalType() { Type = "Bird" };
        var newAnimalType = new AnimalType() { Type = "Shark" };
        _dbContext.AnimalTypes.AddRange(animalType, newAnimalType);
        var animal = new Animal()
        {
            AccountId = 1, AnimalTypes = new List<AnimalType>() { animalType },
            ChippingLocationPoint = new LocationPoint(), SoftDeleted = false
        };
        _dbContext.Animals.Add(animal);
        await _dbContext.SaveChangesAsync();
        using var client = _fixture.CreateClient();
        var upAnimalType = new UpdateAnimalTypeInAnimalDataDto()
            { OldAnimalTypeId = animalType.AnimalTypeId, NewAnimalTypeId = newAnimalType.AnimalTypeId };
        
        var response = await client.PutAsJsonAsync($"/animals/{animal.AnimalId}/types", upAnimalType);
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<AnimalDto>();
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(dto);
        Assert.Contains(dto.AnimalTypes, x => x == newAnimalType.AnimalTypeId);
        Assert.DoesNotContain(dto.AnimalTypes, x => x == animalType.AnimalTypeId);
        
        await _dbContext.DisposeAsync();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public async Task PutAnimalTypeAtAnimalWithInvalidOldTypeId_ShouldReturn400BadRequest(long animalId)
    {
        using var client = _fixture.CreateClient();
        var upAnimalType = new UpdateAnimalTypeInAnimalDataDto() { OldAnimalTypeId = animalId, NewAnimalTypeId = 1 };

        var response = await client.PutAsJsonAsync("/animals/1/types", upAnimalType);
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public async Task PutAnimalTypeAtAnimalWithInvalidNewTypeId_ShouldReturn400BadRequest(long animalId)
    {
        using var client = _fixture.CreateClient();
        var upAnimalType = new UpdateAnimalTypeInAnimalDataDto() { OldAnimalTypeId = 1, NewAnimalTypeId = animalId };

        var response = await client.PutAsJsonAsync("/animals/1/types", upAnimalType);
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public async Task PutAnimalTypeAtAnimalWithInvalidAnimalId_ShouldReturn400BadRequest(long animalId)
    {
        using var client = _fixture.CreateClient();
        var upAnimalType = new UpdateAnimalTypeInAnimalDataDto() { OldAnimalTypeId = 1, NewAnimalTypeId = 1 };

        var response = await client.PutAsJsonAsync($"/animals/{animalId}/types", upAnimalType);
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PutAnimalTypeAtAnimalWithoutAuth_ShouldReturn401Unauthorized()
    {
        using var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = null;
        var upAnimalType = new UpdateAnimalTypeInAnimalDataDto() { OldAnimalTypeId = 1, NewAnimalTypeId = 1 };

        var response = await client.PutAsJsonAsync("/animals/1/types", upAnimalType);
        
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PutAnimalTypeAtNotExistingAnimal_shouldReturn404NotFound()
    {
        using var scope = _fixture.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _transaction = await _dbContext.Database.BeginTransactionAsync();
        
        var animalType = new AnimalType() { Type = "Bird" };
        var newAnimalType = new AnimalType() { Type = "Shark" };
        _dbContext.AnimalTypes.AddRange(animalType, newAnimalType);
        await _dbContext.SaveChangesAsync();
        using var client = _fixture.CreateClient();
        var upAnimalType = new UpdateAnimalTypeInAnimalDataDto()
            { OldAnimalTypeId = animalType.AnimalTypeId, NewAnimalTypeId = newAnimalType.AnimalTypeId };

        var response = await client.PutAsJsonAsync($"/animals/{int.MaxValue}/types", upAnimalType);
        
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _dbContext.DisposeAsync();
    }
    
    [Fact]
    public async Task PutNotExistingAnimalTypeAtAnimal_shouldReturn404NotFound()
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
        var upAnimalType = new UpdateAnimalTypeInAnimalDataDto()
            { OldAnimalTypeId = animalType.AnimalTypeId, NewAnimalTypeId = int.MaxValue };

        var response = await client.PutAsJsonAsync($"/animals/{animal.AnimalId}/types", upAnimalType);
        
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _dbContext.DisposeAsync();
    }
    
    [Fact]
    public async Task PutExistingAnimalTypeAtAnimal_shouldReturn409Conflict()
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
        var upAnimalType = new UpdateAnimalTypeInAnimalDataDto()
            { OldAnimalTypeId = animalType.AnimalTypeId, NewAnimalTypeId = animalType.AnimalTypeId };

        var response = await client.PutAsJsonAsync($"/animals/{animal.AnimalId}/types", upAnimalType);
        
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _dbContext.DisposeAsync();
    }
    
}