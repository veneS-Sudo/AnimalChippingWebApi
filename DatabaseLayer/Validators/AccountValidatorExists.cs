using DatabaseLayer.Context;
using DatabaseLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace DatabaseLayer.Validators;

public class AccountValidatorExists : IGenericValidator<Account>
{
    private readonly AppDbContext _context;

    public AccountValidatorExists(AppDbContext context)
    {
        _context = context;
    }

    public bool Check(Account input)
    {
        var account = _context.Accounts
            .AsNoTracking()
            .FirstOrDefault(a => a.AccountId == input.AccountId);
        return account is not null;
    }

    public void CheckWithThrowExceptions(Account input, string message)
    {
        if(!Check(input))
            throw new ValidationExistsException(message);
    }
}