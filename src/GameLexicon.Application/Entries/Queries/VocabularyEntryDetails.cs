using GameLexicon.Domain.Entries;

namespace GameLexicon.Application.Entries.Queries;

public sealed class VocabularyEntryDetails
{
    public VocabularyEntryDetails(
        VocabularyEntry entry,
        IEnumerable<SentenceExampleDetails> examples,
        IEnumerable<TagSummary> tags)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(examples);
        ArgumentNullException.ThrowIfNull(tags);

        var exampleCopy = examples
            .OrderBy(static example => example.SortOrder)
            .ThenBy(static example => example.Id)
            .ToArray();
        var tagCopy = tags
            .OrderBy(static tag => tag.NormalizedName, StringComparer.Ordinal)
            .ThenBy(static tag => tag.Id)
            .ToArray();

        if (exampleCopy.Any(example => example.EntryId != entry.Id))
        {
            throw new ArgumentException(
                "Every example must belong to the entry.",
                nameof(examples));
        }

        if (exampleCopy.Select(static example => example.Id).Distinct().Count() !=
            exampleCopy.Length)
        {
            throw new ArgumentException(
                "Example identifiers must be distinct.",
                nameof(examples));
        }

        if (exampleCopy.Count(static example => example.IsPrimary) > 1)
        {
            throw new ArgumentException(
                "At most one example may be primary.",
                nameof(examples));
        }

        VocabularyEntrySummary.ValidateDistinctTagIds(tagCopy, nameof(tags));

        Id = entry.Id;
        Headword = entry.Headword;
        NormalizedHeadword = entry.NormalizedHeadword;
        EntryType = entry.EntryType;
        PartOfSpeech = entry.PartOfSpeech;
        Phonetic = entry.Phonetic;
        DefinitionEnglish = entry.DefinitionEnglish;
        TranslationChinese = entry.TranslationChinese;
        Notes = entry.Notes;
        IsArchived = entry.IsArchived;
        CreatedAt = entry.CreatedAt;
        UpdatedAt = entry.UpdatedAt;
        Examples = Array.AsReadOnly(exampleCopy);
        Tags = Array.AsReadOnly(tagCopy);
    }

    public Guid Id { get; }
    public string Headword { get; }
    public string NormalizedHeadword { get; }
    public EntryType EntryType { get; }
    public string? PartOfSpeech { get; }
    public string? Phonetic { get; }
    public string? DefinitionEnglish { get; }
    public string? TranslationChinese { get; }
    public string? Notes { get; }
    public bool IsArchived { get; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; }
    public IReadOnlyList<SentenceExampleDetails> Examples { get; }
    public IReadOnlyList<TagSummary> Tags { get; }
}
