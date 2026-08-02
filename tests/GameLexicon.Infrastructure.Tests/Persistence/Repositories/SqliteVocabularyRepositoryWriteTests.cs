using GameLexicon.Domain.Entries;
using GameLexicon.Infrastructure.Persistence;
using GameLexicon.Infrastructure.Persistence.Migrations;
using GameLexicon.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;

namespace GameLexicon.Infrastructure.Tests.Persistence.Repositories;

public sealed class SqliteVocabularyRepositoryWriteTests
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 8, 2, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset UpdatedAt = CreatedAt.AddMinutes(1);

    [Fact]
    public void TypeShapeAndConstructorMatchWriteSideContract()
    {
        Assert.Throws<ArgumentNullException>(() => new SqliteVocabularyRepository(null!));
        var type = typeof(SqliteVocabularyRepository);
        Assert.True(type.IsPublic && type.IsSealed);
        Assert.DoesNotContain(typeof(GameLexicon.Application.Abstractions.Persistence.IVocabularyRepository), type.GetInterfaces());
        var methods = type.GetMethods().Where(method => method.DeclaringType == type).ToArray();
        var save = Assert.Single(methods, method => method.Name == nameof(SqliteVocabularyRepository.SaveAsync));
        Assert.Equal(typeof(Task), save.ReturnType);
        Assert.Equal(
            [typeof(VocabularyEntry), typeof(CancellationToken)],
            save.GetParameters().Select(parameter => parameter.ParameterType));
        Assert.DoesNotContain(methods, method => method.Name is
            "FindByNormalizedHeadwordAsync" or "GetDetailsAsync" or "SearchAsync");
    }

    [Fact]
    public async Task InsertPersistsEveryFieldExactly()
    {
        using var database = await VocabularyDatabase.CreateAsync();
        var entry = Entry(
            Guid.NewGuid(),
            "  Quest's 世界\nLine  ",
            "  PROVIDED Quest  ",
            EntryType.Expression,
            "  Noun Phrase  ",
            "/kwest/",
            "  English definition  ",
            "中文释义",
            "  Notes\nLine  ",
            true,
            CreatedAt,
            UpdatedAt);

        await database.Repository.SaveAsync(entry, CancellationToken.None);

        var row = await database.ReadAsync(entry.Id);
        AssertRow(entry, row);
        Assert.Equal(1, await database.CountEntriesAsync());
    }

    [Fact]
    public async Task InsertPreservesNullsAsDatabaseNulls()
    {
        using var database = await VocabularyDatabase.CreateAsync();
        var entry = Entry(Guid.NewGuid(), "Safe", "safe", EntryType.Word, null, null, null, null, null, false, CreatedAt, CreatedAt);

        await database.Repository.SaveAsync(entry, CancellationToken.None);

        var row = await database.ReadAsync(entry.Id);
        Assert.Null(row.PartOfSpeech);
        Assert.Null(row.Phonetic);
        Assert.Null(row.DefinitionEnglish);
        Assert.Null(row.TranslationChinese);
        Assert.Null(row.Notes);
    }

    [Theory]
    [InlineData(EntryType.Word, 0)]
    [InlineData(EntryType.Phrase, 1)]
    [InlineData(EntryType.Expression, 2)]
    [InlineData(EntryType.SentencePattern, 3)]
    public async Task InsertMapsEveryEntryTypeExplicitly(EntryType entryType, int storedValue)
    {
        using var database = await VocabularyDatabase.CreateAsync();
        var entry = Entry(Guid.NewGuid(), entryType.ToString(), Guid.NewGuid().ToString("N"), entryType, null, null, null, null, null, false, CreatedAt, CreatedAt);

        await database.Repository.SaveAsync(entry, CancellationToken.None);

        Assert.Equal(storedValue, (await database.ReadAsync(entry.Id)).EntryType);
    }

    [Fact]
    public async Task SaveRejectsNullAndPreCancellationWithoutWriting()
    {
        using var database = await VocabularyDatabase.CreateAsync();
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            database.Repository.SaveAsync(null!, CancellationToken.None));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() => database.Repository.SaveAsync(
            Entry(Guid.NewGuid(), "Cancelled", "cancelled", EntryType.Word, null, null, null, null, null, false, CreatedAt, CreatedAt),
            cancellation.Token));
        Assert.Equal(0, await database.CountEntriesAsync());
    }

    [Fact]
    public async Task UpdateChangesAllMutableFieldsButPreservesIdentityAndCreatedAt()
    {
        using var database = await VocabularyDatabase.CreateAsync();
        var id = Guid.NewGuid();
        var original = Entry(id, "Original", "original", EntryType.Word, "noun", "old", "old en", "旧", "old notes", false, CreatedAt, CreatedAt);
        await database.Repository.SaveAsync(original, CancellationToken.None);
        var updated = Entry(id, "Updated", "updated", EntryType.SentencePattern, null, null, null, null, null, true, CreatedAt, UpdatedAt);

        await database.Repository.SaveAsync(updated, CancellationToken.None);
        await database.Repository.SaveAsync(updated, CancellationToken.None);

        var row = await database.ReadAsync(id);
        AssertRow(updated, row);
        Assert.Equal(FormatUtc(CreatedAt), row.CreatedAtUtc);
        Assert.Equal(1, await database.CountEntriesAsync());
    }

    [Fact]
    public async Task UpdateChangesNullableFieldsFromNullToValues()
    {
        using var database = await VocabularyDatabase.CreateAsync();
        var id = Guid.NewGuid();
        await database.Repository.SaveAsync(
            Entry(id, "Original", "original", EntryType.Word, null, null, null, null, null, false, CreatedAt, CreatedAt),
            CancellationToken.None);
        var updated = Entry(id, "Updated", "updated", EntryType.Phrase, "verb", "fəʊ", "definition", "翻译", "note", false, CreatedAt, UpdatedAt);

        await database.Repository.SaveAsync(updated, CancellationToken.None);

        AssertRow(updated, await database.ReadAsync(id));
    }

    [Fact]
    public async Task CreatedAtMismatchAndStaleUpdatedAtRejectWithoutChanges()
    {
        using var database = await VocabularyDatabase.CreateAsync();
        var id = Guid.NewGuid();
        var stored = Entry(id, "Stored", "stored", EntryType.Word, "noun", null, null, null, "safe", false, CreatedAt, UpdatedAt);
        await database.Repository.SaveAsync(stored, CancellationToken.None);

        var mismatched = Entry(id, "Changed", "changed", EntryType.Phrase, null, null, null, null, null, true, CreatedAt.AddSeconds(1), UpdatedAt.AddSeconds(1));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            database.Repository.SaveAsync(mismatched, CancellationToken.None));
        var stale = Entry(id, "Stale", "stale", EntryType.Expression, null, null, null, null, null, true, CreatedAt, CreatedAt);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            database.Repository.SaveAsync(stale, CancellationToken.None));

        AssertRow(stored, await database.ReadAsync(id));
    }

    [Theory]
    [InlineData("created_at_utc", "invalid")]
    [InlineData("updated_at_utc", "2026-08-02T08:01:00.0000000+08:00")]
    [InlineData("updated_at_utc", "2026-08-01T23:59:00.0000000Z")]
    public async Task CorruptStoredTimesFailWithoutRepair(string column, string value)
    {
        using var database = await VocabularyDatabase.CreateAsync();
        var entry = Entry(Guid.NewGuid(), "Stored", "stored", EntryType.Word, null, null, null, null, null, false, CreatedAt, UpdatedAt);
        await database.Repository.SaveAsync(entry, CancellationToken.None);
        await database.ExecuteAsync(
            $"UPDATE vocabulary_entries SET {column}=$value WHERE id=$id;",
            ("$value", value),
            ("$id", FormatGuid(entry.Id)));

        await Assert.ThrowsAsync<InvalidDataException>(() =>
            database.Repository.SaveAsync(
                Entry(entry.Id, "Changed", "changed", EntryType.Phrase, null, null, null, null, null, false, CreatedAt, UpdatedAt.AddMinutes(1)),
                CancellationToken.None));
        Assert.Equal("Stored", (await database.ReadAsync(entry.Id)).Headword);
    }

    [Fact]
    public async Task UpdateArchiveAndRestorePreserveExampleAndTagAssociations()
    {
        using var database = await VocabularyDatabase.CreateAsync();
        var id = Guid.NewGuid();
        var original = Entry(id, "Quest", "quest", EntryType.Word, null, null, null, null, null, false, CreatedAt, CreatedAt);
        await database.Repository.SaveAsync(original, CancellationToken.None);
        var association = await database.AddAssociationsAsync(id, isPrimary: true, sortOrder: 7);

        var updated = Entry(id, "Quest Updated", "quest updated", EntryType.Phrase, "verb", null, null, null, "note", false, CreatedAt, UpdatedAt);
        await database.Repository.SaveAsync(updated, CancellationToken.None);
        await database.AssertAssociationsAsync(id, association, true, 7);
        var archived = Entry(id, "Quest Updated", "quest updated", EntryType.Phrase, "verb", null, null, null, "note", true, CreatedAt, UpdatedAt.AddMinutes(1));
        await database.Repository.SaveAsync(archived, CancellationToken.None);
        await database.AssertAssociationsAsync(id, association, true, 7);
        var restored = Entry(id, "Quest Updated", "quest updated", EntryType.Phrase, "verb", null, null, null, "note", false, CreatedAt, UpdatedAt.AddMinutes(2));
        await database.Repository.SaveAsync(restored, CancellationToken.None);
        await database.AssertAssociationsAsync(id, association, true, 7);
    }

    [Fact]
    public async Task ActiveUniqueIndexAllowsArchivedDuplicatesAndArchiveReleasesName()
    {
        using var database = await VocabularyDatabase.CreateAsync();
        var active = Entry(Guid.NewGuid(), "Active", "quest", EntryType.Word, null, null, null, null, null, false, CreatedAt, CreatedAt);
        await database.Repository.SaveAsync(active, CancellationToken.None);
        await Assert.ThrowsAsync<SqliteException>(() => database.Repository.SaveAsync(
            Entry(Guid.NewGuid(), "Conflict", "quest", EntryType.Phrase, null, null, null, null, null, false, CreatedAt, CreatedAt),
            CancellationToken.None));
        await database.Repository.SaveAsync(
            Entry(Guid.NewGuid(), "Archived One", "quest", EntryType.Word, null, null, null, null, null, true, CreatedAt, CreatedAt),
            CancellationToken.None);
        await database.Repository.SaveAsync(
            Entry(Guid.NewGuid(), "Archived Two", "quest", EntryType.Word, null, null, null, null, null, true, CreatedAt, CreatedAt),
            CancellationToken.None);

        await database.Repository.SaveAsync(
            Entry(active.Id, "Active", "quest", EntryType.Word, null, null, null, null, null, true, CreatedAt, UpdatedAt),
            CancellationToken.None);
        await database.Repository.SaveAsync(
            Entry(Guid.NewGuid(), "Replacement", "quest", EntryType.Expression, null, null, null, null, null, false, CreatedAt, CreatedAt),
            CancellationToken.None);
        Assert.Equal(4, await database.CountEntriesAsync());
    }

    [Fact]
    public async Task RestoreConflictRollsBackEveryFieldAndAssociations()
    {
        using var database = await VocabularyDatabase.CreateAsync();
        var active = Entry(Guid.NewGuid(), "Active", "quest", EntryType.Word, null, null, null, null, null, false, CreatedAt, CreatedAt);
        var archived = Entry(Guid.NewGuid(), "Archived", "quest", EntryType.Phrase, "noun", null, null, null, "original", true, CreatedAt, CreatedAt);
        await database.Repository.SaveAsync(active, CancellationToken.None);
        await database.Repository.SaveAsync(archived, CancellationToken.None);
        var association = await database.AddAssociationsAsync(archived.Id, false, 4);
        var restore = Entry(archived.Id, "Changed", "quest", EntryType.Expression, null, null, null, null, "changed", false, CreatedAt, UpdatedAt);

        await Assert.ThrowsAsync<SqliteException>(() =>
            database.Repository.SaveAsync(restore, CancellationToken.None));

        AssertRow(archived, await database.ReadAsync(archived.Id));
        AssertRow(active, await database.ReadAsync(active.Id));
        await database.AssertAssociationsAsync(archived.Id, association, false, 4);
    }

    [Fact]
    public async Task UpdateTriggerFailureRollsBackEveryFieldAndAssociations()
    {
        using var database = await VocabularyDatabase.CreateAsync();
        var entry = Entry(Guid.NewGuid(), "Original", "original", EntryType.Word, null, null, null, null, "safe", false, CreatedAt, CreatedAt);
        await database.Repository.SaveAsync(entry, CancellationToken.None);
        var association = await database.AddAssociationsAsync(entry.Id, true, 3);
        await database.ExecuteAsync("""
            CREATE TRIGGER fail_m1_t08_vocabulary_update
            BEFORE UPDATE ON vocabulary_entries
            BEGIN
                SELECT RAISE(ABORT, 'm1-t08-test');
            END;
            """);

        await Assert.ThrowsAsync<SqliteException>(() => database.Repository.SaveAsync(
            Entry(entry.Id, "Changed", "changed", EntryType.Phrase, null, null, null, null, "changed", true, CreatedAt, UpdatedAt),
            CancellationToken.None));

        AssertRow(entry, await database.ReadAsync(entry.Id));
        await database.AssertAssociationsAsync(entry.Id, association, true, 3);
    }

    [Fact]
    public async Task ConcurrentActiveDuplicateSavesHaveExactlyOneWinner()
    {
        using var database = await VocabularyDatabase.CreateAsync();
        var firstRepository = new SqliteVocabularyRepository(database.Factory);
        var secondRepository = new SqliteVocabularyRepository(database.Factory);
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task SaveAfterGateAsync(SqliteVocabularyRepository repository, VocabularyEntry entry)
        {
            await gate.Task;
            await repository.SaveAsync(entry, CancellationToken.None);
        }

        var first = SaveAfterGateAsync(firstRepository, Entry(Guid.NewGuid(), "First", "shared", EntryType.Word, null, null, null, null, null, false, CreatedAt, CreatedAt));
        var second = SaveAfterGateAsync(secondRepository, Entry(Guid.NewGuid(), "Second", "shared", EntryType.Word, null, null, null, null, null, false, CreatedAt, CreatedAt));
        gate.SetResult();
        try
        {
            await Task.WhenAll(first, second);
        }
        catch (Exception)
        {
            // One write must fail at SQLite's single-writer/unique-constraint boundary.
        }

        Assert.Equal(1, new[] { first, second }.Count(task => task.IsCompletedSuccessfully));
        Assert.Equal(1, new[] { first, second }.Count(task => task.IsFaulted));
        var failedTask = Assert.Single(new[] { first, second }, task => task.IsFaulted);
        Assert.IsType<SqliteException>(failedTask.Exception!.GetBaseException());
        Assert.Equal(1, await database.CountActiveAsync("shared"));
    }

    [Fact]
    public async Task ConnectionsAndSidecarsAreReleasedForDeletion()
    {
        string directoryPath;
        using (var database = await VocabularyDatabase.CreateAsync())
        {
            directoryPath = database.DirectoryPath;
            await database.Repository.SaveAsync(
                Entry(Guid.NewGuid(), "Safe", "safe", EntryType.Word, null, null, null, null, null, false, CreatedAt, CreatedAt),
                CancellationToken.None);
            database.DeleteFiles();
        }

        Assert.False(Directory.Exists(directoryPath));
    }

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
        bool isArchived,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt) =>
        new(id, headword, normalizedHeadword, entryType, partOfSpeech, phonetic,
            definitionEnglish, translationChinese, notes, isArchived, createdAt, updatedAt);

    private static string FormatGuid(Guid value) => value.ToString("D").ToLowerInvariant();
    private static string FormatUtc(DateTimeOffset value) => value.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'");

    private static void AssertRow(VocabularyEntry expected, EntryRow actual)
    {
        Assert.Equal(FormatGuid(expected.Id), actual.Id);
        Assert.Equal(expected.Headword, actual.Headword);
        Assert.Equal(expected.NormalizedHeadword, actual.NormalizedHeadword);
        Assert.Equal((int)expected.EntryType, actual.EntryType);
        Assert.Equal(expected.PartOfSpeech, actual.PartOfSpeech);
        Assert.Equal(expected.Phonetic, actual.Phonetic);
        Assert.Equal(expected.DefinitionEnglish, actual.DefinitionEnglish);
        Assert.Equal(expected.TranslationChinese, actual.TranslationChinese);
        Assert.Equal(expected.Notes, actual.Notes);
        Assert.Equal(expected.IsArchived ? 1 : 0, actual.IsArchived);
        Assert.Equal(FormatUtc(expected.CreatedAt), actual.CreatedAtUtc);
        Assert.Equal(FormatUtc(expected.UpdatedAt), actual.UpdatedAtUtc);
    }

    private sealed class VocabularyDatabase : IDisposable
    {
        private readonly TestDirectory _directory;

        private VocabularyDatabase(TestDirectory directory, SqliteConnectionFactory factory)
        {
            _directory = directory;
            Factory = factory;
            Repository = new SqliteVocabularyRepository(factory);
        }

        public SqliteConnectionFactory Factory { get; }
        public SqliteVocabularyRepository Repository { get; }
        public string DirectoryPath => _directory.Path;

        public static async Task<VocabularyDatabase> CreateAsync()
        {
            var directory = new TestDirectory();
            var factory = SqliteConnectionFactoryTests.CreateFactory(Path.Combine(directory.Path, "gamelexicon.db"));
            await new MigrationRunner(factory, [new Migration001_Initial(), new Migration002_ManualExamplesAndSearchSupport()]).RunAsync();
            return new VocabularyDatabase(directory, factory);
        }

        public async Task<EntryRow> ReadAsync(Guid id)
        {
            await using var connection = await Factory.OpenConnectionAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT id, headword, normalized_headword, entry_type, part_of_speech,
                       phonetic, definition_english, translation_chinese, notes,
                       is_archived, created_at_utc, updated_at_utc
                FROM vocabulary_entries
                WHERE id=$id;
                """;
            command.Parameters.AddWithValue("$id", FormatGuid(id));
            await using var reader = await command.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());
            return new EntryRow(
                reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetInt32(3),
                NullableText(reader, 4), NullableText(reader, 5), NullableText(reader, 6),
                NullableText(reader, 7), NullableText(reader, 8), reader.GetInt32(9),
                reader.GetString(10), reader.GetString(11));
        }

        public async Task<(Guid ExampleId, Guid TagId)> AddAssociationsAsync(Guid entryId, bool isPrimary, int sortOrder)
        {
            var exampleId = Guid.NewGuid();
            var tagId = Guid.NewGuid();
            await ExecuteAsync("""
                INSERT INTO sentence_examples
                    (id, capture_id, ocr_region_id, sentence_text, normalized_sentence,
                     target_start, target_length, screenshot_crop_path, game_title, created_at_utc)
                VALUES ($exampleId, NULL, NULL, 'Safe sentence.', 'safe sentence', 0, 4, '', NULL, $createdAt);
                INSERT INTO entry_examples(entry_id, example_id, is_primary, sort_order)
                VALUES ($entryId, $exampleId, $isPrimary, $sortOrder);
                INSERT INTO tags(id, name, normalized_name)
                VALUES ($tagId, 'Safe Tag', $normalizedTag);
                INSERT INTO entry_tags(entry_id, tag_id)
                VALUES ($entryId, $tagId);
                """,
                ("$exampleId", FormatGuid(exampleId)),
                ("$entryId", FormatGuid(entryId)),
                ("$isPrimary", isPrimary ? 1 : 0),
                ("$sortOrder", sortOrder),
                ("$tagId", FormatGuid(tagId)),
                ("$normalizedTag", tagId.ToString("N")),
                ("$createdAt", FormatUtc(CreatedAt)));
            return (exampleId, tagId);
        }

        public async Task AssertAssociationsAsync(
            Guid entryId,
            (Guid ExampleId, Guid TagId) expected,
            bool isPrimary,
            int sortOrder)
        {
            await using var connection = await Factory.OpenConnectionAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT ee.is_primary, ee.sort_order,
                       EXISTS(SELECT 1 FROM sentence_examples WHERE id=$exampleId),
                       EXISTS(SELECT 1 FROM entry_tags WHERE entry_id=$entryId AND tag_id=$tagId),
                       EXISTS(SELECT 1 FROM tags WHERE id=$tagId)
                FROM entry_examples AS ee
                WHERE ee.entry_id=$entryId AND ee.example_id=$exampleId;
                """;
            command.Parameters.AddWithValue("$entryId", FormatGuid(entryId));
            command.Parameters.AddWithValue("$exampleId", FormatGuid(expected.ExampleId));
            command.Parameters.AddWithValue("$tagId", FormatGuid(expected.TagId));
            await using var reader = await command.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());
            Assert.Equal(isPrimary ? 1 : 0, reader.GetInt32(0));
            Assert.Equal(sortOrder, reader.GetInt32(1));
            Assert.Equal(1, reader.GetInt32(2));
            Assert.Equal(1, reader.GetInt32(3));
            Assert.Equal(1, reader.GetInt32(4));
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

        public Task<int> CountEntriesAsync() => CountAsync("SELECT COUNT(*) FROM vocabulary_entries;");

        public async Task<int> CountActiveAsync(string normalizedHeadword)
        {
            await using var connection = await Factory.OpenConnectionAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM vocabulary_entries WHERE normalized_headword=$value AND is_archived=0;";
            command.Parameters.AddWithValue("$value", normalizedHeadword);
            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }

        private async Task<int> CountAsync(string sql)
        {
            await using var connection = await Factory.OpenConnectionAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }

        public void DeleteFiles()
        {
            foreach (var suffix in new[] { "-wal", "-shm", "" })
            {
                var path = Factory.DatabasePath + suffix;
                if (File.Exists(path)) File.Delete(path);
            }
        }

        public void Dispose() => _directory.Dispose();
        private static string? NullableText(SqliteDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private sealed record EntryRow(
        string Id,
        string Headword,
        string NormalizedHeadword,
        int EntryType,
        string? PartOfSpeech,
        string? Phonetic,
        string? DefinitionEnglish,
        string? TranslationChinese,
        string? Notes,
        int IsArchived,
        string CreatedAtUtc,
        string UpdatedAtUtc);
}
