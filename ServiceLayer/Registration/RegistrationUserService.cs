using DatabaseLayer.Context;
using DatabaseLayer.Models;
using Microsoft.Extensions.Logging;
using ServiceLayer.Exceptions;

namespace ServiceLayer.Registration;

public class RegistrationUserService : IRegistrationUserService
{
    private readonly AppDbContext _context;
    private readonly ILogger<RegistrationUserService> _logger;

    public RegistrationUserService(AppDbContext context, ILogger<RegistrationUserService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public int RegistrationUser(UserRegistrationDto dto)
    {
        _logger.LogInformation("Trying register user with {Email}", dto.Email);
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
        _logger.LogInformation("Success register user with id {AccountId}", account.AccountId);
        return account.AccountId;
    }
}