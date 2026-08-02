using GameLexicon.Application.Abstractions.Persistence;
using GameLexicon.Application.Entries.Queries;
using GameLexicon.Domain.Entries;
using GameLexicon.Infrastructure.Persistence;
using GameLexicon.Infrastructure.Persistence.Migrations;
using GameLexicon.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;

namespace GameLexicon.Infrastructure.Tests.Persistence.Repositories;

public sealed class SqliteVocabularyRepositoryQueryTests
{
    private static readonly DateTimeOffset BaseTime =
        new(2026, 8, 2, 1, 0, 0, TimeSpan.Zero);

    [Fact]
    public void RepositoryImplementsCompleteInterfaceWithoutQueryStubs()
    {
        Assert.True(typeof(IVocabularyRepository).IsAssignableFrom(typeof(SqliteVocabularyRepository)));
        var methods = typeof(SqliteVocabularyRepository).GetMethods()
            .Where(method => method.DeclaringType == typeof(SqliteVocabularyRepository))
            .ToArray();
        Assert.Single(methods, method => method.Name == nameof(IVocabularyRepository.SaveAsync));
        Assert.Single(methods, method => method.Name == nameof(IVocabularyRepository.FindByNormalizedHeadwordAsync));
        Assert.Single(methods, method => method.Name == nameof(IVocabularyRepository.GetDetailsAsync));
        Assert.Single(methods, method => method.Name == nameof(IVocabularyRepository.SearchAsync));
        Assert.DoesNotContain(
            methods,
            method => method.GetMethodBody()?.GetILAsByteArray() is null);
    }

    [Fact]
    public async Task FindReturnsCompleteActiveEntryWithExactCallerValue()
    {
        using var database = await QueryDatabase.CreateAsync();
        var expected = Entry(
            Guid.NewGuid(), "Get Out", "get out", EntryType.Expression,
            "verb", "/ɡet/", "leave", "离开", "note", false,
            BaseTime, BaseTime.AddMinutes(1));
        await database.SaveAsync(expected);
        await database.SaveAsync(Entry(
            Guid.NewGuid(), "Archived", "get out", EntryType.Phrase,
            null, null, null, null, null, true, BaseTime, BaseTime));

        var actual = await database.Repository.FindByNormalizedHeadwordAsync(
            "get out", CancellationToken.None);

        Assert.NotNull(actual);
        AssertEntry(expected, actual);
        Assert.Null(await database.Repository.FindByNormalizedHeadwordAsync("Get Out", CancellationToken.None));
        Assert.Null(await database.Repository.FindByNormalizedHeadwordAsync(" get out ", CancellationToken.None));
        Assert.Null(await database.Repository.FindByNormalizedHeadwordAsync("missing", CancellationToken.None));
    }

    [Fact]
    public async Task FindRejectsInvalidInputAndPreCancellation()
    {
        using var database = await QueryDatabase.CreateAsync();
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            database.Repository.FindByNormalizedHeadwordAsync(null!, CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            database.Repository.FindByNormalizedHeadwordAsync(string.Empty, CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            database.Repository.FindByNormalizedHeadwordAsync(" \t", CancellationToken.None));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            database.Repository.FindByNormalizedHeadwordAsync("safe", cancellation.Token));
    }

    [Theory]
    [InlineData("id", "invalid-guid")]
    [InlineData("entry_type", 9)]
    [InlineData("created_at_utc", "invalid-time")]
    [InlineData("updated_at_utc", "2026-08-02T00:00:00.0000000+08:00")]
    public async Task FindRejectsCorruptEntryData(string column, object value)
    {
        using var database = await QueryDatabase.CreateAsync();
        var entry = Entry(Guid.NewGuid(), "Safe", "safe", EntryType.Word, null, null, null, null, null, false, BaseTime, BaseTime);
        await database.SaveAsync(entry);
        await database.ExecuteAsync(
            $"UPDATE vocabulary_entries SET {column}=$value WHERE normalized_headword='safe';",
            ("$value", value));

        await Assert.ThrowsAsync<InvalidDataException>(() =>
            database.Repository.FindByNormalizedHeadwordAsync("safe", CancellationToken.None));
    }

    [Fact]
    public async Task SearchAllRejectsCorruptArchiveState()
    {
        using var database = await QueryDatabase.CreateAsync();
        var entry = Entry(Guid.NewGuid(), "Safe", "safe", EntryType.Word, null, null, null, null, null, false, BaseTime, BaseTime);
        await database.SaveAsync(entry);
        await database.ExecuteAsync(
            "UPDATE vocabulary_entries SET is_archived=2 WHERE id=$id;",
            ("$id", FormatGuid(entry.Id)));

        await Assert.ThrowsAsync<InvalidDataException>(() =>
            database.Repository.SearchAsync(
                Query(archiveFilter: VocabularyArchiveFilter.All),
                CancellationToken.None));
    }

    [Fact]
    public async Task GetDetailsReturnsArchivedEntryAllExamplesAndSortedTags()
    {
        using var database = await QueryDatabase.CreateAsync();
        var entry = Entry(
            Guid.NewGuid(), "Quest", "quest", EntryType.Word, "noun", "kwest",
            "mission", "任务", "keep", true, BaseTime, BaseTime.AddMinutes(2));
        await database.SaveAsync(entry);
        var highId = Guid.Parse("ffffffff-ffff-ffff-ffff-fffffffffff0");
        var lowId = Guid.Parse("00000000-0000-0000-0000-000000000010");
        await database.AddExampleAsync(entry.Id, highId, "Second target", "second target", 7, 6, null, null, false, 2);
        await database.AddExampleAsync(entry.Id, lowId, "First target", "first target", 6, 6, null, "Halo", true, 1);
        var zTag = await database.AddTagAsync(entry.Id, "Zed", "zed");
        var aTag = await database.AddTagAsync(entry.Id, "Alpha Original", "alpha");

        var details = await database.Repository.GetDetailsAsync(entry.Id, CancellationToken.None);

        Assert.NotNull(details);
        Assert.Equal(entry.Id, details.Id);
        Assert.True(details.IsArchived);
        Assert.Equal([lowId, highId], details.Examples.Select(example => example.Id));
        Assert.Equal("target", details.Examples[0].TargetText);
        Assert.True(details.Examples[0].IsPrimary);
        Assert.Null(details.Examples[0].CaptureId);
        Assert.Null(details.Examples[0].OcrRegionId);
        Assert.Equal([aTag, zTag], details.Tags.Select(tag => tag.Id));
        Assert.Equal("Alpha Original", details.Tags[0].Name);
    }

    [Fact]
    public async Task GetDetailsMapsCaptureAndOcrExampleIdentifiers()
    {
        using var database = await QueryDatabase.CreateAsync();
        var entry = Entry(Guid.NewGuid(), "Captured", "captured", EntryType.Word, null, null, null, null, null, false, BaseTime, BaseTime);
        await database.SaveAsync(entry);
        var (exampleId, captureId, ocrRegionId) = await database.AddOcrExampleAsync(entry.Id);

        var details = await database.Repository.GetDetailsAsync(entry.Id, CancellationToken.None);

        var example = Assert.Single(Assert.IsType<VocabularyEntryDetails>(details).Examples);
        Assert.Equal(exampleId, example.Id);
        Assert.Equal(captureId, example.CaptureId);
        Assert.Equal(ocrRegionId, example.OcrRegionId);
        Assert.Equal("target", example.TargetText);
    }

    [Fact]
    public async Task GetDetailsHandlesNotFoundEmptyAggregatesAndInvalidIdentifier()
    {
        using var database = await QueryDatabase.CreateAsync();
        var entry = Entry(Guid.NewGuid(), "Empty", "empty", EntryType.Word, null, null, null, null, null, false, BaseTime, BaseTime);
        await database.SaveAsync(entry);

        var details = await database.Repository.GetDetailsAsync(entry.Id, CancellationToken.None);
        Assert.NotNull(details);
        Assert.Empty(details.Examples);
        Assert.Empty(details.Tags);
        Assert.Null(await database.Repository.GetDetailsAsync(Guid.NewGuid(), CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            database.Repository.GetDetailsAsync(Guid.Empty, CancellationToken.None));
    }

    [Fact]
    public async Task GetDetailsRejectsMultiplePrimaryAndCorruptLink()
    {
        using var database = await QueryDatabase.CreateAsync();
        var entry = Entry(Guid.NewGuid(), "Corrupt", "corrupt", EntryType.Word, null, null, null, null, null, false, BaseTime, BaseTime);
        await database.SaveAsync(entry);
        await database.AddExampleAsync(entry.Id, Guid.NewGuid(), "First target", "first target", 6, 6, null, null, true, 0);
        await database.AddExampleAsync(entry.Id, Guid.NewGuid(), "Other target", "other target", 6, 6, null, null, true, 1);

        await Assert.ThrowsAsync<InvalidDataException>(() =>
            database.Repository.GetDetailsAsync(entry.Id, CancellationToken.None));

        await database.ExecuteAsync(
            "UPDATE entry_examples SET is_primary=2 WHERE entry_id=$entryId;",
            ("$entryId", FormatGuid(entry.Id)));
        await Assert.ThrowsAsync<InvalidDataException>(() =>
            database.Repository.GetDetailsAsync(entry.Id, CancellationToken.None));
    }

    [Fact]
    public async Task DefaultSearchIsActiveOnlyAndPaginatesWithStableUpdatedOrdering()
    {
        using var database = await QueryDatabase.CreateAsync();
        var firstId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var secondId = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var oldId = Guid.NewGuid();
        await database.SaveAsync(Entry(oldId, "Old", "old", EntryType.Word, null, null, null, null, null, false, BaseTime, BaseTime));
        await database.SaveAsync(Entry(secondId, "Second", "second", EntryType.Word, null, null, null, null, null, false, BaseTime, BaseTime.AddMinutes(1)));
        await database.SaveAsync(Entry(firstId, "First", "first", EntryType.Word, null, null, null, null, null, false, BaseTime, BaseTime.AddMinutes(1)));
        await database.SaveAsync(Entry(Guid.NewGuid(), "Archived", "archived", EntryType.Word, null, null, null, null, null, true, BaseTime, BaseTime.AddHours(1)));

        var page1 = await database.Repository.SearchAsync(Query(pageNumber: 1, pageSize: 2), CancellationToken.None);
        var page2 = await database.Repository.SearchAsync(Query(pageNumber: 2, pageSize: 2), CancellationToken.None);
        var beyond = await database.Repository.SearchAsync(Query(pageNumber: 9, pageSize: 2), CancellationToken.None);

        Assert.Equal(3, page1.TotalCount);
        Assert.Equal(2, page1.TotalPages);
        Assert.False(page1.HasPreviousPage);
        Assert.True(page1.HasNextPage);
        Assert.Equal([firstId, secondId], page1.Items.Select(item => item.Id));
        Assert.Equal([oldId], page2.Items.Select(item => item.Id));
        Assert.True(page2.HasPreviousPage);
        Assert.False(page2.HasNextPage);
        Assert.Empty(beyond.Items);
        Assert.Equal(3, beyond.TotalCount);
    }

    [Theory]
    [InlineData("headword", "Quest Marker")]
    [InlineData("normalized_headword", "quest marker")]
    [InlineData("part_of_speech", "quest noun")]
    [InlineData("phonetic", "quest sound")]
    [InlineData("definition_english", "quest definition")]
    [InlineData("translation_chinese", "quest 翻译")]
    [InlineData("notes", "quest notes")]
    public async Task SearchTextSearchesEachSpecifiedEntryField(string column, string value)
    {
        using var database = await QueryDatabase.CreateAsync();
        var id = Guid.NewGuid();
        await database.SaveAsync(Entry(id, "Base", "base", EntryType.Word, "noun", "sound", "definition", "翻译", "notes", false, BaseTime, BaseTime));
        await database.ExecuteAsync(
            $"UPDATE vocabulary_entries SET {column}=$value WHERE id=$id;",
            ("$value", value), ("$id", FormatGuid(id)));

        var result = await database.Repository.SearchAsync(Query(searchText: "QUEST"), CancellationToken.None);
        Assert.Equal([id], result.Items.Select(item => item.Id));
    }

    [Theory]
    [InlineData("%", "has % sign", "plain")]
    [InlineData("_", "has_under", "plain")]
    [InlineData("\\", "has\\slash", "plain")]
    [InlineData("' OR 1=1 --", "literal ' OR 1=1 -- text", "plain")]
    public async Task SearchTextTreatsWildcardsBackslashAndInjectionStyleTextLiterally(
        string searchText,
        string matching,
        string nonMatching)
    {
        using var database = await QueryDatabase.CreateAsync();
        var matchId = Guid.NewGuid();
        await database.SaveAsync(Entry(matchId, matching, Guid.NewGuid().ToString("N"), EntryType.Word, null, null, null, null, null, false, BaseTime, BaseTime));
        await database.SaveAsync(Entry(Guid.NewGuid(), nonMatching, Guid.NewGuid().ToString("N"), EntryType.Word, null, null, null, null, null, false, BaseTime, BaseTime));

        var result = await database.Repository.SearchAsync(Query(searchText: searchText), CancellationToken.None);
        Assert.Equal([matchId], result.Items.Select(item => item.Id));
    }

    [Fact]
    public async Task SearchTextDoesNotTrimNormalizeOrSearchRelatedTables()
    {
        using var database = await QueryDatabase.CreateAsync();
        var entry = Entry(Guid.NewGuid(), "Quest", "quest", EntryType.Word, null, null, null, null, null, false, BaseTime, BaseTime);
        await database.SaveAsync(entry);
        await database.AddExampleAsync(entry.Id, Guid.NewGuid(), "Hidden Needle", "hidden needle", 0, 6, null, "Hidden Game", false, 0);
        await database.AddTagAsync(entry.Id, "Hidden Tag", "hidden tag");

        Assert.Empty((await database.Repository.SearchAsync(Query(searchText: " quest "), CancellationToken.None)).Items);
        Assert.Empty((await database.Repository.SearchAsync(Query(searchText: "Ｑｕｅｓｔ"), CancellationToken.None)).Items);
        Assert.Empty((await database.Repository.SearchAsync(Query(searchText: "Needle"), CancellationToken.None)).Items);
        Assert.Empty((await database.Repository.SearchAsync(Query(searchText: "Hidden Game"), CancellationToken.None)).Items);
        Assert.Empty((await database.Repository.SearchAsync(Query(searchText: "Hidden Tag"), CancellationToken.None)).Items);
    }

    [Fact]
    public async Task GameTitleIsExactNoCaseExistsFilterWithoutDuplicates()
    {
        using var database = await QueryDatabase.CreateAsync();
        var halo = Entry(Guid.NewGuid(), "Halo Entry", "halo entry", EntryType.Word, null, null, null, null, null, false, BaseTime, BaseTime);
        var longer = Entry(Guid.NewGuid(), "Longer", "longer", EntryType.Word, null, null, null, null, null, false, BaseTime, BaseTime);
        await database.SaveAsync(halo);
        await database.SaveAsync(longer);
        await database.AddExampleAsync(halo.Id, Guid.NewGuid(), "Safe target", "safe target", 5, 6, null, "halo", false, 0);
        await database.AddExampleAsync(halo.Id, Guid.NewGuid(), "Other target", "other target", 6, 6, null, "HALO", false, 1);
        await database.AddExampleAsync(longer.Id, Guid.NewGuid(), "Long target", "long target", 5, 6, null, "Halo Infinite", false, 0);

        Assert.Equal([halo.Id], (await database.Repository.SearchAsync(Query(gameTitle: "Halo"), CancellationToken.None)).Items.Select(item => item.Id));
        Assert.Empty((await database.Repository.SearchAsync(Query(gameTitle: " Halo "), CancellationToken.None)).Items);
        Assert.Empty((await database.Repository.SearchAsync(Query(gameTitle: "Halo Inf"), CancellationToken.None)).Items);
    }

    [Fact]
    public async Task TagFilterUsesAllSemanticsAndSummaryTagsAreCompleteAndSorted()
    {
        using var database = await QueryDatabase.CreateAsync();
        var both = Entry(Guid.NewGuid(), "Both", "both", EntryType.Word, null, null, null, null, null, false, BaseTime, BaseTime);
        var partial = Entry(Guid.NewGuid(), "Partial", "partial", EntryType.Word, null, null, null, null, null, false, BaseTime, BaseTime);
        await database.SaveAsync(both);
        await database.SaveAsync(partial);
        var tagA = await database.CreateTagAsync("Zed", "zed");
        var tagB = await database.CreateTagAsync("Alpha", "alpha");
        var extra = await database.CreateTagAsync("Middle", "middle");
        await database.LinkTagAsync(both.Id, tagA);
        await database.LinkTagAsync(both.Id, tagB);
        await database.LinkTagAsync(both.Id, extra);
        await database.LinkTagAsync(partial.Id, tagA);

        var result = await database.Repository.SearchAsync(Query(tagIds: [tagA, tagB]), CancellationToken.None);
        var summary = Assert.Single(result.Items);
        Assert.Equal(both.Id, summary.Id);
        Assert.Equal([tagB, extra, tagA], summary.Tags.Select(tag => tag.Id));
        Assert.Empty((await database.Repository.SearchAsync(Query(tagIds: [Guid.NewGuid()]), CancellationToken.None)).Items);
    }

    [Fact]
    public async Task ArchiveEntryTypeAndAllOtherFiltersCombineWithAnd()
    {
        using var database = await QueryDatabase.CreateAsync();
        var match = Entry(Guid.NewGuid(), "Quest Match", "quest match", EntryType.Phrase, null, null, null, null, null, true, BaseTime, BaseTime);
        var wrongType = Entry(Guid.NewGuid(), "Quest Wrong", "quest wrong", EntryType.Word, null, null, null, null, null, true, BaseTime, BaseTime);
        await database.SaveAsync(match);
        await database.SaveAsync(wrongType);
        var tag = await database.CreateTagAsync("Story", "story");
        await database.LinkTagAsync(match.Id, tag);
        await database.LinkTagAsync(wrongType.Id, tag);
        await database.AddExampleAsync(match.Id, Guid.NewGuid(), "Safe target", "safe target", 5, 6, null, "Halo", false, 0);
        await database.AddExampleAsync(wrongType.Id, Guid.NewGuid(), "Safe target", "safe target", 5, 6, null, "Halo", false, 0);

        var query = Query(
            searchText: "Quest",
            gameTitle: "halo",
            tagIds: [tag],
            entryType: EntryType.Phrase,
            archiveFilter: VocabularyArchiveFilter.ArchivedOnly);
        Assert.Equal([match.Id], (await database.Repository.SearchAsync(query, CancellationToken.None)).Items.Select(item => item.Id));
        Assert.Equal(2, (await database.Repository.SearchAsync(Query(archiveFilter: VocabularyArchiveFilter.ArchivedOnly), CancellationToken.None)).TotalCount);
        Assert.Equal(2, (await database.Repository.SearchAsync(Query(archiveFilter: VocabularyArchiveFilter.All), CancellationToken.None)).TotalCount);
    }

    [Theory]
    [InlineData(VocabularySortOrder.HeadwordAscending)]
    [InlineData(VocabularySortOrder.CreatedAtDescending)]
    public async Task AlternateSortOrdersUseIdAsStableTieBreaker(VocabularySortOrder sortOrder)
    {
        using var database = await QueryDatabase.CreateAsync();
        var firstId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var secondId = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var distinctId = Guid.Parse("00000000-0000-0000-0000-000000000003");
        await database.SaveAsync(Entry(secondId, "SAME", "same-two", EntryType.Word, null, null, null, null, null, false, BaseTime, BaseTime));
        await database.SaveAsync(Entry(firstId, "same", "same-one", EntryType.Word, null, null, null, null, null, false, BaseTime, BaseTime));
        await database.SaveAsync(Entry(
            distinctId,
            sortOrder == VocabularySortOrder.HeadwordAscending ? "Zed" : "Distinct",
            "distinct",
            EntryType.Word,
            null, null, null, null, null, false,
            BaseTime.AddMinutes(1),
            BaseTime.AddMinutes(1)));

        var result = await database.Repository.SearchAsync(Query(sortOrder: sortOrder), CancellationToken.None);
        Guid[] expected = sortOrder == VocabularySortOrder.HeadwordAscending
            ? [firstId, secondId, distinctId]
            : [distinctId, firstId, secondId];
        Assert.Equal(expected, result.Items.Select(item => item.Id));
    }

    [Fact]
    public async Task SummaryMapsPrimaryAndRejectsMultiplePrimary()
    {
        using var database = await QueryDatabase.CreateAsync();
        var noPrimary = Entry(Guid.NewGuid(), "None", "none", EntryType.Word, null, null, null, null, null, false, BaseTime, BaseTime);
        var primary = Entry(Guid.NewGuid(), "Primary", "primary", EntryType.Word, null, null, null, null, null, false, BaseTime, BaseTime);
        await database.SaveAsync(noPrimary);
        await database.SaveAsync(primary);
        await database.AddExampleAsync(noPrimary.Id, Guid.NewGuid(), "No primary", "no primary", 3, 7, null, "None", false, 0);
        await database.AddExampleAsync(primary.Id, Guid.NewGuid(), "The target text", "the target text", 4, 6, null, "Halo", true, 0);

        var result = await database.Repository.SearchAsync(Query(), CancellationToken.None);
        var noneSummary = Assert.Single(result.Items, item => item.Id == noPrimary.Id);
        Assert.Null(noneSummary.PrimaryExampleText);
        Assert.Null(noneSummary.PrimaryGameTitle);
        var primarySummary = Assert.Single(result.Items, item => item.Id == primary.Id);
        Assert.Equal("The target text", primarySummary.PrimaryExampleText);
        Assert.Equal("Halo", primarySummary.PrimaryGameTitle);

        await database.AddExampleAsync(primary.Id, Guid.NewGuid(), "Other target", "other target", 6, 6, null, null, true, 1);
        await Assert.ThrowsAsync<InvalidDataException>(() =>
            database.Repository.SearchAsync(Query(), CancellationToken.None));
    }

    [Fact]
    public async Task SearchRejectsNullCancellationAndUsesLongOffset()
    {
        using var database = await QueryDatabase.CreateAsync();
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            database.Repository.SearchAsync(null!, CancellationToken.None));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            database.Repository.SearchAsync(Query(), cancellation.Token));

        var largePage = await database.Repository.SearchAsync(
            Query(pageNumber: int.MaxValue, pageSize: 200),
            CancellationToken.None);
        Assert.Empty(largePage.Items);
        Assert.Equal(0, largePage.TotalCount);
    }

    [Fact]
    public async Task RequiredIndexesRemainPresentAndFilesCanBeDeleted()
    {
        string directoryPath;
        using (var database = await QueryDatabase.CreateAsync())
        {
            directoryPath = database.DirectoryPath;
            var indexes = await database.ReadIndexNamesAsync();
            Assert.Contains("ux_vocabulary_entries_normalized_active", indexes);
            Assert.Contains("ix_vocabulary_entries_archive_updated", indexes);
            Assert.Contains("ix_vocabulary_entries_archive_type_updated", indexes);
            Assert.Contains("ix_entry_examples_entry_sort", indexes);
            Assert.Contains("ix_entry_examples_example_entry", indexes);
            Assert.Contains("ix_entry_tags_tag_entry", indexes);
            Assert.Contains("ix_sentence_examples_game_created", indexes);
            await database.Repository.SearchAsync(Query(), CancellationToken.None);
            database.DeleteFiles();
        }

        Assert.False(Directory.Exists(directoryPath));
    }

    private static VocabularySearchQuery Query(
        string? searchText = null,
        string? gameTitle = null,
        IEnumerable<Guid>? tagIds = null,
        EntryType? entryType = null,
        VocabularyArchiveFilter archiveFilter = VocabularyArchiveFilter.ActiveOnly,
        VocabularySortOrder sortOrder = VocabularySortOrder.UpdatedAtDescending,
        int pageNumber = 1,
        int pageSize = 50) =>
        new(searchText, gameTitle, tagIds ?? [], entryType, archiveFilter, sortOrder, pageNumber, pageSize);

    private static VocabularyEntry Entry(
        Guid id,
        string headword,
        string normalizedHeadword,
        EntryType entryType,
        string? partOfSpeech,
        string? phonetic,
        string? definitionEnglish,
        string? translationChinese,
        string? notes,
        bool archived,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt) =>
        new(id, headword, normalizedHeadword, entryType, partOfSpeech, phonetic,
            definitionEnglish, translationChinese, notes, archived, createdAt, updatedAt);

    private static void AssertEntry(VocabularyEntry expected, VocabularyEntry actual)
    {
        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Headword, actual.Headword);
        Assert.Equal(expected.NormalizedHeadword, actual.NormalizedHeadword);
        Assert.Equal(expected.EntryType, actual.EntryType);
        Assert.Equal(expected.PartOfSpeech, actual.PartOfSpeech);
        Assert.Equal(expected.Phonetic, actual.Phonetic);
        Assert.Equal(expected.DefinitionEnglish, actual.DefinitionEnglish);
        Assert.Equal(expected.TranslationChinese, actual.TranslationChinese);
        Assert.Equal(expected.Notes, actual.Notes);
        Assert.Equal(expected.IsArchived, actual.IsArchived);
        Assert.Equal(expected.CreatedAt, actual.CreatedAt);
        Assert.Equal(expected.UpdatedAt, actual.UpdatedAt);
    }

    private static string FormatGuid(Guid value) => value.ToString("D").ToLowerInvariant();
    private static string FormatUtc(DateTimeOffset value) => value.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'");

    private sealed class QueryDatabase : IDisposable
    {
        private readonly TestDirectory _directory;

        private QueryDatabase(TestDirectory directory, SqliteConnectionFactory factory)
        {
            _directory = directory;
            Factory = factory;
            Repository = new SqliteVocabularyRepository(factory);
        }

        public SqliteConnectionFactory Factory { get; }
        public SqliteVocabularyRepository Repository { get; }
        public string DirectoryPath => _directory.Path;

        public static async Task<QueryDatabase> CreateAsync()
        {
            var directory = new TestDirectory();
            var factory = SqliteConnectionFactoryTests.CreateFactory(
                Path.Combine(directory.Path, "gamelexicon.db"));
            await new MigrationRunner(
                factory,
                [new Migration001_Initial(), new Migration002_ManualExamplesAndSearchSupport()])
                .RunAsync();
            return new QueryDatabase(directory, factory);
        }

        public Task SaveAsync(VocabularyEntry entry) =>
            Repository.SaveAsync(entry, CancellationToken.None);

        public async Task<Guid> AddTagAsync(Guid entryId, string name, string normalizedName)
        {
            var tagId = await CreateTagAsync(name, normalizedName);
            await LinkTagAsync(entryId, tagId);
            return tagId;
        }

        public async Task<Guid> CreateTagAsync(string name, string normalizedName)
        {
            var tagId = Guid.NewGuid();
            await ExecuteAsync(
                "INSERT INTO tags(id,name,normalized_name) VALUES($id,$name,$normalized);",
                ("$id", FormatGuid(tagId)), ("$name", name), ("$normalized", normalizedName));
            return tagId;
        }

        public Task LinkTagAsync(Guid entryId, Guid tagId) => ExecuteAsync(
            "INSERT INTO entry_tags(entry_id,tag_id) VALUES($entryId,$tagId);",
            ("$entryId", FormatGuid(entryId)), ("$tagId", FormatGuid(tagId)));

        public Task AddExampleAsync(
            Guid entryId,
            Guid exampleId,
            string sentenceText,
            string normalizedSentence,
            int targetStart,
            int targetLength,
            Guid? captureId,
            string? gameTitle,
            bool primary,
            int sortOrder) => ExecuteAsync(
            """
            INSERT INTO sentence_examples
                (id,capture_id,ocr_region_id,sentence_text,normalized_sentence,target_start,
                 target_length,screenshot_crop_path,game_title,created_at_utc)
            VALUES($exampleId,$captureId,NULL,$sentence,$normalized,$start,$length,'',$game,$created);
            INSERT INTO entry_examples(entry_id,example_id,is_primary,sort_order)
            VALUES($entryId,$exampleId,$primary,$sortOrder);
            """,
            ("$exampleId", FormatGuid(exampleId)),
            ("$captureId", captureId.HasValue ? FormatGuid(captureId.Value) : DBNull.Value),
            ("$sentence", sentenceText),
            ("$normalized", normalizedSentence),
            ("$start", targetStart),
            ("$length", targetLength),
            ("$game", gameTitle ?? (object)DBNull.Value),
            ("$created", FormatUtc(BaseTime)),
            ("$entryId", FormatGuid(entryId)),
            ("$primary", primary ? 1 : 0),
            ("$sortOrder", sortOrder));

        public async Task<(Guid ExampleId, Guid CaptureId, Guid OcrRegionId)> AddOcrExampleAsync(Guid entryId)
        {
            var captureId = Guid.NewGuid();
            var ocrRegionId = Guid.NewGuid();
            var exampleId = Guid.NewGuid();
            await ExecuteAsync(
                """
                INSERT INTO captures
                    (id,captured_at_utc,source_window_title,source_process_name,game_title,
                     image_path,pixel_width,pixel_height,status,error_message)
                VALUES($captureId,$created,'','',NULL,'capture.png',100,50,1,NULL);
                INSERT INTO ocr_regions
                    (id,capture_id,x,y,width,height,raw_text,corrected_text,created_at_utc)
                VALUES($ocrId,$captureId,0,0,100,50,'','',$created);
                INSERT INTO sentence_examples
                    (id,capture_id,ocr_region_id,sentence_text,normalized_sentence,target_start,
                     target_length,screenshot_crop_path,game_title,created_at_utc)
                VALUES($exampleId,$captureId,$ocrId,'OCR target text','ocr target text',4,6,
                       'crop.png','Halo',$created);
                INSERT INTO entry_examples(entry_id,example_id,is_primary,sort_order)
                VALUES($entryId,$exampleId,1,0);
                """,
                ("$captureId", FormatGuid(captureId)),
                ("$ocrId", FormatGuid(ocrRegionId)),
                ("$exampleId", FormatGuid(exampleId)),
                ("$entryId", FormatGuid(entryId)),
                ("$created", FormatUtc(BaseTime)));
            return (exampleId, captureId, ocrRegionId);
        }

        public async Task ExecuteAsync(string sql, params (string Name, object Value)[] values)
        {
            await using var connection = await Factory.OpenConnectionAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            foreach (var (name, value) in values)
            {
                command.Parameters.AddWithValue(name, value);
            }

            await command.ExecuteNonQueryAsync();
        }

        public async Task<IReadOnlySet<string>> ReadIndexNamesAsync()
        {
            await using var connection = await Factory.OpenConnectionAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='index';";
            await using var reader = await command.ExecuteReaderAsync();
            var names = new HashSet<string>(StringComparer.Ordinal);
            while (await reader.ReadAsync())
            {
                names.Add(reader.GetString(0));
            }

            return names;
        }

        public void DeleteFiles()
        {
            foreach (var suffix in new[] { "-wal", "-shm", string.Empty })
            {
                var path = Factory.DatabasePath + suffix;
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        public void Dispose() => _directory.Dispose();
    }
}
