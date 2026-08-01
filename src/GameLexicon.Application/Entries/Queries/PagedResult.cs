namespace GameLexicon.Application.Entries.Queries;

public sealed class PagedResult<T>
{
    public PagedResult(
        IEnumerable<T> items,
        int pageNumber,
        int pageSize,
        long totalCount)
    {
        ArgumentNullException.ThrowIfNull(items);

        if (pageNumber < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pageNumber),
                "Page number must be at least one.");
        }

        if (pageSize < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pageSize),
                "Page size must be at least one.");
        }

        if (totalCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(totalCount),
                "Total count must not be negative.");
        }

        var itemCopy = items.ToArray();
        if (itemCopy.Length > pageSize)
        {
            throw new ArgumentException(
                "Item count must not exceed page size.",
                nameof(items));
        }

        Items = Array.AsReadOnly(itemCopy);
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalCount = totalCount;
        TotalPages = totalCount == 0
            ? 0
            : ((totalCount - 1) / pageSize) + 1;
    }

    public IReadOnlyList<T> Items { get; }
    public int PageNumber { get; }
    public int PageSize { get; }
    public long TotalCount { get; }
    public long TotalPages { get; }
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => TotalPages > 0 && PageNumber < TotalPages;
}
