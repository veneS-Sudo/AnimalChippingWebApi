using ServiceLayer.Admin.Generic;
using ServiceLayer.Admin.Generic.SortFilter;

namespace ServiceLayer.Admin.AccountServices.SearchPagination;

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