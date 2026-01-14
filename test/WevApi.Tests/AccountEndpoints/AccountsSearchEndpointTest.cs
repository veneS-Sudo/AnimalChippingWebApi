using System.Net;
using System.Net.Http.Json;
using ServiceLayer.Admin.AccountServices;

namespace WevApi.Tests.AccountEndpoints;

public class AccountsSearchEndpointTest : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _fixture;

    public AccountsSearchEndpointTest(CustomWebApplicationFactory fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetAccounts_ReturnsAccounts()
    {
        var client = _fixture.CreateClient();
        
        var response = await client.GetAsync("/accounts/search");
        var accounts = await response.Content.ReadFromJsonAsync<List<AccountDto>>();

        if (accounts != null) Assert.NotEmpty(accounts);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(-10, 10)]
    [InlineData(10, -10)]
    public async Task GetAccountsWithInvalidSearchValues_ShouldReturn400BadRequest(int from, int size)
    {
        var client = _fixture.CreateClient();
        
        var response = await client.GetAsync($"/accounts/search?from={from}&size={size}");
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAccountsWithoutAuth_ShouldReturn401Unauthorized()
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = null;
        
        var response = await client.GetAsync("/accounts/search");
        
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}