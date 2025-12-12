using ServiceLayer.Admin.Generic;

namespace ServiceLayer.Admin;

public class LocationVisitedSortFilterOptions : SortFilterPageBaseOptions
{
    public string FilterValue { get; set; }
    public VisitedLocationFilterBy VisitedLocationFilterBy { get; set; }
}