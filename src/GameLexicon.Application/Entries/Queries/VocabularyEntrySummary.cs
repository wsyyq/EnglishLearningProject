using GameLexicon.Domain.Entries;

namespace GameLexicon.Application.Entries.Queries;

public sealed class VocabularyEntrySummary
{
    public VocabularyEntrySummary(
        Guid id,
        string headword,
        EntryType entryType,
        string? translationChinese,
        string? primaryExampleText,
        string? primaryGameTitle,
        IEnumerable<TagSummary> tags,
        bool isArchived,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Identifier must not be empty.", nameof(id));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(headword);
        ValidateEntryType(entryType);
        ValidateTimestamps(createdAt, updatedAt);
        ArgumentNullException.ThrowIfNull(tags);

        var tagCopy = tags.ToArray();
        ValidateDistinctTagIds(tagCopy, nameof(tags));

        Id = id;
        Headword = headword;
        EntryType = entryType;
        TranslationChinese = translationChinese;
        PrimaryExampleText = primaryExampleText;
        PrimaryGameTitle = primaryGameTitle;
        Tags = Array.AsReadOnly(tagCopy);
        IsArchived = isArchived;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public Guid Id { get; }
    public string Headword { get; }
    public EntryType EntryType { get; }
    public string? TranslationChinese { get; }
    public string? PrimaryExampleText { get; }
    public string? PrimaryGameTitle { get; }
    public IReadOnlyList<TagSummary> Tags { get; }
    public bool IsArchived { get; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; }

    internal static void ValidateDistinctTagIds(
        IReadOnlyCollection<TagSummary> tags,
        string parameterName)
    {
        if (tags.Select(static tag => tag.Id).Distinct().Count() != tags.Count)
        {
            throw new ArgumentException(
                "Tag identifiers must be distinct.",
                parameterName);
        }
    }

    internal static void ValidateEntryType(EntryType entryType)
    {
        if (!Enum.IsDefined(entryType))
        {
            throw new ArgumentOutOfRangeException(
                nameof(entryType),
                "Entry type must be defined.");
        }
    }

    internal static void ValidateTimestamps(
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        if (createdAt.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Created timestamp must use UTC.", nameof(createdAt));
        }

        if (updatedAt.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Updated timestamp must use UTC.", nameof(updatedAt));
        }

        if (updatedAt < createdAt)
        {
            throw new ArgumentOutOfRangeException(
                nameof(updatedAt),
                "Updated timestamp must not precede creation.");
        }
    }
}
