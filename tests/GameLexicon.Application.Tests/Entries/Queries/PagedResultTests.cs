using GameLexicon.Application.Entries.Queries;

namespace GameLexicon.Application.Tests.Entries.Queries;

public sealed class PagedResultTests
{
    [Fact]
    public void Constructor_StoresValidFirstPage()
    {
        var result = new PagedResult<int>([1, 2], 1, 2, 3);

        Assert.Equal([1, 2], result.Items);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(3, result.TotalCount);
    }

    [Theory]
    [InlineData(0, 50, 0)]
    [InlineData(100, 50, 2)]
    [InlineData(101, 50, 3)]
    public void TotalPages_UsesSafeCeiling(long totalCount, int pageSize, long expected)
    {
        var result = new PagedResult<int>([], 1, pageSize, totalCount);

        Assert.Equal(expected, result.TotalPages);
    }

    [Fact]
    public void TotalPages_HandlesLongMaximumWithoutOverflow()
    {
        var result = new PagedResult<int>([], 1, 1, long.MaxValue);

        Assert.Equal(long.MaxValue, result.TotalPages);
    }

    [Theory]
    [InlineData(1, 3, false, true)]
    [InlineData(2, 3, true, true)]
    [InlineData(3, 3, true, false)]
    [InlineData(4, 3, true, false)]
    public void NavigationFlags_ReflectRequestedPage(
        int pageNumber,
        long totalPages,
        bool hasPrevious,
        bool hasNext)
    {
        var result = new PagedResult<int>(
            [],
            pageNumber,
            1,
            totalPages);

        Assert.Equal(hasPrevious, result.HasPreviousPage);
        Assert.Equal(hasNext, result.HasNextPage);
    }

    [Fact]
    public void Constructor_AllowsEmptyPagePastLastPage()
    {
        var result = new PagedResult<int>([], 4, 50, 101);

        Assert.Empty(result.Items);
        Assert.Equal(3, result.TotalPages);
        Assert.False(result.HasNextPage);
    }

    [Theory]
    [InlineData(0, 1, 0)]
    [InlineData(1, 0, 0)]
    [InlineData(1, 1, -1)]
    public void Constructor_RejectsInvalidPaging(
        int pageNumber,
        int pageSize,
        long totalCount)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new PagedResult<int>([], pageNumber, pageSize, totalCount));
    }

    [Fact]
    public void Constructor_RejectsNullItems()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new PagedResult<int>(null!, 1, 50, 0));
    }

    [Fact]
    public void Constructor_RejectsMoreItemsThanPageSize()
    {
        Assert.Throws<ArgumentException>(() =>
            new PagedResult<int>([1, 2], 1, 1, 2));
    }

    [Fact]
    public void Items_AreDefensivelyCopied()
    {
        var items = new List<int> { 1 };
        var result = new PagedResult<int>(items, 1, 50, 1);

        items[0] = 2;
        items.Add(3);

        Assert.Equal([1], result.Items);
    }
}
