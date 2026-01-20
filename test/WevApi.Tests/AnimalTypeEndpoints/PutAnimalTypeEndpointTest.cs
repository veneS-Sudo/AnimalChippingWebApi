using System.Net;
using System.Net.Http.Json;
using DatabaseLayer.Context;
using DatabaseLayer.Models;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.Admin.Dto;
using ServiceLayer.Admin.TypeAnimalServices;

namespace WevApi.Tests.AnimalTypeEndpoints;

public class PutAnimalTypeEndpointTest : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _fixture;
    private AppDbContext _dbContext;
    private IDbContextTransaction _transaction;

    public PutAnimalTypeEndpointTest(CustomWebApplicationFactory fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task PutAnimalType_ShouldUpdateAnimalType()
    {
        var scope = _fixture.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _transaction = await _dbContext.Database.BeginTransactionAsync();
        
        using var client = _fixture.CreateClient();
        var animalType = new AnimalType() {Type = "Bird"};
        var newAnimal = new CreateUpdateAnimalTypeDto() {Type = "Shark"};
        await _dbContext.AnimalTypes.AddAsync(animalType);
        await _dbContext.SaveChangesAsync();
        var response = await client.PutAsJsonAsync($"/animals/types/{animalType.AnimalTypeId}", newAnimal);
        var resultDto = await response.Content.ReadFromJsonAsync<AnimalTypeDto>();
        
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(resultDto);
        Assert.Equal(animalType.AnimalTypeId, resultDto.AnimalTypeId);
        Assert.NotEqual(animalType.Type, resultDto.Type);
        
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _dbContext.DisposeAsync();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("     ")]
    public async Task PutAnimalTypeWithInvalidType_ShouldReturn400BadRequest(string? type)
    {
        using var scope = _fixture.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _transaction = await _dbContext.Database.BeginTransactionAsync();

        var at = new AnimalType() { Type = "Bird" };
        _dbContext.AnimalTypes.Add(at);
        await _dbContext.SaveChangesAsync();
        using var client = _fixture.CreateClient();

        var response = await client.PutAsJsonAsync($"/animals/types/{at.AnimalTypeId}",
            new CreateUpdateAnimalTypeDto() { Type = type });
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _dbContext.DisposeAsync();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public async Task PutAnimalTypeWithInvalidId_ShouldReturn400BadRequest(int? id)
    {
        using var client = _fixture.CreateClient();

        var response = await client.PutAsJsonAsync($"/animals/types/{id}",
            new CreateUpdateAnimalTypeDto() { Type = "Shark" });
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PutAnimalTypeWithoutAuth_ShouldReturn401Unauthorized()
    {
        using var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = null;

        var response = await client.PutAsJsonAsync("/animals/types/1",
            new CreateUpdateAnimalTypeDto() { Type = "Shark" });
        
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PutNotExistingAnimalType_ShouldReturn404NotFound()
    {
        using var client = _fixture.CreateClient();

        var response = await client.PutAsJsonAsync($"/animals/types/{int.MaxValue}",
            new CreateUpdateAnimalTypeDto() { Type = "Shark" });
        
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PutExistingAnimalType_ShouldReturn409Conflict()
    {
        using var scope = _fixture.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _transaction = await _dbContext.Database.BeginTransactionAsync();

        var firstType = new AnimalType() { Type = "Bird" };
        var secondType = new AnimalType() { Type = "Shark" };
        _dbContext.AnimalTypes.AddRange(firstType, secondType);
        await _dbContext.SaveChangesAsync();
        using var client = _fixture.CreateClient();

        var response = await client.PutAsJsonAsync($"/animals/types/{secondType.AnimalTypeId}",
            new CreateUpdateAnimalTypeDto() { Type = "Bird" });
        
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _dbContext.DisposeAsync();
    }
}