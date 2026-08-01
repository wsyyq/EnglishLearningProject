using GameLexicon.Application.Entries.Queries;
using GameLexicon.Domain.Entries;

namespace GameLexicon.Application.Tests.Entries.Queries;

public sealed class VocabularySearchQueryTests
{
    [Fact]
    public void Constructor_UsesExpectedDefaults()
    {
        var query = new VocabularySearchQuery();

        Assert.Null(query.SearchText);
        Assert.Null(query.GameTitle);
        Assert.Empty(query.TagIds);
        Assert.Null(query.EntryType);
        Assert.Equal(VocabularyArchiveFilter.ActiveOnly, query.ArchiveFilter);
        Assert.Equal(VocabularySortOrder.UpdatedAtDescending, query.SortOrder);
        Assert.Equal(1, query.PageNumber);
        Assert.Equal(50, query.PageSize);
    }

    [Fact]
    public void Constructor_PreservesCallerTextWithoutNormalization()
    {
        var query = Create(searchText: "  MiXeD  ", gameTitle: "  GAME  ");

        Assert.Equal("  MiXeD  ", query.SearchText);
        Assert.Equal("  GAME  ", query.GameTitle);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(200)]
    public void Constructor_AcceptsPageSizeBoundaries(int pageSize)
    {
        Assert.Equal(pageSize, Create(pageSize: pageSize).PageSize);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(201)]
    public void Constructor_RejectsPageSizeOutsideRange(int pageSize)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Create(pageSize: pageSize));
    }

    [Fact]
    public void Constructor_RejectsPageNumberBelowOne()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Create(pageNumber: 0));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsBlankSearchText(string value)
    {
        Assert.Throws<ArgumentException>(() => Create(searchText: value));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsBlankGameTitle(string value)
    {
        Assert.Throws<ArgumentException>(() => Create(gameTitle: value));
    }

    [Fact]
    public void Constructor_RejectsNullTagCollection()
    {
        Assert.Throws<ArgumentNullException>(() => new VocabularySearchQuery(
            null,
            null,
            null!,
            null,
            VocabularyArchiveFilter.ActiveOnly,
            VocabularySortOrder.UpdatedAtDescending,
            1,
            50));
    }

    [Fact]
    public void Constructor_RejectsEmptyTagId()
    {
        Assert.Throws<ArgumentException>(() => Create(tagIds: [Guid.Empty]));
    }

    [Fact]
    public void Constructor_RejectsDuplicateTagId()
    {
        var id = Guid.NewGuid();

        Assert.Throws<ArgumentException>(() => Create(tagIds: [id, id]));
    }

    [Fact]
    public void TagIds_AreDefensivelyCopied()
    {
        var original = Guid.NewGuid();
        var tagIds = new List<Guid> { original };
        var query = Create(tagIds: tagIds);

        tagIds[0] = Guid.NewGuid();
        tagIds.Add(Guid.NewGuid());

        Assert.Equal([original], query.TagIds);
    }

    [Fact]
    public void Constructor_RejectsUndefinedEntryType()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Create(entryType: (EntryType)999));
    }

    [Fact]
    public void Constructor_RejectsUndefinedArchiveFilter()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Create(archiveFilter: (VocabularyArchiveFilter)999));
    }

    [Fact]
    public void Constructor_RejectsUndefinedSortOrder()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Create(sortOrder: (VocabularySortOrder)999));
    }

    private static VocabularySearchQuery Create(
        string? searchText = null,
        string? gameTitle = null,
        IEnumerable<Guid>? tagIds = null,
        EntryType? entryType = null,
        VocabularyArchiveFilter archiveFilter = VocabularyArchiveFilter.ActiveOnly,
        VocabularySortOrder sortOrder = VocabularySortOrder.UpdatedAtDescending,
        int pageNumber = 1,
        int pageSize = 50) =>
        new(
            searchText,
            gameTitle,
            tagIds ?? Array.Empty<Guid>(),
            entryType,
            archiveFilter,
            sortOrder,
            pageNumber,
            pageSize);
}
