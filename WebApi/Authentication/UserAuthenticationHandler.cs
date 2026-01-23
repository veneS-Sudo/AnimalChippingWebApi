using System.Net.Http.Headers;
using System.Security.Authentication;
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
    private readonly ILogger<UserAuthenticationHandler> _logger;

    public UserAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger,
        UrlEncoder encoder, ISystemClock clock, IUserAuthenticationService accountService,
        ILogger<UserAuthenticationHandler> logger1) : base(options, logger, encoder, clock)
    {
        _accountService = accountService;
        _logger = logger1;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        
        string? message = null;
        var endpoint = Context.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<IAllowAnonymous>() != null
            && !Request.Headers.ContainsKey(HeaderNames.Authorization))
        {
            return AuthenticateResult.NoResult();
        }

        _logger.LogInformation("Trying to authenticate");
        if (!Request.Headers.ContainsKey(HeaderNames.Authorization))
        {
            message = "Missing Authorization Header";
            _logger.LogWarning("Authentication error: {AuthenticationError}", message);
            return AuthenticateResult.Fail(message);
        }

        if (!Request.Headers[HeaderNames.Authorization].ToString().StartsWith("Basic"))
        {
            message = "Invalid Authorization Header: missing 'Basic' at start";
            _logger.LogWarning("Authentication error: {AuthenticationError}", message);
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
            _logger.LogWarning("Authentication error: {AuthenticationError}", "Invalid Authorization Header");
            return AuthenticateResult.Fail("Invalid Authorization Header");
        }

        if (user is null)
        {
            _logger.LogWarning("Authentication error: {AuthenticationError}", "Invalid Username or Password");
            return AuthenticateResult.Fail("Invalid Username or Password");
        }
        
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.AccountId.ToString()),
            new Claim(ClaimTypes.Name, string.Join(' ', user.FirstName, user.LastName))
        };
        
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        _logger.LogInformation("Authentication success");
        return AuthenticateResult.Success(ticket);
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Headers["WWW-Authenticate"] = "Basic realm=\"\", charset=\"UTF-8\"";
        return base.HandleChallengeAsync(properties);
    }
}