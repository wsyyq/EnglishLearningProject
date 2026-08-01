namespace GameLexicon.Domain.Entries;

public sealed class VocabularyEntry
{
    public VocabularyEntry(
        Guid id,
        string headword,
        string normalizedHeadword,
        EntryType entryType,
        string? partOfSpeech,
        string? phonetic,
        string? definitionEnglish,
        string? translationChinese,
        string? notes,
        bool isArchived,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        EntryGuard.NotEmpty(id, nameof(id));
        EntryGuard.Required(headword, nameof(headword));
        EntryGuard.Required(normalizedHeadword, nameof(normalizedHeadword));
        ValidateEntryType(entryType, nameof(entryType));
        EntryGuard.Utc(createdAt, nameof(createdAt));
        EntryGuard.Utc(updatedAt, nameof(updatedAt));
        ValidateTimestampOrder(createdAt, updatedAt, nameof(updatedAt));

        Id = id;
        Headword = headword;
        NormalizedHeadword = normalizedHeadword;
        EntryType = entryType;
        PartOfSpeech = partOfSpeech;
        Phonetic = phonetic;
        DefinitionEnglish = definitionEnglish;
        TranslationChinese = translationChinese;
        Notes = notes;
        IsArchived = isArchived;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public Guid Id { get; }
    public string Headword { get; private set; }
    public string NormalizedHeadword { get; private set; }
    public EntryType EntryType { get; private set; }
    public string? PartOfSpeech { get; private set; }
    public string? Phonetic { get; private set; }
    public string? DefinitionEnglish { get; private set; }
    public string? TranslationChinese { get; private set; }
    public string? Notes { get; private set; }
    public bool IsArchived { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public void UpdateHeadword(
        string headword,
        string normalizedHeadword,
        DateTimeOffset updatedAt)
    {
        EntryGuard.Required(headword, nameof(headword));
        EntryGuard.Required(normalizedHeadword, nameof(normalizedHeadword));
        ValidateUpdateTime(updatedAt);

        Headword = headword;
        NormalizedHeadword = normalizedHeadword;
        UpdatedAt = updatedAt;
    }

    public void UpdateDetails(
        EntryType entryType,
        string? partOfSpeech,
        string? phonetic,
        string? definitionEnglish,
        string? translationChinese,
        string? notes,
        DateTimeOffset updatedAt)
    {
        ValidateEntryType(entryType, nameof(entryType));
        ValidateUpdateTime(updatedAt);

        EntryType = entryType;
        PartOfSpeech = partOfSpeech;
        Phonetic = phonetic;
        DefinitionEnglish = definitionEnglish;
        TranslationChinese = translationChinese;
        Notes = notes;
        UpdatedAt = updatedAt;
    }

    public void SetArchived(bool isArchived, DateTimeOffset updatedAt)
    {
        ValidateUpdateTime(updatedAt);

        IsArchived = isArchived;
        UpdatedAt = updatedAt;
    }

    private void ValidateUpdateTime(DateTimeOffset updatedAt)
    {
        EntryGuard.Utc(updatedAt, nameof(updatedAt));
        ValidateTimestampOrder(CreatedAt, updatedAt, nameof(updatedAt));
        ValidateTimestampOrder(UpdatedAt, updatedAt, nameof(updatedAt));
    }

    private static void ValidateEntryType(EntryType entryType, string parameterName)
    {
        if (!Enum.IsDefined(entryType))
        {
            throw new ArgumentOutOfRangeException(parameterName, "Entry type must be defined.");
        }
    }

    private static void ValidateTimestampOrder(
        DateTimeOffset minimum,
        DateTimeOffset value,
        string parameterName)
    {
        if (value < minimum)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Timestamp must not move backwards.");
        }
    }
}
