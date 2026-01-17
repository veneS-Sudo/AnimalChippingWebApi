using System.Net;
using System.Net.Http.Json;
using DatabaseLayer.Context;
using DatabaseLayer.Models;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.Admin.AccountServices;
using ServiceLayer.Registration;

namespace WevApi.Tests.AccountEndpoints;

public class RegistrationEndpointTest : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _fixture;
    private IDbContextTransaction _transaction;
    private AppDbContext _dbContext;

    public RegistrationEndpointTest(CustomWebApplicationFactory fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task RegistrationAccount_ShouldRegisterAccount()
    {
        using var scope = _fixture.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _transaction = await _dbContext.Database.BeginTransactionAsync();
        
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = null;
        string firstName = "John", lastName = "Doe", email = "john.doe@example.com", password = "password";
        var registrationDto = new UserRegistrationDto()
            { FirstName = firstName, Email = email, LastName = lastName, Password = password };
        var response = await client.PostAsJsonAsync("/registration", registrationDto);
        
        response.EnsureSuccessStatusCode();
        var account = await response.Content.ReadFromJsonAsync<AccountDto>();
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(account);
        Assert.Equal(firstName, account.FirstName);
        Assert.Equal(lastName, account.LastName);
        Assert.Equal(email, account.Email);
        
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _dbContext.DisposeAsync();
    }

    [Theory]
    [InlineData("", "Doe", "john.doe@example.com", "password")]
    [InlineData("  ", "Doe", "john.doe@example.com", "password")]
    [InlineData("Joe", "", "john.doe@example.com", "password")]
    [InlineData("Joe", "  ", "john.doe@example.com", "password")]
    [InlineData("Joe", "Doe", "", "password")]
    [InlineData("Joe", "Doe", "  ", "password")]
    [InlineData("Joe", "Doe", " john.doe@example.com", "")]
    [InlineData("Joe", "Doe", " john.doe@example.com", "  ")]
    public async Task RegistrationAccount_ShouldReturn400BadRequest(string firstName, string lastName,
        string email, string password)
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = null;
        var registrationDto = new UserRegistrationDto()
            { FirstName = firstName, Email = email, LastName = lastName, Password = password };
        var response = await client.PostAsJsonAsync("/registration", registrationDto);
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RegistrationAccount_ShouldReturn403Forbidden()
    {
        var client = _fixture.CreateClient();
        string firstName = "John", lastName = "Doe", email = "test2@test.com", password = "123456789";
        var registrationDto = new UserRegistrationDto()
            { FirstName = firstName, Email = email, LastName = lastName, Password = password };
        
        
        var response = await client.PostAsJsonAsync("/registration", registrationDto);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RegistrationAccount_ShouldReturn409Conflict()
    {
        using var scope = _fixture.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _transaction = await _dbContext.Database.BeginTransactionAsync();
        
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = null;
        string firstName = "John", lastName = "Doe", email = "test2@test.com", password = "123456789";
        var registrationDto = new Account() { FirstName = firstName, Email = email, LastName = lastName, Password = password };
        _dbContext.Accounts.Add(registrationDto);
        await _dbContext.SaveChangesAsync();
        
        var response = await client.PostAsJsonAsync("/registration", registrationDto);
        
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _dbContext.DisposeAsync();
    }
}