using AnimalChippingWebApi.Core.Models;
using AnimalChippingWebApi.DataBaseLayer.Context;

namespace ServiceLayer.Registration;

public class RegistrationUserService
{
    private readonly AppDbContext _context;

    public RegistrationUserService(AppDbContext context)
    {
        _context = context;
    }

    public int RegistrationUser(UserRegistrationDto dto)
    {
        if (_context.Accounts.Any(x => x.Email == dto.Email))
            throw new Exception($"Account with email {dto.Email} already exists");
        var account = new Account
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            Password = dto.Password
        };

        _context.Accounts.Add(account);
        _context.SaveChanges();
        return account.AccountId;
    }
}