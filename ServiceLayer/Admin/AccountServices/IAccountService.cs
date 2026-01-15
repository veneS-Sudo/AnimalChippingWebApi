using ServiceLayer.Admin.AccountServices.SearchPagination;

namespace ServiceLayer.Admin.AccountServices;

public interface IAccountService
{
    AccountDto GetAccountById(int id);
    IQueryable<AccountDto> GetAccounts(AccountSortFilterPageOptions filterOptions);
    bool UpdateAccountById(int accountId, UpdateAccountDto dto);
    bool DeleteAccountById(int accountId);
}