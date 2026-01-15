using DatabaseLayer.Context;
using DatabaseLayer.Models;
using ServiceLayer.Exceptions;

namespace ServiceLayer.Registration;

public class RegistrationUserService : IRegistrationUserService
{
    private readonly AppDbContext _context;

    public RegistrationUserService(AppDbContext context)
    {
        _context = context;
    }

    public int RegistrationUser(UserRegistrationDto dto)
    {
        if (_context.Accounts.Any(x => x.Email == dto.Email))
            throw new UniqueValueException($"Account with email {dto.Email} already exists");
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