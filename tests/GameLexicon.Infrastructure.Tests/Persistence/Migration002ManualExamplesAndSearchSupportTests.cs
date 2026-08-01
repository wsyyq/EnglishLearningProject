using GameLexicon.Infrastructure.Persistence;
using GameLexicon.Infrastructure.Persistence.Migrations;
using Microsoft.Data.Sqlite;

namespace GameLexicon.Infrastructure.Tests.Persistence;

public sealed class Migration002ManualExamplesAndSearchSupportTests
{
    private static readonly string[] RequiredIndexes =
    [
        "ix_vocabulary_entries_archive_updated",
        "ix_vocabulary_entries_archive_type_updated",
        "ix_entry_examples_entry_sort",
        "ix_entry_examples_example_entry",
        "ix_entry_tags_tag_entry",
        "ix_sentence_examples_game_created"
    ];

    [Fact]
    public void VersionIsTwo()
    {
        Assert.Equal(2, new Migration002_ManualExamplesAndSearchSupport().Version);
    }

    [Fact]
    public async Task EmptyDatabaseMigratesToVersionTwoWithExpectedSchemaAndIndexes()
    {
        using var directory = new TestDirectory();
        var factory = CreateFactory(directory.Path);

        var result = await CreateRunner(factory).RunAsync();

        Assert.Equal(2, result.CurrentVersion);
        Assert.Equal([1, 2], result.AppliedVersions);
        await using var connection = await factory.OpenConnectionAsync();
        Assert.Equal(2L, await ScalarAsync<long>(connection, "SELECT COUNT(*) FROM schema_migrations;"));
        Assert.Equal(0L, await ForeignKeyViolationCountAsync(connection));
        Assert.Equal(0L, await TemporaryObjectCountAsync(connection));
        Assert.Equal(0L, await CaptureIdNotNullAsync(connection));
        Assert.Equal(10L, await ScalarAsync<long>(
            connection,
            "SELECT COUNT(*) FROM pragma_table_info('sentence_examples');"));
        Assert.Equal(6L, await ScalarAsync<long>(connection, """
            SELECT COUNT(*) FROM pragma_table_info('sentence_examples')
            WHERE name IN ('sentence_text', 'normalized_sentence', 'target_start',
                           'target_length', 'screenshot_crop_path', 'created_at_utc')
              AND "notnull"=1;
            """));
        foreach (var index in RequiredIndexes)
        {
            Assert.Equal(1L, await SchemaObjectCountAsync(connection, "index", index));
        }

        Assert.Equal(
            ["is_archived", "updated_at_utc", "id"],
            await IndexColumnsAsync(connection, "ix_vocabulary_entries_archive_updated"));
        Assert.Equal(
            ["is_archived", "entry_type", "updated_at_utc", "id"],
            await IndexColumnsAsync(connection, "ix_vocabulary_entries_archive_type_updated"));
        Assert.Equal(
            ["entry_id", "sort_order", "example_id"],
            await IndexColumnsAsync(connection, "ix_entry_examples_entry_sort"));
        Assert.Equal(
            ["example_id", "entry_id"],
            await IndexColumnsAsync(connection, "ix_entry_examples_example_entry"));
        Assert.Equal(
            ["tag_id", "entry_id"],
            await IndexColumnsAsync(connection, "ix_entry_tags_tag_entry"));
        Assert.Equal(
            ["game_title", "created_at_utc", "id"],
            await IndexColumnsAsync(connection, "ix_sentence_examples_game_created"));
        var archiveIndexSql = await SchemaSqlAsync(
            connection,
            "ix_vocabulary_entries_archive_updated");
        Assert.Contains("updated_at_utc DESC", archiveIndexSql, StringComparison.OrdinalIgnoreCase);
        var gameIndexSql = await SchemaSqlAsync(connection, "ix_sentence_examples_game_created");
        Assert.Contains("game_title COLLATE NOCASE", gameIndexSql, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1L, await SchemaObjectCountAsync(
            connection,
            "index",
            "ux_vocabulary_entries_normalized_active"));
        Assert.Equal(0L, await ScalarAsync<long>(connection, """
            SELECT COUNT(*) FROM sqlite_master
            WHERE type='table' AND name LIKE '%fts%';
            """));
    }

    [Fact]
    public async Task VersionOneUpgradePreservesAllExampleAndLinkFields()
    {
        using var directory = new TestDirectory();
        var factory = CreateFactory(directory.Path);
        await new MigrationRunner(factory, [new Migration001_Initial()]).RunAsync();
        await using (var connection = await factory.OpenConnectionAsync())
        {
            await ExecuteAsync(connection, SeedVersionOneSql);
        }

        var result = await CreateRunner(factory).RunAsync();

        Assert.Equal([2], result.AppliedVersions);
        await using var verification = await factory.OpenConnectionAsync();
        Assert.Equal(2L, await ScalarAsync<long>(verification, "SELECT COUNT(*) FROM sentence_examples;"));
        Assert.Equal(2L, await ScalarAsync<long>(verification, "SELECT COUNT(*) FROM entry_examples;"));
        Assert.Equal(1L, await ScalarAsync<long>(verification, "SELECT COUNT(*) FROM entry_tags;"));
        Assert.Equal(1L, await ScalarAsync<long>(verification, """
            SELECT COUNT(*) FROM sentence_examples
            WHERE id='example-capture'
              AND capture_id='capture'
              AND ocr_region_id IS NULL
              AND sentence_text='Captured sentence.'
              AND normalized_sentence='captured sentence'
              AND target_start=0
              AND target_length=8
              AND screenshot_crop_path='capture.png'
              AND game_title='Test Game'
              AND created_at_utc='2026-08-01T00:00:00.0000000Z';
            """));
        Assert.Equal(1L, await ScalarAsync<long>(verification, """
            SELECT COUNT(*) FROM sentence_examples
            WHERE id='example-ocr' AND capture_id='capture' AND ocr_region_id='region';
            """));
        Assert.Equal(1L, await ScalarAsync<long>(verification, """
            SELECT COUNT(*) FROM entry_examples
            WHERE entry_id='entry' AND example_id='example-ocr'
              AND is_primary=1 AND sort_order=7;
            """));
        Assert.Equal(0L, await ForeignKeyViolationCountAsync(verification));
        Assert.Equal(0L, await TemporaryObjectCountAsync(verification));

        await ExecuteAsync(verification, """
            INSERT INTO sentence_examples
                (id, capture_id, ocr_region_id, sentence_text, normalized_sentence,
                 target_start, target_length, created_at_utc)
            VALUES
                ('manual', NULL, NULL, 'Manual sentence.', 'manual sentence',
                 0, 6, '2026-08-01T00:00:00.0000000Z');
            """);
        Assert.Equal(1L, await ScalarAsync<long>(
            verification,
            "SELECT COUNT(*) FROM sentence_examples WHERE id='manual';"));
    }

    [Fact]
    public async Task SourceConstraintAndForeignKeyBehaviorsAreEnforced()
    {
        using var directory = new TestDirectory();
        var factory = CreateFactory(directory.Path);
        await CreateRunner(factory).RunAsync();
        await using var connection = await factory.OpenConnectionAsync();
        await ExecuteAsync(connection, SeedParentsSql);

        await ExecuteAsync(connection, ExampleInsert("manual", "NULL", "NULL"));
        await ExecuteAsync(connection, ExampleInsert("capture-only", "'capture'", "NULL"));
        await ExecuteAsync(connection, ExampleInsert("ocr", "'capture'", "'region'"));

        await Assert.ThrowsAsync<SqliteException>(() =>
            ExecuteAsync(connection, ExampleInsert("invalid-source", "NULL", "'region'")));
        await Assert.ThrowsAsync<SqliteException>(() =>
            ExecuteAsync(connection, ExampleInsert("missing-capture", "'missing'", "NULL")));
        await Assert.ThrowsAsync<SqliteException>(() =>
            ExecuteAsync(connection, ExampleInsert("missing-region", "'capture'", "'missing'")));
        await Assert.ThrowsAsync<SqliteException>(() =>
            ExecuteAsync(connection, "DELETE FROM captures WHERE id='capture';"));

        await ExecuteAsync(connection, "DELETE FROM ocr_regions WHERE id='region';");
        Assert.Equal(1L, await ScalarAsync<long>(connection, """
            SELECT COUNT(*) FROM sentence_examples
            WHERE id='ocr' AND capture_id='capture' AND ocr_region_id IS NULL;
            """));

        await ExecuteAsync(connection, """
            INSERT INTO entry_examples(entry_id, example_id, is_primary, sort_order)
            VALUES ('entry', 'manual', 1, 0);
            DELETE FROM sentence_examples WHERE id='manual';
            """);
        Assert.Equal(0L, await ScalarAsync<long>(connection, "SELECT COUNT(*) FROM entry_examples;"));
        await ExecuteAsync(connection, """
            INSERT INTO entry_examples(entry_id, example_id, is_primary, sort_order)
            VALUES ('entry', 'capture-only', 1, 0);
            DELETE FROM vocabulary_entries WHERE id='entry';
            """);
        Assert.Equal(0L, await ScalarAsync<long>(connection, "SELECT COUNT(*) FROM entry_examples;"));
    }

    [Fact]
    public async Task SecondRunIsIdempotent()
    {
        using var directory = new TestDirectory();
        var factory = CreateFactory(directory.Path);
        var runner = CreateRunner(factory);

        var first = await runner.RunAsync();
        var second = await runner.RunAsync();

        Assert.Equal([1, 2], first.AppliedVersions);
        Assert.Empty(second.AppliedVersions);
        Assert.Equal(2, second.CurrentVersion);
        await using var connection = await factory.OpenConnectionAsync();
        Assert.Equal(1L, await ScalarAsync<long>(
            connection,
            "SELECT COUNT(*) FROM schema_migrations WHERE version=2;"));
        foreach (var index in RequiredIndexes)
        {
            Assert.Equal(1L, await SchemaObjectCountAsync(connection, "index", index));
        }
    }

    [Fact]
    public async Task IndexCreationFailureRollsBackEntireVersionTwoMigration()
    {
        using var directory = new TestDirectory();
        var factory = CreateFactory(directory.Path);
        await new MigrationRunner(factory, [new Migration001_Initial()]).RunAsync();
        await using (var connection = await factory.OpenConnectionAsync())
        {
            await ExecuteAsync(connection, SeedVersionOneSql);
            await ExecuteAsync(connection, """
                CREATE INDEX ix_vocabulary_entries_archive_updated
                ON vocabulary_entries(headword);
                """);
        }

        await Assert.ThrowsAsync<SqliteException>(() => CreateRunner(factory).RunAsync());

        await using var verification = await factory.OpenConnectionAsync();
        Assert.Equal(1L, await CaptureIdNotNullAsync(verification));
        Assert.Equal(0L, await ScalarAsync<long>(
            verification,
            "SELECT COUNT(*) FROM schema_migrations WHERE version=2;"));
        Assert.Equal(2L, await ScalarAsync<long>(verification, "SELECT COUNT(*) FROM sentence_examples;"));
        Assert.Equal(2L, await ScalarAsync<long>(verification, "SELECT COUNT(*) FROM entry_examples;"));
        Assert.Equal(0L, await TemporaryObjectCountAsync(verification));
        Assert.Equal(["headword"], await IndexColumnsAsync(
            verification,
            "ix_vocabulary_entries_archive_updated"));
    }

    [Fact]
    public async Task DatabaseAndSidecarsCanBeDeletedAfterConnectionsAreDisposed()
    {
        string directoryPath;
        string databasePath;
        using (var directory = new TestDirectory())
        {
            directoryPath = directory.Path;
            databasePath = Path.Combine(directory.Path, "gamelexicon.db");
            var factory = CreateFactory(directory.Path);
            await CreateRunner(factory).RunAsync();
            await using (var connection = await factory.OpenConnectionAsync())
            {
                await ExecuteAsync(connection, "CREATE TABLE deletion_probe (id INTEGER PRIMARY KEY);");
            }

            foreach (var path in new[] { databasePath, databasePath + "-wal", databasePath + "-shm" })
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }

            Assert.False(File.Exists(databasePath));
            Assert.False(File.Exists(databasePath + "-wal"));
            Assert.False(File.Exists(databasePath + "-shm"));
        }

        Assert.False(Directory.Exists(directoryPath));
    }

    private static MigrationRunner CreateRunner(SqliteConnectionFactory factory) =>
        new(factory, [new Migration001_Initial(), new Migration002_ManualExamplesAndSearchSupport()]);

    private static SqliteConnectionFactory CreateFactory(string directory) =>
        SqliteConnectionFactoryTests.CreateFactory(Path.Combine(directory, "gamelexicon.db"));

    private static async Task ExecuteAsync(SqliteConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<T> ScalarAsync<T>(SqliteConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return (T)(await command.ExecuteScalarAsync())!;
    }

    private static async Task<long> SchemaObjectCountAsync(
        SqliteConnection connection,
        string type,
        string name)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type=$type AND name=$name;";
        command.Parameters.AddWithValue("$type", type);
        command.Parameters.AddWithValue("$name", name);
        return (long)(await command.ExecuteScalarAsync())!;
    }

    private static async Task<string> SchemaSqlAsync(
        SqliteConnection connection,
        string name)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT sql FROM sqlite_master WHERE name=$name;";
        command.Parameters.AddWithValue("$name", name);
        return (string)(await command.ExecuteScalarAsync())!;
    }

    private static async Task<long> CaptureIdNotNullAsync(SqliteConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA table_info(sentence_examples);";
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            if (reader.GetString(1) == "capture_id")
            {
                return reader.GetInt64(3);
            }
        }

        throw new InvalidOperationException("capture_id column is missing.");
    }

    private static async Task<string[]> IndexColumnsAsync(
        SqliteConnection connection,
        string indexName)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA index_info('{indexName}');";
        await using var reader = await command.ExecuteReaderAsync();
        var columns = new List<string>();
        while (await reader.ReadAsync())
        {
            columns.Add(reader.GetString(2));
        }

        return [.. columns];
    }

    private static Task<long> ForeignKeyViolationCountAsync(SqliteConnection connection) =>
        ScalarAsync<long>(connection, "SELECT COUNT(*) FROM pragma_foreign_key_check;");

    private static Task<long> TemporaryObjectCountAsync(SqliteConnection connection) =>
        ScalarAsync<long>(connection, """
            SELECT COUNT(*) FROM sqlite_master
            WHERE name IN ('sentence_examples_m002_new', 'entry_examples_m002_backup');
            """);

    private static string ExampleInsert(string id, string captureId, string regionId) => $"""
        INSERT INTO sentence_examples
            (id, capture_id, ocr_region_id, sentence_text, normalized_sentence,
             target_start, target_length, created_at_utc)
        VALUES
            ('{id}', {captureId}, {regionId}, 'Safe sentence.', 'safe sentence',
             0, 4, '2026-08-01T00:00:00.0000000Z');
        """;

    private const string SeedParentsSql = """
        INSERT INTO captures
            (id, captured_at_utc, image_path, pixel_width, pixel_height, status)
        VALUES ('capture', '2026-08-01T00:00:00.0000000Z', 'capture.png', 1, 1, 0);
        INSERT INTO ocr_regions
            (id, capture_id, x, y, width, height, created_at_utc)
        VALUES ('region', 'capture', 0, 0, 1, 1, '2026-08-01T00:00:00.0000000Z');
        INSERT INTO vocabulary_entries
            (id, headword, normalized_headword, entry_type, created_at_utc, updated_at_utc)
        VALUES ('entry', 'Safe', 'safe', 0,
                '2026-08-01T00:00:00.0000000Z', '2026-08-01T00:00:00.0000000Z');
        """;

    private const string SeedVersionOneSql = SeedParentsSql + """
        INSERT INTO sentence_examples
            (id, capture_id, ocr_region_id, sentence_text, normalized_sentence,
             target_start, target_length, screenshot_crop_path, game_title, created_at_utc)
        VALUES
            ('example-capture', 'capture', NULL, 'Captured sentence.', 'captured sentence',
             0, 8, 'capture.png', 'Test Game', '2026-08-01T00:00:00.0000000Z'),
            ('example-ocr', 'capture', 'region', 'OCR sentence.', 'ocr sentence',
             4, 8, 'ocr.png', 'Test Game', '2026-08-01T00:00:01.0000000Z');
        INSERT INTO entry_examples(entry_id, example_id, is_primary, sort_order)
        VALUES
            ('entry', 'example-capture', 0, 3),
            ('entry', 'example-ocr', 1, 7);
        INSERT INTO tags(id, name, normalized_name) VALUES ('tag', 'Safe Tag', 'safe tag');
        INSERT INTO entry_tags(entry_id, tag_id) VALUES ('entry', 'tag');
        """;
}
