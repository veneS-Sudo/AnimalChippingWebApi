namespace DatabaseLayer.QueryObjects;

public static class GenericPaging
{
    public static IQueryable<T> Page<T>(this IQueryable<T> query, int pageNumber, int skip, int pageSize)
    {
       if (pageSize <= 0)
           throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than zero.");
       
       if (pageNumber != 0)
           query = query.Skip(pageNumber * pageSize);
       if (skip % pageSize > 0)
           query = query.Skip(skip % pageSize);
       return query.Take(pageSize);
    }
}