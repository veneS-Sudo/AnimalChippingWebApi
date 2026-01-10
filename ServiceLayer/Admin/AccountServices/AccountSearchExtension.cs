using DatabaseLayer.Models;

namespace ServiceLayer.Admin.AccountServices;

public static class AccountSearchExtension
{
    public static IQueryable<Account> FilterAccountsBy(this IQueryable<Account> query, AccountSearchFilterValues? values)
    {
        if (values is null)
            return query;
        
        if (values.FirstName is not null)
            query = query.Where(a => a.FirstName.Contains(values.FirstName, StringComparison.CurrentCultureIgnoreCase));
        if (values.LastName is not null)
            query = query.Where(a => a.LastName.Contains(values.LastName, StringComparison.CurrentCultureIgnoreCase));
        if (values.Email is not null)
            query = query.Where(a => a.Email.Contains(values.Email, StringComparison.CurrentCultureIgnoreCase));

        return query;
    }
}