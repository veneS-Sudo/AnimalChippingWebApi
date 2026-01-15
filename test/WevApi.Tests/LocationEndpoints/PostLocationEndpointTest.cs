using System.Net;
using System.Net.Http.Json;
using AnimalChippingWebApi.DataBaseLayer.Context;
using AnimalChippingWebApi.Models.Location;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.Admin.AnimalServices.LocationServices;

namespace WevApi.Tests.LocationEndpoints;

public class PostLocationEndpointTest : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _fixture;
    private AppDbContext _dbContext;
    private IDbContextTransaction _transaction;

    public PostLocationEndpointTest(CustomWebApplicationFactory fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task PostLocation_CreatesLocation()
    {
        using var scope = _fixture.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _transaction = await _dbContext.Database.BeginTransactionAsync();
        
        var client = _fixture.CreateClient();
        var location = new LocationCreateUpdateDto() {Longitude = 1, Latitude = 1};
        
        var response = await client.PostAsJsonAsync("/locations", location);
        var createdLocation = await response.Content.ReadFromJsonAsync<LocationPointDto>();
        
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(createdLocation);
        Assert.Equal(location.Longitude, createdLocation.Longitude);
        Assert.Equal(location.Latitude, createdLocation.Latitude);
        
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _dbContext.DisposeAsync();
    }

    [Theory]
    [InlineData(-91, 100)]
    [InlineData(91, 100)]
    [InlineData(1, 181)]
    [InlineData(1, -181)]
    [InlineData(1000, 1000)]
    public async Task PostLocationWithInvalidData_ShouldReturn400BadRequest(int latitude, int longitude)
    {
        var client = _fixture.CreateClient();
        var location = new LocationCreateUpdateDto() {Longitude = longitude, Latitude = latitude};
        
        var response = await client.PostAsJsonAsync("/locations", location);
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostLocationWithoutAuth_ShouldReturn401Unauthorized()
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = null;
        var location = new LocationCreateUpdateDto() {Longitude = 1, Latitude = 1};
        
        var response = await client.PostAsJsonAsync("/locations", location);
        
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostLocationWithSameData_ShouldReturn409Conflict()
    {
        using var scope = _fixture.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _transaction = await _dbContext.Database.BeginTransactionAsync();
        
        var client = _fixture.CreateClient();
        var location = new LocationCreateUpdateDto() {Longitude = 1, Latitude = 1};
        
        var response = await client.PostAsJsonAsync("/locations", location);
        response.EnsureSuccessStatusCode();
        response = await client.PostAsJsonAsync("/locations", location);
        
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _dbContext.DisposeAsync();
    }
}