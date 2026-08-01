using GameLexicon.Application.Entries.Queries;
using GameLexicon.Domain.Entries;

namespace GameLexicon.Application.Tests.Entries.Queries;

public sealed class EntryReadModelTests
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 8, 1, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void TagSummary_StoresCallerTextWithoutNormalizing()
    {
        var tag = new TagSummary(Guid.NewGuid(), "  Name  ", "CUSTOM KEY");

        Assert.Equal("  Name  ", tag.Name);
        Assert.Equal("CUSTOM KEY", tag.NormalizedName);
    }

    [Fact]
    public void TagSummary_RejectsEmptyId()
    {
        Assert.Throws<ArgumentException>(() => new TagSummary(Guid.Empty, "Name", "name"));
    }

    [Theory]
    [InlineData(null, "normalized")]
    [InlineData("", "normalized")]
    [InlineData(" ", "normalized")]
    [InlineData("name", null)]
    [InlineData("name", "")]
    [InlineData("name", " ")]
    public void TagSummary_RejectsMissingText(string? name, string? normalizedName)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new TagSummary(Guid.NewGuid(), name!, normalizedName!));
    }

    [Fact]
    public void SentenceDetails_CopiesDomainAndLinkScalars()
    {
        var entryId = Guid.NewGuid();
        var example = CreateExample(captureId: null);
        var link = new EntryExampleLink(entryId, example.Id, true, 2);

        var details = new SentenceExampleDetails(example, link);

        Assert.Equal(entryId, details.EntryId);
        Assert.Equal(example.Id, details.Id);
        Assert.Null(details.CaptureId);
        Assert.Equal("Get out", details.TargetText);
        Assert.True(details.IsPrimary);
        Assert.Equal(2, details.SortOrder);
    }

    [Fact]
    public void SentenceDetails_RejectsMismatchedExampleAndLink()
    {
        var example = CreateExample();
        var link = new EntryExampleLink(Guid.NewGuid(), Guid.NewGuid(), false, 0);

        Assert.Throws<ArgumentException>(() => new SentenceExampleDetails(example, link));
    }

    [Fact]
    public void Summary_DefensivelyCopiesTagsAndPreservesText()
    {
        var originalTag = new TagSummary(Guid.NewGuid(), "Tag", "tag");
        var tags = new List<TagSummary> { originalTag };

        var summary = CreateSummary(tags: tags, headword: "  Headword  ");
        tags.Clear();

        Assert.Equal("  Headword  ", summary.Headword);
        Assert.Equal([originalTag], summary.Tags);
    }

    [Fact]
    public void Summary_RejectsDuplicateTagIds()
    {
        var id = Guid.NewGuid();

        Assert.Throws<ArgumentException>(() => CreateSummary(tags:
        [
            new TagSummary(id, "One", "one"),
            new TagSummary(id, "Two", "two")
        ]));
    }

    [Fact]
    public void Summary_RejectsUndefinedEntryType()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CreateSummary(entryType: (EntryType)999));
    }

    [Fact]
    public void Summary_RejectsInvalidTimestampOrder()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateSummary(
            createdAt: CreatedAt,
            updatedAt: CreatedAt.AddTicks(-1)));
    }

    [Fact]
    public void Details_CopiesScalarStateAndCollections()
    {
        var entry = CreateEntry();
        var example = CreateDetails(entry.Id, sortOrder: 1, isPrimary: true);
        var tag = new TagSummary(Guid.NewGuid(), "Tag", "tag");
        var examples = new List<SentenceExampleDetails> { example };
        var tags = new List<TagSummary> { tag };

        var details = new VocabularyEntryDetails(entry, examples, tags);
        entry.UpdateHeadword("Changed", "changed", CreatedAt.AddMinutes(1));
        examples.Clear();
        tags.Clear();

        Assert.Equal("Get Out", details.Headword);
        Assert.Equal("get out", details.NormalizedHeadword);
        Assert.Equal([example], details.Examples);
        Assert.Equal([tag], details.Tags);
    }

    [Fact]
    public void Details_SortsExamplesAndTagsStably()
    {
        var entry = CreateEntry();
        var later = CreateDetails(entry.Id, sortOrder: 2);
        var earlier = CreateDetails(entry.Id, sortOrder: 1);
        var zTag = new TagSummary(Guid.NewGuid(), "Z", "z");
        var aTag = new TagSummary(Guid.NewGuid(), "A", "a");

        var details = new VocabularyEntryDetails(
            entry,
            [later, earlier],
            [zTag, aTag]);

        Assert.Equal([earlier.Id, later.Id], details.Examples.Select(static item => item.Id));
        Assert.Equal([aTag.Id, zTag.Id], details.Tags.Select(static item => item.Id));
    }

    [Fact]
    public void Details_AllowsNoPrimaryExample()
    {
        var entry = CreateEntry();

        var details = new VocabularyEntryDetails(
            entry,
            [CreateDetails(entry.Id, isPrimary: false)],
            []);

        Assert.DoesNotContain(details.Examples, static example => example.IsPrimary);
    }

    [Fact]
    public void Details_RejectsMultiplePrimaryExamples()
    {
        var entry = CreateEntry();

        Assert.Throws<ArgumentException>(() => new VocabularyEntryDetails(
            entry,
            [
                CreateDetails(entry.Id, isPrimary: true),
                CreateDetails(entry.Id, isPrimary: true)
            ],
            []));
    }

    [Fact]
    public void Details_RejectsDuplicateExampleIds()
    {
        var entry = CreateEntry();
        var example = CreateExample();

        Assert.Throws<ArgumentException>(() => new VocabularyEntryDetails(
            entry,
            [
                CreateDetails(entry.Id, example: example),
                CreateDetails(entry.Id, example: example)
            ],
            []));
    }

    [Fact]
    public void Details_RejectsExampleLinkedToDifferentEntry()
    {
        var entry = CreateEntry();

        Assert.Throws<ArgumentException>(() => new VocabularyEntryDetails(
            entry,
            [CreateDetails(Guid.NewGuid())],
            []));
    }

    [Fact]
    public void Details_RejectsDuplicateTagIds()
    {
        var entry = CreateEntry();
        var id = Guid.NewGuid();

        Assert.Throws<ArgumentException>(() => new VocabularyEntryDetails(
            entry,
            [],
            [
                new TagSummary(id, "One", "one"),
                new TagSummary(id, "Two", "two")
            ]));
    }

    private static VocabularyEntrySummary CreateSummary(
        string headword = "Get Out",
        EntryType entryType = EntryType.Phrase,
        IEnumerable<TagSummary>? tags = null,
        DateTimeOffset? createdAt = null,
        DateTimeOffset? updatedAt = null) =>
        new(
            Guid.NewGuid(),
            headword,
            entryType,
            "出去",
            "Get out now",
            "Game",
            tags ?? [],
            false,
            createdAt ?? CreatedAt,
            updatedAt ?? CreatedAt);

    private static VocabularyEntry CreateEntry() =>
        new(
            Guid.NewGuid(),
            "Get Out",
            "get out",
            EntryType.Phrase,
            null,
            null,
            null,
            "出去",
            null,
            false,
            CreatedAt,
            CreatedAt);

    private static SentenceExample CreateExample(Guid? captureId = null) =>
        new(
            Guid.NewGuid(),
            captureId,
            null,
            "Get out now",
            "get out now",
            0,
            7,
            null,
            "Game",
            CreatedAt);

    private static SentenceExampleDetails CreateDetails(
        Guid entryId,
        int sortOrder = 0,
        bool isPrimary = false,
        SentenceExample? example = null)
    {
        example ??= CreateExample();
        return new SentenceExampleDetails(
            example,
            new EntryExampleLink(entryId, example.Id, isPrimary, sortOrder));
    }
}
