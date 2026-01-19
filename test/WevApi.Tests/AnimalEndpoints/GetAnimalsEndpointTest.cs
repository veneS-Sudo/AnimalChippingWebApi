using System.Net;
using System.Net.Http.Json;
using AnimalChippingWebApi.Models.Animal;
using DatabaseLayer.Context;
using DatabaseLayer.Models;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace WevApi.Tests.AnimalEndpoints;

public class GetAnimalsEndpointTest : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _fixture;
    private AppDbContext _dbContext;
    private IDbContextTransaction _transaction;

    public GetAnimalsEndpointTest(CustomWebApplicationFactory fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetAnimals_ShouldReturnAllAnimals()
    {
        using var scope = _fixture.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _transaction = await _dbContext.Database.BeginTransactionAsync();

        var animals = Enumerable.Range(1, 10).Select(i => new Animal()
        {
            AnimalId = i, AccountId = 1, ChippingLocationPoint = new LocationPoint() { Latitude = 1, Longitude = 1 },
            AnimalTypes = new List<AnimalType>()
        }).ToList();
        _dbContext.Animals.AddRange(animals);
        await _dbContext.SaveChangesAsync();
        using var client = _fixture.CreateClient();

        var response = await client.GetAsync("/animals/search");
        var responseAnimals = await response.Content.ReadFromJsonAsync<List<AnimalDto>>();
        response.EnsureSuccessStatusCode();
        
        Assert.NotNull(responseAnimals);
        Assert.NotEmpty(responseAnimals);
        Assert.Equal(animals.Count, responseAnimals.Count);
        Assert.Equal<AnimalDto>(animals.Select(x => new AnimalDto() { AnimalId = x.AnimalId }).ToList(), responseAnimals,
            new ValueComparer<AnimalDto>((x, y) => x.AnimalId == y.AnimalId, x => x.AnimalId.GetHashCode()));
        
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _dbContext.DisposeAsync();
    }

    [Theory]
    [InlineData(-1, 10)]
    [InlineData(-10, 10)]
    [InlineData(5, -10)]
    [InlineData(5, 0)]
    public async Task GetAnimalsByInvalidSearchParameters_ShouldReturn400BadRequest(int from, int size)
    {
        using var client = _fixture.CreateClient();
        
        var response = await client.GetAsync($"/animals/search?from={from}&size={size}");
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("2015-20-20")]
    [InlineData("2015/01/01")]
    [InlineData("2015:01:01")]
    public async Task GetAnimalsWithInvalidStartDateParameter_ShouldReturn400BadRequest(string startDate)
    {
        using var client = _fixture.CreateClient();
        
        var response = await client.GetAsync($"/animals/search?startDateTime={startDate}");
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    
    [Theory]
    [InlineData("2015-20-20")]
    [InlineData("2015/01/01")]
    [InlineData("2015:01:01")]
    public async Task GetAnimalsWithInvalidEndDateParameter_ShouldReturn400BadRequest(string endDate)
    {
        using var client = _fixture.CreateClient();
        
        var response = await client.GetAsync($"/animals/search?endDateTime={endDate}");
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAnimalsWithInvalidLifeStatusParameter_ShouldReturn400BadRequest()
    {
        using var client = _fixture.CreateClient();
        var lifeStatus = "To be or not to be";
        
        var response = await client.GetAsync($"/animals/search?lifeStatus={lifeStatus}");
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAnimalsWithInvalidGenderParameter_ShouldReturn400BadRequest()
    {
        using var client = _fixture.CreateClient();
        var gender = "helicopter";
        
        var response = await client.GetAsync($"/animals/search?gender={gender}");
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public async Task GetAnimalsWithInvalidChipperIdParameter_ShouldReturn400BadRequest(int chipperId)
    {
        using var client = _fixture.CreateClient();
        
        var response = await client.GetAsync($"/animals/search?chipperId={chipperId}");
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public async Task GetAnimalsWithInvalidChippingLocationIdParameter_ShouldReturn400BadRequest(int chippingLocationId)
    {
        using var client = _fixture.CreateClient();
        
        var response = await client.GetAsync($"/animals/search?chippingLocationId={chippingLocationId}");
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAnimalsWithoutAuth_ShouldReturn401Unauthorized()
    {
        using var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = null;
        
        var response = await client.GetAsync("/animals/search");
        
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}