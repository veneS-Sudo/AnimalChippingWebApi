using System.Net;
using System.Net.Http.Json;
using DatabaseLayer.Context;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.Admin.Dto;
using ServiceLayer.Admin.TypeAnimalServices;

namespace WevApi.Tests.AnimalTypeEndpoints;

public class PostAnimalTypeEndpointTest : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _fixture;
    private AppDbContext _dbContext;
    private IDbContextTransaction _transaction;

    public PostAnimalTypeEndpointTest(CustomWebApplicationFactory fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task PostAnimalType_ShouldCreateAnimalType()
    {
        var scope = _fixture.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _transaction = await _dbContext.Database.BeginTransactionAsync();

        var client = _fixture.CreateClient();
        var dto = new CreateUpdateAnimalTypeDto() { Type = "Bird" };

        var response = await client.PostAsJsonAsync("/animals/types", dto);
        response.EnsureSuccessStatusCode();
        var resultDto = await response.Content.ReadFromJsonAsync<AnimalTypeDto>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(resultDto);
        Assert.Equal(dto.Type, resultDto.Type);

        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _dbContext.DisposeAsync();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("     ")]
    public async Task PostAnimalTypeWithInvalidType_ShouldReturn400BadRequest(string type)
    {
        var client = _fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/animals/types", new { Type = type });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostAnimalTypeWithoutAuth_ShouldReturn401Unauthorized()
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = null;

        var response = await client.PostAsJsonAsync("/animals/types", new { Type = "Bird" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostSameAnimalTypeTwice_ShouldReturn409Conflict()
    {
        var scope = _fixture.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _transaction = await _dbContext.Database.BeginTransactionAsync();

        var client = _fixture.CreateClient();
        var dto = new CreateUpdateAnimalTypeDto() { Type = "Bird" };

        var response = await client.PostAsJsonAsync("/animals/types", dto);
        response.EnsureSuccessStatusCode();
        response = await client.PostAsJsonAsync("/animals/types", dto);
        
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _dbContext.DisposeAsync();
    }

}