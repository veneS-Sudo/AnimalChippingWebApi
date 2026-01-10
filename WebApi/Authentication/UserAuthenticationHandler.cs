using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using DatabaseLayer.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using ServiceLayer.Authentication;

namespace AnimalChippingWebApi.Authentication;

public class UserAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string AuthenticationSchemeName = "Basic";
    private readonly IUserAuthenticationService _accountService;
    public UserAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger,
        UrlEncoder encoder, ISystemClock clock, IUserAuthenticationService accountService) : base(options, logger, encoder, clock)
    {
        _accountService = accountService;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string? message = null;
        var endpoint = Context.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<IAllowAnonymous>() != null)
        {
            return AuthenticateResult.NoResult();
        }

        if (!Request.Headers.ContainsKey(HeaderNames.Authorization))
        {
            message = "Missing Authorization Header";
            return AuthenticateResult.Fail(message);
        }

        if (!Request.Headers[HeaderNames.Authorization].ToString().StartsWith("Basic"))
        {
            message = "Invalid Authorization Header: missing 'Basic' at start";
            return AuthenticateResult.Fail(message, new AuthenticationProperties(new Dictionary<string, string>() {{"Error", message}}!));
        }
        Account? user = null;
        try
        {
            var authHeader = AuthenticationHeaderValue.Parse(Request.Headers[HeaderNames.Authorization]);
            var credentialBytes = Convert.FromBase64String(authHeader.Parameter);
            var credentials = Encoding.UTF8.GetString(credentialBytes).Split(new [] { ':' }, 2);
            var login = credentials[0];
            var password = credentials[1];
            user = await _accountService.AuthenticateAsync(login, password);
        }
        catch 
        {
            return AuthenticateResult.Fail("Invalid Authorization Header");
        }

        if (user is null)
            return AuthenticateResult.Fail("Invalid Username or Password");
        
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.AccountId.ToString()),
            new Claim(ClaimTypes.Name, string.Join(' ', user.FirstName, user.LastName))
        };
        
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Headers["WWW-Authenticate"] = "Basic realm=\"\", charset=\"UTF-8\"";
        return base.HandleChallengeAsync(properties);
    }
}