using ServiceLayer.Admin.Generic.SortFilter;

namespace ServiceLayer.Admin.VisitedLocationServices.SearchPagination;

public class LocationVisitedSortFilterOptions : SortFilterPageBaseOptions
{
    public string FilterValue { get; set; }
    public VisitedLocationFilterBy VisitedLocationFilterBy { get; set; }
}