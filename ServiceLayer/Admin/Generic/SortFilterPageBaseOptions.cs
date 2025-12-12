namespace ServiceLayer.Admin.Generic;

public abstract class SortFilterPageBaseOptions
{
    
public const int DefaultPageSize = 10;
    private int _pageSize = DefaultPageSize;
    private int _pageNum = 1;
    public readonly int[] PageSizes = { 5, DefaultPageSize, 20, 50, 100, 500, 1000 };
    
    public int PageSize
    {
        get => _pageSize;
        set =>
            _pageSize = value > PageSizes.Max() 
                ? PageSizes.Max()
                : value < PageSizes.Min() 
                    ? PageSizes.Min() 
                    : value;
    }
    
    public int PageNum
    {
        get => _pageNum;
        set => _pageNum = value;
    }
    
    public int NumPages { get; private set; }

    public void SetupRest<T>(IQueryable<T> query)
    {
        NumPages = (int)Math.Ceiling((double)query.Count() / PageSize);
        PageNum = Math.Min(Math.Max(1, PageNum), NumPages);
    }}