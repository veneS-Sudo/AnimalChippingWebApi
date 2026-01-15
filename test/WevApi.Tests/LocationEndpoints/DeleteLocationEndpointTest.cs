using System.Net;
using AnimalChippingWebApi.DataBaseLayer.Context;
using DatabaseLayer.Models;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace WevApi.Tests.LocationEndpoints;

public class DeleteLocationEndpointTest : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _fixture;
    private AppDbContext _dbContext;
    private IDbContextTransaction _transaction;

    public DeleteLocationEndpointTest(CustomWebApplicationFactory fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task DeleteLocation_ShouldDeleteLocation()
    {
        var scope = _fixture.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _transaction = await _dbContext.Database.BeginTransactionAsync();
        
        var client = _fixture.CreateClient();
        var point = new LocationPoint() { Latitude = 1, Longitude = -1 };
        _dbContext.Locations.Add(point);
        await _dbContext.SaveChangesAsync();
        var response = await client.DeleteAsync($"/locations/{point.LocationPointId}");
        
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _dbContext.DisposeAsync();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-1000)]
    public async Task DeleteLocationWithInvalidId_ShouldReturn400BadRequest(int id)
    {
        var client = _fixture.CreateClient();
        
        var response = await client.DeleteAsync($"/locations/{id}");
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteLocationWithLinkedAnimal_ShouldReturn400BadRequest()
    {
        
        var scope = _fixture.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _transaction = await _dbContext.Database.BeginTransactionAsync();
        
        var client = _fixture.CreateClient();
        var point = new LocationPoint() { Latitude = 1, Longitude = -1 };
        var animal = new Animal()
            { ChippingLocationPoint = point, AnimalTypes = new List<AnimalType>(), AccountId = 1 };
        _dbContext.Locations.Add(point);
        _dbContext.Animals.Add(animal);
        await _dbContext.SaveChangesAsync();
        
        var response = await client.DeleteAsync($"/locations/{point.LocationPointId}");
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _dbContext.DisposeAsync();
    }

    [Fact]
    public async Task DeleteLocationWithoutAuth_ShouldReturn401Unauthorized()
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = null;
        
        var response = await client.DeleteAsync("/locations/1");
        
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteNotExistingLocation_ShouldReturn404NotFound()
    {
        
        var client = _fixture.CreateClient();
        
        var response = await client.DeleteAsync($"/locations/{int.MaxValue}");
        
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}