using DatabaseLayer.Models;
using ServiceLayer.Admin.Generic;
using ServiceLayer.Admin.Generic.SortFilter;

namespace ServiceLayer.Admin.AnimalServices.SearchPagination;

public class AnimalSortFilterPageOptions : SortFilterPageBaseOptions
{
    public AnimalSearchFilterValues FilterValues { get; set; }
    
}

public class AnimalSearchFilterValues
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public long? ChipperId { get; set; }
    public long? ChippingLocationId { get; set; }
    public LiveStatusType? LiveStatus { get; set; }
    public GenderType? Gender { get; set; }
}