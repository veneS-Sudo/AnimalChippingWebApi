using System.Net;
using System.Net.Http.Json;
using AnimalChippingWebApi.Models.Animal;
using DatabaseLayer.Context;
using DatabaseLayer.Models;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace WevApi.Tests.AnimalEndpoints;

public class GetAnimalEndpointTest : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _fixture;
    private AppDbContext _dbContext;
    private IDbContextTransaction _transaction;

    public GetAnimalEndpointTest(CustomWebApplicationFactory fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetAnimalById_ShouldReturnAnimal()
    {
        using var scope = _fixture.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _transaction = await _dbContext.Database.BeginTransactionAsync();
        
        using var client = _fixture.CreateClient();
        var animal = new Animal
        {
            AccountId = 1, ChippingLocationPoint = new LocationPoint() { Latitude = 1, Longitude = 1 },
            AnimalTypes = new List<AnimalType>()
        };
        _dbContext.Animals.Add(animal);
        await _dbContext.SaveChangesAsync();
        
        var response = await client.GetAsync($"/animals/{animal.AnimalId}");
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<AnimalDto>();
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(dto);
        Assert.Equal(animal.AnimalId, dto.AnimalId);
        Assert.Equal(animal.AccountId, dto.AnimalId);
        Assert.Equal(LiveStatusType.Alive.ToString(), dto.LiveStatus);
        Assert.Null(dto.DeathDateTime);
        
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _dbContext.DisposeAsync();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public async Task GetAnimalByInvalidId_ShouldReturn400BadRequest(int animalId)
    {
        using var client = _fixture.CreateClient();
        
        var response = await client.GetAsync($"/animals/{animalId}");
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAnimalByIdWithoutAuth_ShouldReturn401Unauthorized()
    {
        
        using var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = null;
        
        var response = await client.GetAsync("/animals/1");
        
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetNotExistingAnimal_ShouldReturn404NotFound()
    {
        using var client = _fixture.CreateClient();
        
        var response = await client.GetAsync($"/animals/{int.MaxValue}");
        
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}