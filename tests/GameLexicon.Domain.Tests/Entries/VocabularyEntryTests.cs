using GameLexicon.Domain.Entries;

namespace GameLexicon.Domain.Tests.Entries;

public sealed class VocabularyEntryTests
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 8, 1, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_StoresAllFields()
    {
        var id = Guid.NewGuid();
        var updatedAt = CreatedAt.AddMinutes(1);

        var entry = CreateEntry(id: id, isArchived: true, updatedAt: updatedAt);

        Assert.Equal(id, entry.Id);
        Assert.Equal("Get Out", entry.Headword);
        Assert.Equal("get out", entry.NormalizedHeadword);
        Assert.Equal(EntryType.Phrase, entry.EntryType);
        Assert.Equal("verb", entry.PartOfSpeech);
        Assert.Equal("phonetic", entry.Phonetic);
        Assert.Equal("leave", entry.DefinitionEnglish);
        Assert.Equal("出去", entry.TranslationChinese);
        Assert.Equal("note", entry.Notes);
        Assert.True(entry.IsArchived);
        Assert.Equal(CreatedAt, entry.CreatedAt);
        Assert.Equal(updatedAt, entry.UpdatedAt);
    }

    [Fact]
    public void Constructor_AllowsNullOptionalFields()
    {
        var entry = new VocabularyEntry(
            Guid.NewGuid(),
            "word",
            "word",
            EntryType.Word,
            null,
            null,
            null,
            null,
            null,
            false,
            CreatedAt,
            CreatedAt);

        Assert.Null(entry.PartOfSpeech);
        Assert.Null(entry.Phonetic);
        Assert.Null(entry.DefinitionEnglish);
        Assert.Null(entry.TranslationChinese);
        Assert.Null(entry.Notes);
    }

    [Fact]
    public void Constructor_RejectsEmptyId()
    {
        Assert.Throws<ArgumentException>(() => CreateEntry(id: Guid.Empty));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsMissingHeadword(string? value)
    {
        Assert.ThrowsAny<ArgumentException>(() => CreateEntry(headword: value!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsMissingNormalizedHeadword(string? value)
    {
        Assert.ThrowsAny<ArgumentException>(() => CreateEntry(normalizedHeadword: value!));
    }

    [Fact]
    public void Constructor_RejectsUndefinedEntryType()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CreateEntry(entryType: (EntryType)999));
    }

    [Fact]
    public void Constructor_RejectsNonUtcCreatedAt()
    {
        Assert.Throws<ArgumentException>(() =>
            CreateEntry(createdAt: CreatedAt.ToOffset(TimeSpan.FromHours(8))));
    }

    [Fact]
    public void Constructor_RejectsNonUtcUpdatedAt()
    {
        Assert.Throws<ArgumentException>(() =>
            CreateEntry(updatedAt: CreatedAt.ToOffset(TimeSpan.FromHours(8))));
    }

    [Fact]
    public void Constructor_RejectsUpdatedAtBeforeCreatedAt()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CreateEntry(updatedAt: CreatedAt.AddTicks(-1)));
    }

    [Fact]
    public void UpdateHeadword_StoresProvidedValuesWithoutNormalizing()
    {
        var entry = CreateEntry();
        var updatedAt = CreatedAt.AddMinutes(1);

        entry.UpdateHeadword("  PROVIDED  ", "CUSTOM KEY", updatedAt);

        Assert.Equal("  PROVIDED  ", entry.Headword);
        Assert.Equal("CUSTOM KEY", entry.NormalizedHeadword);
        Assert.Equal(updatedAt, entry.UpdatedAt);
    }

    [Fact]
    public void UpdateHeadword_InvalidInputLeavesObjectUnchanged()
    {
        var entry = CreateEntry();

        Assert.Throws<ArgumentException>(() =>
            entry.UpdateHeadword("Changed", " ", CreatedAt.AddMinutes(1)));

        Assert.Equal("Get Out", entry.Headword);
        Assert.Equal("get out", entry.NormalizedHeadword);
        Assert.Equal(CreatedAt, entry.UpdatedAt);
    }

    [Fact]
    public void UpdateHeadword_BackwardsTimeLeavesObjectUnchanged()
    {
        var entry = CreateEntry(updatedAt: CreatedAt.AddMinutes(2));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            entry.UpdateHeadword("Changed", "changed", CreatedAt.AddMinutes(1)));

        Assert.Equal("Get Out", entry.Headword);
        Assert.Equal("get out", entry.NormalizedHeadword);
        Assert.Equal(CreatedAt.AddMinutes(2), entry.UpdatedAt);
    }

    [Fact]
    public void UpdateDetails_StoresOptionalFields()
    {
        var entry = CreateEntry();
        var updatedAt = CreatedAt.AddMinutes(1);

        entry.UpdateDetails(
            EntryType.Expression,
            null,
            "new phonetic",
            null,
            "新翻译",
            "new note",
            updatedAt);

        Assert.Equal(EntryType.Expression, entry.EntryType);
        Assert.Null(entry.PartOfSpeech);
        Assert.Equal("new phonetic", entry.Phonetic);
        Assert.Null(entry.DefinitionEnglish);
        Assert.Equal("新翻译", entry.TranslationChinese);
        Assert.Equal("new note", entry.Notes);
        Assert.Equal(updatedAt, entry.UpdatedAt);
    }

    [Fact]
    public void UpdateDetails_InvalidEntryTypeLeavesObjectUnchanged()
    {
        var entry = CreateEntry();

        Assert.Throws<ArgumentOutOfRangeException>(() => entry.UpdateDetails(
            (EntryType)999,
            "changed",
            null,
            null,
            null,
            null,
            CreatedAt.AddMinutes(1)));

        Assert.Equal(EntryType.Phrase, entry.EntryType);
        Assert.Equal("verb", entry.PartOfSpeech);
        Assert.Equal(CreatedAt, entry.UpdatedAt);
    }

    [Fact]
    public void SetArchived_ChangesStateAndPreservesIdentity()
    {
        var entry = CreateEntry();
        var id = entry.Id;

        entry.SetArchived(true, CreatedAt.AddMinutes(1));

        Assert.True(entry.IsArchived);
        Assert.Equal(id, entry.Id);
        Assert.Equal(CreatedAt, entry.CreatedAt);
    }

    [Fact]
    public void SetArchived_RejectsNonUtcTimeWithoutChangingState()
    {
        var entry = CreateEntry();

        Assert.Throws<ArgumentException>(() => entry.SetArchived(
            true,
            CreatedAt.ToOffset(TimeSpan.FromHours(1))));

        Assert.False(entry.IsArchived);
        Assert.Equal(CreatedAt, entry.UpdatedAt);
    }

    private static VocabularyEntry CreateEntry(
        Guid? id = null,
        string headword = "Get Out",
        string normalizedHeadword = "get out",
        EntryType entryType = EntryType.Phrase,
        bool isArchived = false,
        DateTimeOffset? createdAt = null,
        DateTimeOffset? updatedAt = null) =>
        new(
            id ?? Guid.NewGuid(),
            headword,
            normalizedHeadword,
            entryType,
            "verb",
            "phonetic",
            "leave",
            "出去",
            "note",
            isArchived,
            createdAt ?? CreatedAt,
            updatedAt ?? CreatedAt);
}
