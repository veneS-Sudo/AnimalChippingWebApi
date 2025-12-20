using AnimalChippingWebApi.Core.Models;
using ServiceLayer.Admin.Dto;

namespace ServiceLayer.Admin.AccountServices;

public interface IAccountService
{
    Account GetAccountById(int id);
    IQueryable<Account> GetAccounts(AccountSortFilterPageOptions filterOptions);
    bool UpdateAccountById(int accountId, UpdateAccountDto dto);
    bool DeleteAccountById(int accountId);
}