using System.Net;
using System.Net.Http.Json;
using AnimalChippingWebApi.Models.VisitedLocation;
using DatabaseLayer.Context;
using DatabaseLayer.Models;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace WevApi.Tests.VisitedLocationEndpoints;

public class VisitedLocationSearchEndpointTest : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _fixture;
    private AppDbContext _dbContext;
    private IDbContextTransaction _transaction;

    public VisitedLocationSearchEndpointTest(CustomWebApplicationFactory fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetAnimalVisitedLocation_ShouldReturnAnimalVisitedLocations()
    {
        using var scope = _fixture.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _transaction = await _dbContext.Database.BeginTransactionAsync();

        var rnd = new Random();
        var count = rnd.Next(1, 89);
        var visitedLocations = Enumerable.Range(2, count).Select(i => new AnimalVisitedLocation()
            { LocationPoint = new LocationPoint() { Latitude = i, Longitude = i } }).ToList();
        _dbContext.AnimalVisitedLocations.AddRange(visitedLocations);
        var animal = new Animal()
        {
            AccountId = 1, AnimalTypes = new List<AnimalType>(),
            ChippingLocationPoint = new LocationPoint() { Latitude = 1, Longitude = 1 }, SoftDeleted = false,
            LiveStatus = LiveStatusType.Alive, VisitedLocations = visitedLocations
        };
        _dbContext.Animals.Add(animal);
        await _dbContext.SaveChangesAsync();
        using var client = _fixture.CreateClient();

        var response = await client.GetAsync($"/animals/{animal.AnimalId}/locations?size={count}");
        response.EnsureSuccessStatusCode();
        var visitedLocationsDto = await response.Content.ReadFromJsonAsync<List<VisitedLocationDto>>();
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(visitedLocationsDto);
        Assert.NotEmpty(visitedLocationsDto);
        Assert.Equal(count, visitedLocationsDto.Count);
        Assert.Equal(
            visitedLocations.Select(x => x.AnimalVisitedLocationId)
                .ToList(), visitedLocationsDto.Select(x => x.VisitedLocationId));
        
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _dbContext.DisposeAsync();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public async Task GetAnimalVisitedLocationsWithInvalidId_ShouldReturn400BadRequest(int id)
    {
        using var client = _fixture.CreateClient();

        var response = await client.GetAsync($"/animals/{id}/locations");
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(-10, 10)]
    [InlineData(10, -10)]
    public async Task GetAnimalVisitedLocationsWithInvalidSearchParameters_ShouldReturn400BadRequest(int from, int size)
    {
        using var client = _fixture.CreateClient();

        var response = await client.GetAsync($"/animals/1/locations?from={from}&size={size}");
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("2015-20-20")]
    [InlineData("2015/01/01")]
    [InlineData("2015:01:01")]
    public async Task GetAnimalVisitedLocationsWithInvalidStartDateParameters_ShouldReturn400BadRequest(string date)
    {
        using var client = _fixture.CreateClient();

        var response = await client.GetAsync($"/animals/1/locations?startDateTime={date}");
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    
    [Theory]
    [InlineData("2015-20-20")]
    [InlineData("2015/01/01")]
    [InlineData("2015:01:01")]
    public async Task GetAnimalVisitedLocationsWithInvalidEndDateParameters_ShouldReturn400BadRequest(string date)
    {
        using var client = _fixture.CreateClient();

        var response = await client.GetAsync($"/animals/1/locations?endDateTime={date}");
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAnimalVisitedLocationsWithoutAuth_ShouldReturn401Unauthorized()
    {
        using var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = null;

        var response = await client.GetAsync("/animals/1/locations");
        
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetNotExistsAnimalVisitedLocations_ShouldReturn404NotFound()
    {
        using var client = _fixture.CreateClient();

        var response = await client.GetAsync($"/animals/{int.MaxValue}/locations");
        
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}