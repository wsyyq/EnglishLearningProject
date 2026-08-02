using GameLexicon.Domain.Entries;
using GameLexicon.Infrastructure.Persistence;
using GameLexicon.Infrastructure.Persistence.Migrations;
using GameLexicon.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;

namespace GameLexicon.Infrastructure.Tests.Persistence.Repositories;

public sealed class SqliteSentenceExampleRepositoryTests
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 8, 2, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ConstructorRejectsNullFactory()
    {
        Assert.Throws<ArgumentNullException>(() => new SqliteSentenceExampleRepository(null!));
    }

    [Fact]
    public async Task ManualCaptureAndOcrExamplesRoundTripWithoutNormalization()
    {
        using var database = await RepositoryDatabase.CreateAsync();
        var manual = Example(Guid.NewGuid(), null, null, "Manual text.", "provided normalized", 0, 6, null, null);
        var capture = Example(Guid.NewGuid(), database.CaptureId, null, "Capture text.", "capture provided", 8, 4, "", "Game");
        var ocr = Example(Guid.NewGuid(), database.CaptureId, database.OcrRegionId, "😀 OCR text.", "ocr provided", 3, 3, "crop.png", "Game");

        await database.Repository.SaveAsync(manual, CancellationToken.None);
        await database.Repository.SaveAsync(capture, CancellationToken.None);
        await database.Repository.SaveAsync(ocr, CancellationToken.None);

        AssertExample(manual, await database.Repository.GetByIdAsync(manual.Id, CancellationToken.None));
        AssertExample(capture, await database.Repository.GetByIdAsync(capture.Id, CancellationToken.None));
        AssertExample(ocr, await database.Repository.GetByIdAsync(ocr.Id, CancellationToken.None));
        Assert.Equal("OCR", (await database.Repository.GetByIdAsync(ocr.Id, CancellationToken.None))!.TargetText);
        Assert.Equal("", (await database.Repository.GetByIdAsync(manual.Id, CancellationToken.None))!.ScreenshotCropPath);
    }

    [Fact]
    public async Task GetByIdReturnsNullAndRejectsEmptyOrCancellation()
    {
        using var database = await RepositoryDatabase.CreateAsync();
        Assert.Null(await database.Repository.GetByIdAsync(Guid.NewGuid(), CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            database.Repository.GetByIdAsync(Guid.Empty, CancellationToken.None));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            database.Repository.GetByIdAsync(Guid.NewGuid(), cancellation.Token));
    }

    [Fact]
    public async Task EmptyIdentifiersAndPreCancelledOperationsNeverWrite()
    {
        using var database = await RepositoryDatabase.CreateAsync();
        var id = Guid.NewGuid();
        var example = Example(id, null, null, "Safe text.", "safe", 0, 4, "", null);
        await Assert.ThrowsAsync<ArgumentException>(() =>
            database.Repository.GetForEntryAsync(Guid.Empty, CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            database.Repository.SetPrimaryAsync(Guid.Empty, id, CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            database.Repository.SetPrimaryAsync(database.EntryId, Guid.Empty, CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            database.Repository.RemoveLinkAsync(Guid.Empty, id, CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            database.Repository.RemoveLinkAsync(database.EntryId, Guid.Empty, CancellationToken.None));
        Assert.Empty(await database.Repository.GetForEntryAsync(Guid.NewGuid(), CancellationToken.None));

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            database.Repository.SaveAsync(example, cancellation.Token));
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            database.Repository.SaveLinkAsync(
                new EntryExampleLink(database.EntryId, id, false, 0),
                cancellation.Token));
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            database.Repository.SetPrimaryAsync(database.EntryId, id, cancellation.Token));
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            database.Repository.RemoveLinkAsync(database.EntryId, id, cancellation.Token));
        Assert.Null(await database.Repository.GetByIdAsync(id, CancellationToken.None));
    }

    [Fact]
    public async Task SaveUpdatesInPlaceWithoutDeletingLinks()
    {
        using var database = await RepositoryDatabase.CreateAsync();
        var id = Guid.NewGuid();
        var original = Example(id, null, null, "Original text.", "original", 0, 8, "", null);
        await database.Repository.SaveAsync(original, CancellationToken.None);
        await database.Repository.SaveLinkAsync(
            new EntryExampleLink(database.EntryId, id, true, 4),
            CancellationToken.None);

        var updated = Example(id, null, null, "Updated text.", "already normalized", 8, 4, "updated.png", "New Game");
        await database.Repository.SaveAsync(updated, CancellationToken.None);

        AssertExample(updated, await database.Repository.GetByIdAsync(id, CancellationToken.None));
        var details = await database.Repository.GetForEntryAsync(database.EntryId, CancellationToken.None);
        Assert.Single(details);
        Assert.True(details[0].IsPrimary);
        Assert.Equal(4, details[0].SortOrder);
    }

    [Fact]
    public async Task SaveForeignKeyFailureRollsBackExistingRow()
    {
        using var database = await RepositoryDatabase.CreateAsync();
        var id = Guid.NewGuid();
        var original = Example(id, null, null, "Original text.", "original", 0, 8, "", null);
        await database.Repository.SaveAsync(original, CancellationToken.None);
        var invalidCapture = Example(id, Guid.NewGuid(), null, "Changed text.", "changed", 0, 7, "", null);

        await Assert.ThrowsAsync<SqliteException>(() =>
            database.Repository.SaveAsync(invalidCapture, CancellationToken.None));

        AssertExample(original, await database.Repository.GetByIdAsync(id, CancellationToken.None));
    }

    [Fact]
    public async Task SaveRejectsNullAndMissingOcrForeignKey()
    {
        using var database = await RepositoryDatabase.CreateAsync();
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            database.Repository.SaveAsync(null!, CancellationToken.None));
        var invalid = Example(
            Guid.NewGuid(),
            database.CaptureId,
            Guid.NewGuid(),
            "Safe text.",
            "safe text",
            0,
            4,
            "",
            null);
        await Assert.ThrowsAsync<SqliteException>(() =>
            database.Repository.SaveAsync(invalid, CancellationToken.None));
        Assert.Null(await database.Repository.GetByIdAsync(invalid.Id, CancellationToken.None));
    }

    [Theory]
    [InlineData("capture_id", "not-a-guid")]
    [InlineData("created_at_utc", "2026-08-02T08:00:00.0000000+08:00")]
    [InlineData("target_length", "999")]
    public async Task CorruptExampleDataFailsInsteadOfBeingRepaired(string column, string value)
    {
        using var database = await RepositoryDatabase.CreateAsync();
        var example = Example(Guid.NewGuid(), null, null, "Safe text.", "safe text", 0, 4, "", null);
        await database.Repository.SaveAsync(example, CancellationToken.None);
        await database.ExecuteParameterizedAsync(
            $"PRAGMA foreign_keys=OFF; UPDATE sentence_examples SET {column}=$value WHERE id=$id;",
            ("$value", value),
            ("$id", example.Id.ToString("D")));

        await Assert.ThrowsAsync<InvalidDataException>(() =>
            database.Repository.GetByIdAsync(example.Id, CancellationToken.None));
    }

    [Fact]
    public async Task SaveLinkUpsertsAndDoesNotClearAnotherPrimary()
    {
        using var database = await RepositoryDatabase.CreateAsync();
        var first = Example(Guid.NewGuid(), null, null, "First text.", "first", 0, 5, "", null);
        var second = Example(Guid.NewGuid(), null, null, "Second text.", "second", 0, 6, "", null);
        await database.Repository.SaveAsync(first, CancellationToken.None);
        await database.Repository.SaveAsync(second, CancellationToken.None);
        await database.Repository.SaveLinkAsync(new(database.EntryId, first.Id, true, 2), CancellationToken.None);
        await database.Repository.SaveLinkAsync(new(database.EntryId, second.Id, true, 5), CancellationToken.None);
        await database.Repository.SaveLinkAsync(new(database.EntryId, second.Id, true, 1), CancellationToken.None);

        var details = await database.Repository.GetForEntryAsync(database.EntryId, CancellationToken.None);
        Assert.Equal([second.Id, first.Id], details.Select(item => item.Id));
        Assert.All(details, item => Assert.True(item.IsPrimary));
        Assert.Equal(1, details[0].SortOrder);
        Assert.NotNull(await database.Repository.GetByIdAsync(second.Id, CancellationToken.None));
    }

    [Fact]
    public async Task SaveLinkForeignKeyFailurePreservesExistingLink()
    {
        using var database = await RepositoryDatabase.CreateAsync();
        var example = Example(Guid.NewGuid(), null, null, "Safe text.", "safe", 0, 4, "", null);
        await database.Repository.SaveAsync(example, CancellationToken.None);
        await database.Repository.SaveLinkAsync(new(database.EntryId, example.Id, false, 3), CancellationToken.None);

        await Assert.ThrowsAsync<SqliteException>(() => database.Repository.SaveLinkAsync(
            new EntryExampleLink(Guid.NewGuid(), example.Id, true, 9),
            CancellationToken.None));
        await Assert.ThrowsAsync<SqliteException>(() => database.Repository.SaveLinkAsync(
            new EntryExampleLink(database.EntryId, Guid.NewGuid(), true, 9),
            CancellationToken.None));

        var details = await database.Repository.GetForEntryAsync(database.EntryId, CancellationToken.None);
        Assert.Single(details);
        Assert.False(details[0].IsPrimary);
        Assert.Equal(3, details[0].SortOrder);
    }

    [Fact]
    public async Task GetForEntryReturnsStableReadOnlyOrderingAndRejectsCorruptBoolean()
    {
        using var database = await RepositoryDatabase.CreateAsync();
        Assert.Empty(await database.Repository.GetForEntryAsync(database.EntryId, CancellationToken.None));
        var lowId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var highId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        foreach (var id in new[] { highId, lowId })
        {
            await database.Repository.SaveAsync(
                Example(id, null, null, "Safe text.", "safe", 0, 4, "", null),
                CancellationToken.None);
            await database.Repository.SaveLinkAsync(new(database.EntryId, id, false, 2), CancellationToken.None);
        }

        var details = await database.Repository.GetForEntryAsync(database.EntryId, CancellationToken.None);
        Assert.Equal([lowId, highId], details.Select(item => item.Id));
        Assert.Throws<NotSupportedException>(() =>
            ((IList<GameLexicon.Application.Entries.Queries.SentenceExampleDetails>)details).Add(details[0]));

        await database.ExecuteParameterizedAsync(
            "UPDATE entry_examples SET is_primary=2 WHERE entry_id=$entryId AND example_id=$exampleId;",
            ("$entryId", database.EntryId.ToString("D")),
            ("$exampleId", lowId.ToString("D")));
        await Assert.ThrowsAsync<InvalidDataException>(() =>
            database.Repository.GetForEntryAsync(database.EntryId, CancellationToken.None));
    }

    [Fact]
    public async Task GetForEntryRejectsNegativeStoredSortOrder()
    {
        using var database = await RepositoryDatabase.CreateAsync();
        var id = await database.AddLinkedExampleAsync(false, 1);
        await database.ExecuteParameterizedAsync(
            "UPDATE entry_examples SET sort_order=-1 WHERE entry_id=$entryId AND example_id=$exampleId;",
            ("$entryId", database.EntryId.ToString("D")),
            ("$exampleId", id.ToString("D")));

        await Assert.ThrowsAsync<InvalidDataException>(() =>
            database.Repository.GetForEntryAsync(database.EntryId, CancellationToken.None));
    }

    [Fact]
    public async Task SetPrimaryAtomicallySelectsTargetAndPreservesSortOrder()
    {
        using var database = await RepositoryDatabase.CreateAsync();
        var first = await database.AddLinkedExampleAsync(true, 4);
        var second = await database.AddLinkedExampleAsync(false, 7);

        await database.Repository.SetPrimaryAsync(database.EntryId, second, CancellationToken.None);

        var details = await database.Repository.GetForEntryAsync(database.EntryId, CancellationToken.None);
        Assert.False(details.Single(item => item.Id == first).IsPrimary);
        Assert.True(details.Single(item => item.Id == second).IsPrimary);
        Assert.Equal(4, details.Single(item => item.Id == first).SortOrder);
        Assert.Equal(7, details.Single(item => item.Id == second).SortOrder);
    }

    [Fact]
    public async Task SetPrimaryMissingTargetRollsBackAndPreservesOriginalPrimary()
    {
        using var database = await RepositoryDatabase.CreateAsync();
        var original = await database.AddLinkedExampleAsync(true, 1);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => database.Repository.SetPrimaryAsync(
            database.EntryId,
            Guid.NewGuid(),
            CancellationToken.None));

        var details = await database.Repository.GetForEntryAsync(database.EntryId, CancellationToken.None);
        Assert.True(Assert.Single(details).IsPrimary);
        Assert.Equal(original, details[0].Id);
    }

    [Fact]
    public async Task RemoveLinkIsIdempotentAndNeverDeletesExample()
    {
        using var database = await RepositoryDatabase.CreateAsync();
        var exampleId = await database.AddLinkedExampleAsync(true, 1);

        await database.Repository.RemoveLinkAsync(database.EntryId, exampleId, CancellationToken.None);
        await database.Repository.RemoveLinkAsync(database.EntryId, exampleId, CancellationToken.None);

        Assert.Empty(await database.Repository.GetForEntryAsync(database.EntryId, CancellationToken.None));
        Assert.NotNull(await database.Repository.GetByIdAsync(exampleId, CancellationToken.None));
    }

    [Fact]
    public async Task ConnectionsAndSidecarsAreReleasedForDeletion()
    {
        string directoryPath;
        using (var database = await RepositoryDatabase.CreateAsync())
        {
            directoryPath = database.DirectoryPath;
            var id = Guid.NewGuid();
            await database.Repository.SaveAsync(
                Example(id, null, null, "Safe text.", "safe", 0, 4, "", null),
                CancellationToken.None);
            await database.Repository.GetByIdAsync(id, CancellationToken.None);
            await database.DeleteFilesAsync();
        }

        Assert.False(Directory.Exists(directoryPath));
    }

    private static SentenceExample Example(
        Guid id,
        Guid? captureId,
        Guid? ocrRegionId,
        string text,
        string normalized,
        int start,
        int length,
        string? screenshot,
        string? game) =>
        new(id, captureId, ocrRegionId, text, normalized, start, length, screenshot, game, CreatedAt);

    private static void AssertExample(SentenceExample expected, SentenceExample? actual)
    {
        Assert.NotNull(actual);
        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.CaptureId, actual.CaptureId);
        Assert.Equal(expected.OcrRegionId, actual.OcrRegionId);
        Assert.Equal(expected.SentenceText, actual.SentenceText);
        Assert.Equal(expected.NormalizedSentence, actual.NormalizedSentence);
        Assert.Equal(expected.TargetStart, actual.TargetStart);
        Assert.Equal(expected.TargetLength, actual.TargetLength);
        Assert.Equal(expected.ScreenshotCropPath ?? string.Empty, actual.ScreenshotCropPath);
        Assert.Equal(expected.GameTitle, actual.GameTitle);
        Assert.Equal(expected.CreatedAt, actual.CreatedAt);
    }

    private sealed class RepositoryDatabase : IDisposable
    {
        private readonly TestDirectory _directory;
        private readonly SqliteConnectionFactory _factory;

        private RepositoryDatabase(TestDirectory directory, SqliteConnectionFactory factory)
        {
            _directory = directory;
            _factory = factory;
            Repository = new SqliteSentenceExampleRepository(factory);
        }

        public Guid EntryId { get; } = Guid.NewGuid();
        public Guid CaptureId { get; } = Guid.NewGuid();
        public Guid OcrRegionId { get; } = Guid.NewGuid();
        public SqliteSentenceExampleRepository Repository { get; }
        public string DirectoryPath => _directory.Path;

        public static async Task<RepositoryDatabase> CreateAsync()
        {
            var directory = new TestDirectory();
            var factory = SqliteConnectionFactoryTests.CreateFactory(
                Path.Combine(directory.Path, "gamelexicon.db"));
            var database = new RepositoryDatabase(directory, factory);
            await new MigrationRunner(
                factory,
                [new Migration001_Initial(), new Migration002_ManualExamplesAndSearchSupport()])
                .RunAsync();
            await database.SeedParentsAsync();
            return database;
        }

        public async Task<Guid> AddLinkedExampleAsync(bool primary, int sortOrder)
        {
            var id = Guid.NewGuid();
            await Repository.SaveAsync(
                Example(id, null, null, "Safe text.", "safe", 0, 4, "", null),
                CancellationToken.None);
            await Repository.SaveLinkAsync(new(EntryId, id, primary, sortOrder), CancellationToken.None);
            return id;
        }

        public async Task ExecuteParameterizedAsync(string sql, params (string Name, object Value)[] values)
        {
            await using var connection = await _factory.OpenConnectionAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            foreach (var (name, value) in values)
            {
                command.Parameters.AddWithValue(name, value);
            }

            await command.ExecuteNonQueryAsync();
        }

        public async Task DeleteFilesAsync()
        {
            foreach (var suffix in new[] { "-wal", "-shm", "" })
            {
                var path = _factory.DatabasePath + suffix;
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }

            await Task.CompletedTask;
        }

        public void Dispose() => _directory.Dispose();

        private async Task SeedParentsAsync()
        {
            await using var connection = await _factory.OpenConnectionAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO captures
                    (id, captured_at_utc, image_path, pixel_width, pixel_height, status)
                VALUES ($captureId, $createdAt, 'fixture.png', 1, 1, 0);
                INSERT INTO ocr_regions
                    (id, capture_id, x, y, width, height, created_at_utc)
                VALUES ($regionId, $captureId, 0, 0, 1, 1, $createdAt);
                INSERT INTO vocabulary_entries
                    (id, headword, normalized_headword, entry_type, created_at_utc, updated_at_utc)
                VALUES ($entryId, 'Fixture', $normalizedHeadword, 0, $createdAt, $createdAt);
                """;
            command.Parameters.AddWithValue("$captureId", CaptureId.ToString("D"));
            command.Parameters.AddWithValue("$regionId", OcrRegionId.ToString("D"));
            command.Parameters.AddWithValue("$entryId", EntryId.ToString("D"));
            command.Parameters.AddWithValue("$normalizedHeadword", EntryId.ToString("N"));
            command.Parameters.AddWithValue("$createdAt", "2026-08-02T00:00:00.0000000Z");
            await command.ExecuteNonQueryAsync();
        }
    }
}
