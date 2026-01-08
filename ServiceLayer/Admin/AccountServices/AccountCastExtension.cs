using AnimalChippingWebApi.Core.Models;

namespace ServiceLayer.Admin.AccountServices;

internal static class AccountCastExtension
{
    public static AccountDto ToDto(this Account account)
    {
        var dto = new AccountDto()
        {
            AccountId = account.AccountId,
            FirstName = account.FirstName,
            LastName = account.LastName,
            Email = account.Email,
        };
        return dto;
    }
}