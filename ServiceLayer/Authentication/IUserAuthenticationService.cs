using DatabaseLayer.Models;

namespace ServiceLayer.Authentication;

public interface IUserAuthenticationService
{
    Task<Account?> AuthenticateAsync(string login, string password);
}