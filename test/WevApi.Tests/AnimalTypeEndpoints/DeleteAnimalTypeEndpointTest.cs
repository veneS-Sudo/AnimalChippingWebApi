using System.Net;
using DatabaseLayer.Context;
using DatabaseLayer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace WevApi.Tests.AnimalTypeEndpoints;

public class DeleteAnimalTypeEndpointTest : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _fixture;
    private AppDbContext _dbContext;
    private IDbContextTransaction _transaction;

    public DeleteAnimalTypeEndpointTest(CustomWebApplicationFactory fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task DeleteAnimalType_ShouldDeleteAnimalType()
    {
        using var scope = _fixture.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _transaction = await _dbContext.Database.BeginTransactionAsync();
        
        var animalType = new AnimalType() {Type = "Bird"};
        _dbContext.AnimalTypes.Add(animalType);
        await _dbContext.SaveChangesAsync();
        using var client = _fixture.CreateClient();

        var response = await client.DeleteAsync($"/animals/types/{animalType.AnimalTypeId}");
        response.EnsureSuccessStatusCode();
        animalType = await _dbContext.AnimalTypes.FirstOrDefaultAsync(x => x.AnimalTypeId == animalType.AnimalTypeId);
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(animalType);
        
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _dbContext.DisposeAsync();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public async Task DeleteAnimalTypeWithInvalidId_ShouldReturn400BadRequest(int animalTypeId)
    {
        using var client = _fixture.CreateClient();
        
        var response = await client.DeleteAsync($"/animals/types/{animalTypeId}");
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteAnimalTypeBindingToAnimal_ShouldReturn400BadRequest()
    {
        using var scope = _fixture.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _transaction = await _dbContext.Database.BeginTransactionAsync();
        
        var animalType = new AnimalType() {Type = "Bird"};
        _dbContext.AnimalTypes.Add(animalType);
        var animal = new Animal()
        {
            AccountId = 1, AnimalTypes = new List<AnimalType> { animalType },
            ChippingLocationPoint = new LocationPoint()
        };
        _dbContext.Animals.Add(animal);
        await _dbContext.SaveChangesAsync();
        using var client = _fixture.CreateClient();

        var response = await client.DeleteAsync($"/animals/types/{animalType.AnimalTypeId}");
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _dbContext.DisposeAsync();
    }

    [Fact]
    public async Task DeleteAnimalTypeWithoutAuth_ShouldReturn401Unauthorized()
    {
        using var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = null;
        
        var response = await client.DeleteAsync("/animals/types/1");
        
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteNotExistingAnimalType_ShouldReturn404NotFound()
    {
        using var client = _fixture.CreateClient();
        
        var response = await client.DeleteAsync($"/animals/types/{int.MaxValue}");
        
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}