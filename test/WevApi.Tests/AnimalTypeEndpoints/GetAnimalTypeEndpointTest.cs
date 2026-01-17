using System.Net;
using System.Net.Http.Json;
using DatabaseLayer.Context;
using DatabaseLayer.Models;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.Admin.TypeAnimalServices;

namespace WevApi.Tests.AnimalTypeEndpoints;

public class GetAnimalTypeEndpointTest : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _fixture;
    private AppDbContext _dbContext;
    private IDbContextTransaction _transaction;

    public GetAnimalTypeEndpointTest(CustomWebApplicationFactory fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetAnimalType_ShouldReturnsAnimalType()
    {
        var scope = _fixture.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _transaction = await _dbContext.Database.BeginTransactionAsync();
        
        var client = _fixture.CreateClient();
        var animalType = new AnimalType() {Type = "Bird"};
        _dbContext.AnimalTypes.Add(animalType);
        await _dbContext.SaveChangesAsync();

        var response = await client.GetAsync($"/animals/types/{animalType.AnimalTypeId}");
        var dto = await response.Content.ReadFromJsonAsync<AnimalTypeDto>();
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(dto);
        Assert.Equal(animalType.AnimalTypeId, dto.AnimalTypeId);
        Assert.Equal(animalType.Type, dto.Type);

        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _dbContext.DisposeAsync();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public async Task GetAnimalTypeWithInvalidId_ShouldReturn400BadRequest(int animalTypeId)
    {
        var client = _fixture.CreateClient();
        
        var response = await client.GetAsync($"/animals/types/{animalTypeId}");
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAnimalTypeWithoutAuth_ShouldReturn401Unauthorized()
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = null;
        
        var response = await client.GetAsync($"/animals/types/1");
        
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(1000)]
    [InlineData(int.MaxValue)]
    public async Task GetNotExistingAnimalType_ShouldReturn404NotFound(int animalTypeId)
    {
        var client = _fixture.CreateClient();
        
        var response = await client.GetAsync($"/animals/types/{animalTypeId}");
        
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

}