using System.Net;
using System.Net.Http.Json;
using AnimalChippingWebApi.Models.Account;
using DatabaseLayer.Context;
using DatabaseLayer.Models;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.Admin.AccountServices;

namespace WevApi.Tests.AccountEndpoints;

public class PutAccountEndpointTest : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _fixture;
    private IDbContextTransaction _transaction;
    private AppDbContext _dbContext;
    
    public PutAccountEndpointTest(CustomWebApplicationFactory fixture)
    {
        _fixture = fixture;
    }
    
    [Fact]
    public async Task PutAccount_ShouldUpdateAccount()
    {
        using var scope = _fixture.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _transaction = await _dbContext.Database.BeginTransactionAsync();
        
        using var client = _fixture.CreateClient();
        var dto = _dbContext.Accounts.FirstOrDefault(x => x.AccountId == 1);
        var updateDto = new UserUpdateDto() {Email = "newemail@mail.com", Password = "123456789", FirstName = "test", LastName = "test"};
        
        var response = await client.PutAsJsonAsync("/accounts/1", updateDto);
        var updatedAccount = await response.Content.ReadFromJsonAsync<AccountDto>();
        
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(dto.AccountId, updatedAccount.AccountId);
        Assert.NotEqual(dto.FirstName, updatedAccount.FirstName);
        Assert.NotEqual(dto.LastName, updatedAccount.LastName);
        Assert.NotEqual(dto.Email, updatedAccount.Email);
        
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
    [InlineData("Joe", "Doe", "john.com", "password")]
    public async Task PutAccountWithInvalidData_ShouldReturn404BadRequest(string email, string firstName,
        string lastName, string password)
    {
        using var client = _fixture.CreateClient();
        var dto = await client.GetFromJsonAsync<AccountDto>("/accounts/1");
        var updateDto = new UserUpdateDto() {Email = email, Password = password, FirstName = firstName, LastName = lastName};
        
        var response = await client.PutAsJsonAsync("/accounts/1", updateDto);
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PutAccountWithoutAuth_ShouldReturn401Forbidden()
    {
        using var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = null;
        var updateDto = new UserUpdateDto() {Email = "test@mail.com", Password = "password", FirstName = "test", LastName = "test"};
        
        var response = await client.PutAsJsonAsync("/accounts/1", updateDto);
        
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PutNotOwnerAccount_ShouldReturn403Forbidden()
    {
        using var scope = _fixture.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _transaction = await _dbContext.Database.BeginTransactionAsync();
        
        using var client = _fixture.CreateClient();
        var account = new Account() {FirstName = "test", LastName = "test", Email = "test@example.com", Password = "password"};
        _dbContext.Accounts.Add(account);
        await _dbContext.SaveChangesAsync();
        var updateAccount = new UpdateAccountDto() {Email = "test10@example.com", Password = "password", FirstName = "test", LastName = "test"};
        
        var response = await client.PutAsJsonAsync($"/accounts/{account.AccountId}", updateAccount);
        
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _dbContext.DisposeAsync();
    }

    [Fact]
    public async Task PutNotExistingAccount_ShouldReturn403Forbidden()
    {
        using var client = _fixture.CreateClient();
        var updateAccount = new UpdateAccountDto() {Email = "test@test.com", Password = "password", FirstName = "test", LastName = "test"};
        
        var response = await client.PutAsJsonAsync($"/accounts/{int.MaxValue}", updateAccount);
        
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PutAccountEmailToExistingEmail_ShouldReturn409Conflict()
    {
        using var scope = _fixture.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _transaction = await _dbContext.Database.BeginTransactionAsync();
        
        using var client = _fixture.CreateClient();
        var account = new Account() {FirstName = "test", LastName = "test", Email = "test@example.com", Password = "password"};
        _dbContext.Accounts.Add(account);
        await _dbContext.SaveChangesAsync();
        var updateAccount = new UpdateAccountDto() {Email = account.Email, Password = "password", FirstName = "test", LastName = "test"};
        
        var response = await client.PutAsJsonAsync("/accounts/1", updateAccount);
        
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _dbContext.DisposeAsync();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1000)]
    public async Task PutAccountWithInvalidId_ShouldReturn400BadRequest(int invalidId)
    {
        using var client = _fixture.CreateClient();
        var updateAccount = new UpdateAccountDto() {Email = "example@example.com", Password = "password", FirstName = "test", LastName = "test"};
        
        var response = await client.PutAsJsonAsync($"/accounts/{invalidId}", updateAccount);
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}