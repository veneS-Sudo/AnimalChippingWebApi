using AnimalChippingWebApi.Core.Models;
using AnimalChippingWebApi.DataBaseLayer.Context;
using Microsoft.EntityFrameworkCore;

namespace ServiceLayer.Authentication;

public class UserAuthenticationService : IUserAuthenticationService
{
    private readonly AppDbContext _context;

    public UserAuthenticationService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Account?> AuthenticateAsync(string login, string password)
    {
        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Email == login && a.Password == password);
        return account;
    }
}