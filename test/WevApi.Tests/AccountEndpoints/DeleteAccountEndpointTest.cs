using System.Net;
using DatabaseLayer.Context;
using DatabaseLayer.Models;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace WevApi.Tests.AccountEndpoints;

public class DeleteAccountEndpointTest : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _fixture;
    private IDbContextTransaction _transaction;
    private AppDbContext _dbContext;

    public DeleteAccountEndpointTest(CustomWebApplicationFactory fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task DeleteOwnedAccount_ShouldDeleteAccount()
    {
        using var scope = _fixture.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _transaction = await _dbContext.Database.BeginTransactionAsync();
        
        using var client = _fixture.CreateClient();
        var accountId = 1;

        var response = await client.DeleteAsync($"/accounts/{accountId}");
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _dbContext.DisposeAsync();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1000)]
    public async Task DeleteAccountWithInvalidId_ShouldReturn400BadRequest(int id)
    {
        using var client = _fixture.CreateClient();

        var response = await client.DeleteAsync($"/accounts/{id}");
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteAccountLinkedWithAnimal_ShouldReturn400BadRequest()
    {
        using var scope = _fixture.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _transaction = await _dbContext.Database.BeginTransactionAsync();
        using var client = _fixture.CreateClient();

        _dbContext.Animals.Add(new Animal()
        {
            AccountId = 1, AnimalId = 1, AnimalTypes = new List<AnimalType>() { new() { Type = "Bird" } },
            ChippingLocationPoint = new LocationPoint()
        });
        await _dbContext.SaveChangesAsync();
        
        var response = await client.DeleteAsync("/accounts/1");
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _dbContext.DisposeAsync();
    }

    [Fact]
    public async Task DeleteAccountWithOutAuth_ShouldReturn401Unauthorized()
    {
        using var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = null; 
        var response = await client.DeleteAsync("/accounts/1");
        
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteNotOwnedAccount_ShouldReturn403Forbidden()
    {
        using var scope = _fixture.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _transaction = await _dbContext.Database.BeginTransactionAsync();
        
        using var client = _fixture.CreateClient();
        var account = new Account() {FirstName = "test", LastName = "test", Email = "test@example.com", Password = "password"};
        _dbContext.Accounts.Add(account);
        await _dbContext.SaveChangesAsync();
        
        var response = await client.DeleteAsync($"/accounts/{account.AccountId}");
        
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _dbContext.DisposeAsync();
    }

    [Fact]
    public async Task DeleteNotExistingAccount_ShouldReturn403Forbidden()
    {
        using var client = _fixture.CreateClient();
        
        var response = await client.DeleteAsync($"/accounts/{int.MaxValue}");
        
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}