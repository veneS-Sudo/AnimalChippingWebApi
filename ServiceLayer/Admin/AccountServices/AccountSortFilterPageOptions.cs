using ServiceLayer.Admin.Generic;

namespace ServiceLayer.Admin.AccountServices;

public class AccountSortFilterPageOptions : SortFilterPageBaseOptions
{
    public AccountSearchFilterValues filterValues { get; set; }
}

public class AccountSearchFilterValues
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
}