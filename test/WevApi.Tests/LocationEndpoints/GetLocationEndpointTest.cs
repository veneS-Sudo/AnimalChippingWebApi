using System.Net;
using System.Net.Http.Json;
using AnimalChippingWebApi.DataBaseLayer.Context;
using DatabaseLayer.Models;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.Admin.AnimalServices.LocationServices;

namespace WevApi.Tests.LocationEndpoints;

public class GetLocationEndpointTest : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _fixture;
    private AppDbContext _dbContext;
    private IDbContextTransaction _transaction;

    public GetLocationEndpointTest(CustomWebApplicationFactory fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetLocation_ShouldReturnLocation()
    {
        using var scope = _fixture.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _transaction = await _dbContext.Database.BeginTransactionAsync();

        var client = _fixture.CreateClient();
        var location = new LocationPoint() {Latitude = 1, Longitude = 1};
        _dbContext.Locations.Add(location);
        await _dbContext.SaveChangesAsync();

        var response = await client.GetAsync($"/locations/{location.LocationPointId}");
        var responseLocation = await response.Content.ReadFromJsonAsync<LocationPointDto>();
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(responseLocation);
        Assert.Equal(location.LocationPointId, responseLocation.LocationId);
        Assert.Equal(location.Latitude, responseLocation.Latitude);
        Assert.Equal(location.Longitude, responseLocation.Longitude);
        
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _dbContext.DisposeAsync();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public async Task GetLocationWithInvalidId_ShouldReturn400BadRequest(int invalidId)
    {
        var client = _fixture.CreateClient();
        
        var response = await client.GetAsync($"/locations/{invalidId}");
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetLocationWithoutAuth_ShouldReturn401Unauthorized()
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = null;
        
        var response = await client.GetAsync("/locations/1");
        
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetNotExistingLocation_ShouldReturn404NotFound()
    {
        
        var client = _fixture.CreateClient();
        
        var response = await client.GetAsync("/locations/100000");
        
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}