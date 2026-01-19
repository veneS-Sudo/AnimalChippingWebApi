using System.Net;
using System.Net.Http.Json;
using ServiceLayer.Admin.AccountServices;

namespace WevApi.Tests.AccountEndpoints;

public class GetAccountEndpointTest : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _fixture;

    public GetAccountEndpointTest(CustomWebApplicationFactory fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetAccount_ReturnsAccount()
    {
        using var client = _fixture.CreateClient();
        var response = await client.GetAsync("/accounts/1");

        var account = response.Content.ReadFromJsonAsync<AccountDto>();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(account);
    }

    [Theory]
    [InlineData(-99999)]
    [InlineData(0)]
    public async Task GetAccount_ShouldThrow400BadRequest(int id)
    {
        using var client = _fixture.CreateClient();
        var response = await client.GetAsync($"/accounts/{id}");
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAccountWithoutAuth_ShouldThrow401Unauthorized()
    {
        
        using var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = null;
        var response = await client.GetAsync($"/accounts/1");
        
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAccountWithNotExistsAccountId_ShouldThrow404NotFound()
    {
        using var client = _fixture.CreateClient();
        var response = await client.GetAsync($"/accounts/{int.MaxValue}");
        
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
    
}