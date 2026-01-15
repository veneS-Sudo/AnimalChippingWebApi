using DatabaseLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace ServiceLayer.Admin.AccountServices.SearchPagination;

public static class AccountSearchExtension
{
    public static IQueryable<Account> FilterAccountsBy(this IQueryable<Account> query, AccountSearchFilterValues? values)
    {
        if (values is null)
            return query;
        
        if (values.FirstName is not null)
            query = query.Where(a => EF.Functions.Like(a.FirstName, $"%{values.FirstName}%"));
        if (values.LastName is not null)
            query = query.Where(a => EF.Functions.Like(a.LastName, $"%{values.LastName}%"));
        if (values.Email is not null)
            query = query.Where(a => EF.Functions.Like(a.Email, $"%{values.Email}%"));

        return query;
    }
}