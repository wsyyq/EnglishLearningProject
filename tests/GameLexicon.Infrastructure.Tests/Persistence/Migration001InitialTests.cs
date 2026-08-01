using GameLexicon.Infrastructure.Persistence.Migrations;
using Microsoft.Data.Sqlite;

namespace GameLexicon.Infrastructure.Tests.Persistence;

public sealed class Migration001InitialTests
{
    private static readonly string[] ExpectedTables =
    [
        "schema_migrations",
        "captures",
        "ocr_regions",
        "ocr_tokens",
        "sentence_examples",
        "vocabulary_entries",
        "entry_examples",
        "tags",
        "entry_tags",
        "review_cards",
        "review_logs",
        "app_settings"
    ];

    private static readonly string[] ExpectedIndexes =
    [
        "ux_vocabulary_entries_normalized_active",
        "ux_review_cards_entry_type",
        "ix_review_cards_due"
    ];

    [Fact]
    public async Task InitialMigrationCreatesAllTablesAndIndexes()
    {
        using var directory = new TestDirectory();
        var factory = await CreateMigratedDatabaseAsync(directory.Path);
        await using var connection = await factory.OpenConnectionAsync();

        foreach (var table in ExpectedTables)
        {
            Assert.Equal(1L, await CountSchemaObjectAsync(connection, "table", table));
        }

        foreach (var index in ExpectedIndexes)
        {
            Assert.Equal(1L, await CountSchemaObjectAsync(connection, "index", index));
        }
    }

    [Fact]
    public async Task ForeignKeyRejectsChildWithoutParent()
    {
        using var directory = new TestDirectory();
        var factory = await CreateMigratedDatabaseAsync(directory.Path);
        await using var connection = await factory.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO ocr_regions
                (id, capture_id, x, y, width, height, created_at_utc)
            VALUES
                ('region', 'missing-capture', 0, 0, 1, 1, '2026-08-01T00:00:00.0000000Z');
            """;

        var exception = await Assert.ThrowsAsync<SqliteException>(() => command.ExecuteNonQueryAsync());

        Assert.Equal(19, exception.SqliteErrorCode);
    }

    [Fact]
    public async Task CascadeAndRestrictDeleteBehaviorsAreEnforced()
    {
        using var directory = new TestDirectory();
        var factory = await CreateMigratedDatabaseAsync(directory.Path);
        await using var connection = await factory.OpenConnectionAsync();
        await ExecuteAsync(connection, """
            INSERT INTO captures
                (id, captured_at_utc, image_path, pixel_width, pixel_height, status)
            VALUES ('capture', '2026-08-01T00:00:00.0000000Z', 'image.png', 1, 1, 0);
            INSERT INTO ocr_regions
                (id, capture_id, x, y, width, height, created_at_utc)
            VALUES ('region', 'capture', 0, 0, 1, 1, '2026-08-01T00:00:00.0000000Z');
            """);

        await ExecuteAsync(connection, "DELETE FROM ocr_regions WHERE id='region';");
        Assert.Equal(
            0L,
            await SqliteConnectionFactoryTests.ExecuteScalarAsync<long>(
                connection,
                "SELECT COUNT(*) FROM ocr_regions WHERE id='region';"));

        await ExecuteAsync(connection, """
            INSERT INTO sentence_examples
                (id, capture_id, sentence_text, normalized_sentence, target_start, target_length, created_at_utc)
            VALUES ('example', 'capture', 'Safe test', 'safe test', 0, 4, '2026-08-01T00:00:00.0000000Z');
            """);
        await Assert.ThrowsAsync<SqliteException>(() =>
            ExecuteAsync(connection, "DELETE FROM captures WHERE id='capture';"));
    }

    [Fact]
    public async Task UniqueAndPartialUniqueIndexesAreEnforced()
    {
        using var directory = new TestDirectory();
        var factory = await CreateMigratedDatabaseAsync(directory.Path);
        await using var connection = await factory.OpenConnectionAsync();
        await ExecuteAsync(connection, """
            INSERT INTO tags VALUES ('tag-1', 'Tag One', 'tag');
            INSERT INTO vocabulary_entries
                (id, headword, normalized_headword, entry_type, created_at_utc, updated_at_utc)
            VALUES
                ('entry-1', 'Term', 'term', 0, '2026-08-01T00:00:00.0000000Z', '2026-08-01T00:00:00.0000000Z');
            """);

        await Assert.ThrowsAsync<SqliteException>(() =>
            ExecuteAsync(connection, "INSERT INTO tags VALUES ('tag-2', 'Tag Two', 'tag');"));
        await Assert.ThrowsAsync<SqliteException>(() => ExecuteAsync(connection, """
            INSERT INTO vocabulary_entries
                (id, headword, normalized_headword, entry_type, created_at_utc, updated_at_utc)
            VALUES
                ('entry-2', 'TERM', 'term', 0, '2026-08-01T00:00:00.0000000Z', '2026-08-01T00:00:00.0000000Z');
            """));

        await ExecuteAsync(connection, "UPDATE vocabulary_entries SET is_archived=1 WHERE id='entry-1';");
        await ExecuteAsync(connection, """
            INSERT INTO vocabulary_entries
                (id, headword, normalized_headword, entry_type, created_at_utc, updated_at_utc)
            VALUES
                ('entry-2', 'TERM', 'term', 0, '2026-08-01T00:00:00.0000000Z', '2026-08-01T00:00:00.0000000Z');
            """);
    }

    private static async Task<GameLexicon.Infrastructure.Persistence.SqliteConnectionFactory>
        CreateMigratedDatabaseAsync(string directory)
    {
        var factory = SqliteConnectionFactoryTests.CreateFactory(
            Path.Combine(directory, "gamelexicon.db"));
        var result = await new MigrationRunner(factory, [new Migration001_Initial()]).RunAsync();
        Assert.Equal(1, result.CurrentVersion);
        return factory;
    }

    private static async Task<long> CountSchemaObjectAsync(
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

    private static async Task ExecuteAsync(SqliteConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }
}
