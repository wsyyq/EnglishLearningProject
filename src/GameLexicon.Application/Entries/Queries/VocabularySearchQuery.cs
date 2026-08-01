using GameLexicon.Domain.Entries;

namespace GameLexicon.Application.Entries.Queries;

public sealed class VocabularySearchQuery
{
    public VocabularySearchQuery()
        : this(
            null,
            null,
            Array.Empty<Guid>(),
            null,
            VocabularyArchiveFilter.ActiveOnly,
            VocabularySortOrder.UpdatedAtDescending,
            1,
            50)
    {
    }

    public VocabularySearchQuery(
        string? searchText,
        string? gameTitle,
        IEnumerable<Guid> tagIds,
        EntryType? entryType,
        VocabularyArchiveFilter archiveFilter,
        VocabularySortOrder sortOrder,
        int pageNumber,
        int pageSize)
    {
        ValidateOptionalFilter(searchText, nameof(searchText));
        ValidateOptionalFilter(gameTitle, nameof(gameTitle));
        ArgumentNullException.ThrowIfNull(tagIds);

        if (entryType.HasValue && !Enum.IsDefined(entryType.Value))
        {
            throw new ArgumentOutOfRangeException(
                nameof(entryType),
                "Entry type must be defined.");
        }

        if (!Enum.IsDefined(archiveFilter))
        {
            throw new ArgumentOutOfRangeException(
                nameof(archiveFilter),
                "Archive filter must be defined.");
        }

        if (!Enum.IsDefined(sortOrder))
        {
            throw new ArgumentOutOfRangeException(
                nameof(sortOrder),
                "Sort order must be defined.");
        }

        if (pageNumber < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pageNumber),
                "Page number must be at least one.");
        }

        if (pageSize is < 1 or > 200)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pageSize),
                "Page size must be between one and 200.");
        }

        var tagIdCopy = tagIds.ToArray();
        if (tagIdCopy.Any(static id => id == Guid.Empty))
        {
            throw new ArgumentException(
                "Tag identifiers must not be empty.",
                nameof(tagIds));
        }

        if (tagIdCopy.Distinct().Count() != tagIdCopy.Length)
        {
            throw new ArgumentException(
                "Tag identifiers must be distinct.",
                nameof(tagIds));
        }

        SearchText = searchText;
        GameTitle = gameTitle;
        TagIds = Array.AsReadOnly(tagIdCopy);
        EntryType = entryType;
        ArchiveFilter = archiveFilter;
        SortOrder = sortOrder;
        PageNumber = pageNumber;
        PageSize = pageSize;
    }

    public string? SearchText { get; }
    public string? GameTitle { get; }
    public IReadOnlyList<Guid> TagIds { get; }
    public EntryType? EntryType { get; }
    public VocabularyArchiveFilter ArchiveFilter { get; }
    public VocabularySortOrder SortOrder { get; }
    public int PageNumber { get; }
    public int PageSize { get; }

    private static void ValidateOptionalFilter(string? value, string parameterName)
    {
        if (value is not null && string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "Filter text must not be empty or whitespace.",
                parameterName);
        }
    }
}
