using GameLexicon.Domain.Entries;
using GameLexicon.Infrastructure.Persistence;
using GameLexicon.Infrastructure.Persistence.Migrations;
using GameLexicon.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;

namespace GameLexicon.Infrastructure.Tests.Persistence.Repositories;

public sealed class SqliteTagRepositoryTests
{
    [Fact]
    public void ConstructorRejectsNullFactory()
    {
        Assert.Throws<ArgumentNullException>(() => new SqliteTagRepository(null!));
    }

    [Fact]
    public async Task FindUsesExactUnmodifiedNormalizedName()
    {
        using var database = await TagDatabase.CreateAsync();
        var tag = await database.AddTagAsync("Quest", "quest");

        AssertTag(tag, await database.Repository.FindByNormalizedNameAsync("quest", CancellationToken.None));
        Assert.Null(await database.Repository.FindByNormalizedNameAsync("Quest", CancellationToken.None));
        Assert.Null(await database.Repository.FindByNormalizedNameAsync(" quest ", CancellationToken.None));
        Assert.Null(await database.Repository.FindByNormalizedNameAsync("missing", CancellationToken.None));
    }

    [Fact]
    public async Task FindValidatesInputAndCancellation()
    {
        using var database = await TagDatabase.CreateAsync();
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            database.Repository.FindByNormalizedNameAsync(null!, CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            database.Repository.FindByNormalizedNameAsync(string.Empty, CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            database.Repository.FindByNormalizedNameAsync(" \t", CancellationToken.None));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            database.Repository.FindByNormalizedNameAsync("quest", cancellation.Token));
    }

    [Theory]
    [InlineData("id", "not-a-guid")]
    [InlineData("name", "   ")]
    public async Task FindRejectsCorruptStoredTag(string column, string value)
    {
        using var database = await TagDatabase.CreateAsync();
        await database.AddTagAsync("Safe", "safe");
        await database.ExecuteAsync(
            $"UPDATE tags SET {column}=$value WHERE normalized_name=$normalizedName;",
            ("$value", value),
            ("$normalizedName", "safe"));

        await Assert.ThrowsAsync<InvalidDataException>(() =>
            database.Repository.FindByNormalizedNameAsync("safe", CancellationToken.None));
    }

    [Fact]
    public async Task GetOrCreateInsertsAndIsIdempotentWithoutOverwritingExistingName()
    {
        using var database = await TagDatabase.CreateAsync();
        var original = new Tag(Guid.NewGuid(), "Quest", "quest");
        AssertTag(original, await database.Repository.GetOrCreateAsync(original, CancellationToken.None));
        AssertTag(original, await database.Repository.GetOrCreateAsync(original, CancellationToken.None));

        var competing = new Tag(Guid.NewGuid(), "Changed", "quest");
        var persisted = await database.Repository.GetOrCreateAsync(competing, CancellationToken.None);
        AssertTag(original, persisted);
        Assert.Equal(1, await database.CountAsync("tags"));
    }

    [Fact]
    public async Task GetOrCreateRejectsNullCancellationAndUnrelatedPrimaryKeyConflict()
    {
        using var database = await TagDatabase.CreateAsync();
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            database.Repository.GetOrCreateAsync(null!, CancellationToken.None));

        var existing = await database.AddTagAsync("First", "first");
        var conflicting = new Tag(existing.Id, "Second", "second");
        await Assert.ThrowsAsync<SqliteException>(() =>
            database.Repository.GetOrCreateAsync(conflicting, CancellationToken.None));
        Assert.Null(await database.Repository.FindByNormalizedNameAsync("second", CancellationToken.None));
        Assert.Equal(1, await database.CountAsync("tags"));

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            database.Repository.GetOrCreateAsync(
                new Tag(Guid.NewGuid(), "Cancelled", "cancelled"),
                cancellation.Token));
        Assert.Equal(1, await database.CountAsync("tags"));
    }

    [Fact]
    public async Task ConcurrentGetOrCreateReturnsOnePersistentTag()
    {
        using var database = await TagDatabase.CreateAsync();
        var firstRepository = new SqliteTagRepository(database.Factory);
        var secondRepository = new SqliteTagRepository(database.Factory);
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task<Tag> CreateAsync(SqliteTagRepository repository, Tag candidate)
        {
            await gate.Task;
            return await repository.GetOrCreateAsync(candidate, CancellationToken.None);
        }

        var firstTask = CreateAsync(firstRepository, new Tag(Guid.NewGuid(), "First", "shared"));
        var secondTask = CreateAsync(secondRepository, new Tag(Guid.NewGuid(), "Second", "shared"));
        gate.SetResult();
        var results = await Task.WhenAll(firstTask, secondTask);

        Assert.Equal(results[0].Id, results[1].Id);
        Assert.Equal(results[0].Name, results[1].Name);
        Assert.Equal("shared", results[0].NormalizedName);
        Assert.Equal(1, await database.CountAsync("tags"));
    }

    [Fact]
    public async Task GetForEntryReturnsStableReadOnlyEntryScopedTags()
    {
        using var database = await TagDatabase.CreateAsync();
        var otherEntry = await database.AddEntryAsync();
        var zulu = await database.AddTagAsync("Zulu", "zulu");
        var alpha = await database.AddTagAsync("Alpha", "alpha");
        var shared = await database.AddTagAsync("Shared", "shared");
        await database.SetLinksRawAsync(database.EntryId, zulu.Id, alpha.Id, shared.Id);
        await database.SetLinksRawAsync(otherEntry, shared.Id);

        var tags = await database.Repository.GetForEntryAsync(database.EntryId, CancellationToken.None);
        Assert.Equal([alpha.Id, shared.Id, zulu.Id], tags.Select(tag => tag.Id));
        Assert.Equal(["Alpha", "Shared", "Zulu"], tags.Select(tag => tag.Name));
        Assert.Throws<NotSupportedException>(() => ((IList<Tag>)tags).Add(alpha));
        Assert.Empty(await database.Repository.GetForEntryAsync(Guid.NewGuid(), CancellationToken.None));
        Assert.Empty(await database.Repository.GetForEntryAsync(await database.AddEntryAsync(), CancellationToken.None));
        Assert.Single(await database.Repository.GetForEntryAsync(otherEntry, CancellationToken.None));
    }

    [Fact]
    public async Task GetForEntryValidatesInputCancellationAndCorruptRows()
    {
        using var database = await TagDatabase.CreateAsync();
        await Assert.ThrowsAsync<ArgumentException>(() =>
            database.Repository.GetForEntryAsync(Guid.Empty, CancellationToken.None));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            database.Repository.GetForEntryAsync(database.EntryId, cancellation.Token));

        var tag = await database.AddTagAsync("Safe", "safe");
        await database.SetLinksRawAsync(database.EntryId, tag.Id);
        await database.ExecuteAsync(
            "UPDATE tags SET normalized_name='   ' WHERE id=$id;",
            ("$id", FormatGuid(tag.Id)));
        await Assert.ThrowsAsync<InvalidDataException>(() =>
            database.Repository.GetForEntryAsync(database.EntryId, CancellationToken.None));
    }

    [Fact]
    public async Task SetForEntryReplacesClearsAndPreservesTagsAndOtherEntries()
    {
        using var database = await TagDatabase.CreateAsync();
        var otherEntry = await database.AddEntryAsync();
        var a = await database.AddTagAsync("A", "a");
        var b = await database.AddTagAsync("B", "b");
        var c = await database.AddTagAsync("C", "c");
        var unused = await database.AddTagAsync("Unused", "unused");
        await database.Repository.SetForEntryAsync(database.EntryId, [a.Id, b.Id], CancellationToken.None);
        await database.Repository.SetForEntryAsync(otherEntry, [a.Id], CancellationToken.None);

        await database.Repository.SetForEntryAsync(database.EntryId, [c.Id, b.Id], CancellationToken.None);
        await database.Repository.SetForEntryAsync(database.EntryId, [b.Id, c.Id], CancellationToken.None);
        Assert.Equal([b.Id, c.Id], (await database.Repository.GetForEntryAsync(database.EntryId, CancellationToken.None)).Select(tag => tag.Id));
        Assert.Equal(a.Id, Assert.Single(await database.Repository.GetForEntryAsync(otherEntry, CancellationToken.None)).Id);

        await database.Repository.SetForEntryAsync(database.EntryId, [], CancellationToken.None);
        Assert.Empty(await database.Repository.GetForEntryAsync(database.EntryId, CancellationToken.None));
        Assert.Equal(4, await database.CountAsync("tags"));
        Assert.NotNull(await database.Repository.FindByNormalizedNameAsync(unused.NormalizedName, CancellationToken.None));
    }

    [Fact]
    public async Task SetForEntryValidatesSnapshotBeforeAwaitAndRejectsMissingRowsAtomically()
    {
        using var database = await TagDatabase.CreateAsync();
        var a = await database.AddTagAsync("A", "a");
        var b = await database.AddTagAsync("B", "b");
        await database.Repository.SetForEntryAsync(database.EntryId, [a.Id], CancellationToken.None);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            database.Repository.SetForEntryAsync(database.EntryId, null!, CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            database.Repository.SetForEntryAsync(Guid.Empty, [a.Id], CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            database.Repository.SetForEntryAsync(database.EntryId, [Guid.Empty], CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            database.Repository.SetForEntryAsync(database.EntryId, [a.Id, a.Id], CancellationToken.None));
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            database.Repository.SetForEntryAsync(Guid.NewGuid(), [], CancellationToken.None));
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            database.Repository.SetForEntryAsync(database.EntryId, [b.Id, Guid.NewGuid()], CancellationToken.None));
        Assert.Equal(a.Id, Assert.Single(await database.Repository.GetForEntryAsync(database.EntryId, CancellationToken.None)).Id);

        var mutable = new List<Guid> { b.Id };
        var replacement = database.Repository.SetForEntryAsync(database.EntryId, mutable, CancellationToken.None);
        mutable.Clear();
        await replacement;
        Assert.Equal(b.Id, Assert.Single(await database.Repository.GetForEntryAsync(database.EntryId, CancellationToken.None)).Id);
    }

    [Fact]
    public async Task SetForEntryRollsBackDeleteWhenInsertTriggerFails()
    {
        using var database = await TagDatabase.CreateAsync();
        var a = await database.AddTagAsync("A", "a");
        var b = await database.AddTagAsync("B", "b");
        var c = await database.AddTagAsync("C", "c");
        await database.Repository.SetForEntryAsync(database.EntryId, [a.Id, b.Id], CancellationToken.None);
        await database.ExecuteAsync("""
            CREATE TRIGGER fail_m1_t07_entry_tag_insert
            BEFORE INSERT ON entry_tags
            BEGIN
                SELECT RAISE(ABORT, 'm1-t07-test');
            END;
            """);

        await Assert.ThrowsAsync<SqliteException>(() =>
            database.Repository.SetForEntryAsync(database.EntryId, [b.Id, c.Id], CancellationToken.None));

        var tags = await database.Repository.GetForEntryAsync(database.EntryId, CancellationToken.None);
        Assert.Equal([a.Id, b.Id], tags.Select(tag => tag.Id));
    }

    [Fact]
    public async Task SetForEntryPreCancellationDoesNotModifyLinks()
    {
        using var database = await TagDatabase.CreateAsync();
        var a = await database.AddTagAsync("A", "a");
        var b = await database.AddTagAsync("B", "b");
        await database.Repository.SetForEntryAsync(database.EntryId, [a.Id], CancellationToken.None);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            database.Repository.SetForEntryAsync(database.EntryId, [b.Id], cancellation.Token));
        Assert.Equal(a.Id, Assert.Single(await database.Repository.GetForEntryAsync(database.EntryId, CancellationToken.None)).Id);
    }

    [Fact]
    public async Task DatabaseConstraintsAndCascadeBehaviorRemainActive()
    {
        using var database = await TagDatabase.CreateAsync();
        var firstEntry = database.EntryId;
        var secondEntry = await database.AddEntryAsync();
        var tag = await database.AddTagAsync("Tag", "tag");
        await database.SetLinksRawAsync(firstEntry, tag.Id);
        await database.SetLinksRawAsync(secondEntry, tag.Id);

        await Assert.ThrowsAsync<SqliteException>(() => database.ExecuteAsync(
            "INSERT INTO tags(id,name,normalized_name) VALUES($id,'Duplicate','tag');",
            ("$id", FormatGuid(Guid.NewGuid()))));
        await Assert.ThrowsAsync<SqliteException>(() => database.SetLinksRawAsync(firstEntry, tag.Id));

        await database.ExecuteAsync("DELETE FROM vocabulary_entries WHERE id=$id;", ("$id", FormatGuid(firstEntry)));
        Assert.NotNull(await database.Repository.FindByNormalizedNameAsync("tag", CancellationToken.None));
        Assert.Single(await database.Repository.GetForEntryAsync(secondEntry, CancellationToken.None));
        await database.ExecuteAsync("DELETE FROM tags WHERE id=$id;", ("$id", FormatGuid(tag.Id)));
        Assert.Empty(await database.Repository.GetForEntryAsync(secondEntry, CancellationToken.None));
    }

    [Fact]
    public async Task ConnectionsAndSidecarsAreReleasedForDeletion()
    {
        string directoryPath;
        using (var database = await TagDatabase.CreateAsync())
        {
            directoryPath = database.DirectoryPath;
            var tag = await database.AddTagAsync("Safe", "safe");
            await database.Repository.FindByNormalizedNameAsync(tag.NormalizedName, CancellationToken.None);
            await database.Repository.SetForEntryAsync(database.EntryId, [tag.Id], CancellationToken.None);
            database.DeleteFiles();
        }

        Assert.False(Directory.Exists(directoryPath));
    }

    private static string FormatGuid(Guid value) => value.ToString("D").ToLowerInvariant();

    private static void AssertTag(Tag expected, Tag? actual)
    {
        Assert.NotNull(actual);
        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.NormalizedName, actual.NormalizedName);
    }

    private sealed class TagDatabase : IDisposable
    {
        private readonly TestDirectory _directory;

        private TagDatabase(TestDirectory directory, SqliteConnectionFactory factory)
        {
            _directory = directory;
            Factory = factory;
            Repository = new SqliteTagRepository(factory);
        }

        public Guid EntryId { get; } = Guid.NewGuid();
        public SqliteConnectionFactory Factory { get; }
        public SqliteTagRepository Repository { get; }
        public string DirectoryPath => _directory.Path;

        public static async Task<TagDatabase> CreateAsync()
        {
            var directory = new TestDirectory();
            var factory = SqliteConnectionFactoryTests.CreateFactory(Path.Combine(directory.Path, "gamelexicon.db"));
            var database = new TagDatabase(directory, factory);
            await new MigrationRunner(
                factory,
                [new Migration001_Initial(), new Migration002_ManualExamplesAndSearchSupport()])
                .RunAsync();
            await database.AddEntryAsync(database.EntryId);
            return database;
        }

        public async Task<Tag> AddTagAsync(string name, string normalizedName)
        {
            var tag = new Tag(Guid.NewGuid(), name, normalizedName);
            return await Repository.GetOrCreateAsync(tag, CancellationToken.None);
        }

        public Task<Guid> AddEntryAsync() => AddEntryAsync(Guid.NewGuid());

        public async Task<Guid> AddEntryAsync(Guid id)
        {
            await ExecuteAsync("""
                INSERT INTO vocabulary_entries
                    (id, headword, normalized_headword, entry_type, created_at_utc, updated_at_utc)
                VALUES ($id, 'Fixture', $normalizedHeadword, 0, $createdAt, $createdAt);
                """,
                ("$id", FormatGuid(id)),
                ("$normalizedHeadword", id.ToString("N")),
                ("$createdAt", "2026-08-02T00:00:00.0000000Z"));
            return id;
        }

        public async Task SetLinksRawAsync(Guid entryId, params Guid[] tagIds)
        {
            foreach (var tagId in tagIds)
            {
                await ExecuteAsync(
                    "INSERT INTO entry_tags(entry_id,tag_id) VALUES($entryId,$tagId);",
                    ("$entryId", FormatGuid(entryId)),
                    ("$tagId", FormatGuid(tagId)));
            }
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

        public async Task<int> CountAsync(string tableName)
        {
            await using var connection = await Factory.OpenConnectionAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = tableName switch
            {
                "tags" => "SELECT COUNT(*) FROM tags;",
                _ => throw new ArgumentOutOfRangeException(nameof(tableName))
            };
            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }

        public void DeleteFiles()
        {
            foreach (var suffix in new[] { "-wal", "-shm", "" })
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
