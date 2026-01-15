using System.Net;
using System.Net.Http.Json;
using AnimalChippingWebApi.DataBaseLayer.Context;
using AnimalChippingWebApi.Models.Location;
using DatabaseLayer.Models;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.Admin.AnimalServices.LocationServices;

namespace WevApi.Tests.LocationEndpoints;

public class PutLocationEndpointTest : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _fixture;
    private AppDbContext _dbContext;
    private IDbContextTransaction _transaction;

    public PutLocationEndpointTest(CustomWebApplicationFactory fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task PutLocation_ShouldUpdateLocation()
    {
        var scope = _fixture.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _transaction = await _dbContext.Database.BeginTransactionAsync();

        var client = _fixture.CreateClient();
        var point = new LocationPoint() { Longitude = 1, Latitude = 1 };
        _dbContext.Locations.Add(point);
        await _dbContext.SaveChangesAsync();
        
        var updateLocation = new LocationCreateUpdateDto() { Longitude = 10, Latitude = 10 };
        var response = await client.PutAsJsonAsync($"/locations/{point.LocationPointId}", updateLocation);
        
        response.EnsureSuccessStatusCode();
        var updatedLocation = await response.Content.ReadFromJsonAsync<LocationPointDto>();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(updateLocation.Longitude, updatedLocation.Longitude);
        Assert.Equal(updateLocation.Latitude, updatedLocation.Latitude);
        
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _dbContext.DisposeAsync();
    }

    [Theory]
    [InlineData(-91, 100, 1)]
    [InlineData(91, 100, 1)]
    [InlineData(1, 181, 1)]
    [InlineData(1, -181, 1)]
    [InlineData(1000, 1000, 1)]
    [InlineData(0, 0, 0)]
    [InlineData(0, 0, -100)]
    public async Task PutLocationWithInvalidData_ShouldReturn400BadRequest(int latitude, int longitude, int id)
    {
        var client = _fixture.CreateClient();
        var point = new LocationCreateUpdateDto() {Latitude = latitude, Longitude = longitude};
        
        var response = await client.PutAsJsonAsync($"/locations/{id}", point);
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PutLocationWithoutAuth_ShouldReturn401Unauthorized()
    {
        
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = null;
        var point = new LocationCreateUpdateDto() {Latitude = 1, Longitude = 1};
        
        var response = await client.PutAsJsonAsync("/locations/1", point);
        
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PutNotExistingLocation_ShouldReturn404NotFound()
    {
        
        var client = _fixture.CreateClient();
        var point = new LocationCreateUpdateDto() { Latitude = 10, Longitude = 10 };
        
        var response = await client.PutAsJsonAsync($"/locations/{int.MaxValue}", point);
        
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PutLocationWithSameOtherData_ShouldReturn409Conflict()
    {
        var scope = _fixture.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _transaction = await _dbContext.Database.BeginTransactionAsync();

        var client = _fixture.CreateClient();
        var point1 = new LocationPoint() { Longitude = 1, Latitude = 1 };
        var point2 = new LocationPoint() { Longitude = 10, Latitude = 10 };
        _dbContext.Locations.AddRange(point1, point2);
        await _dbContext.SaveChangesAsync();
        
        var updateLocation = new LocationCreateUpdateDto() { Longitude = 1, Latitude = 1 };
        var response = await client.PutAsJsonAsync($"/locations/{point2.LocationPointId}", updateLocation);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _dbContext.DisposeAsync();
    }
}