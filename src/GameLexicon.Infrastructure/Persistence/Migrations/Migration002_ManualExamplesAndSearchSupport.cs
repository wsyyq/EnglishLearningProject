using Microsoft.Data.Sqlite;

namespace GameLexicon.Infrastructure.Persistence.Migrations;

public sealed class Migration002_ManualExamplesAndSearchSupport : IDatabaseMigration
{
    public int Version => 2;

    public async Task ApplyAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(transaction);
        cancellationToken.ThrowIfCancellationRequested();

        await EnsureExpectedVersionOneSchemaAsync(connection, transaction, cancellationToken)
            .ConfigureAwait(false);
        var exampleCount = await CountRowsAsync(
            connection,
            transaction,
            "sentence_examples",
            cancellationToken).ConfigureAwait(false);
        var linkCount = await CountRowsAsync(
            connection,
            transaction,
            "entry_examples",
            cancellationToken).ConfigureAwait(false);

        await ExecuteAsync(connection, transaction, RebuildSql, cancellationToken).ConfigureAwait(false);

        await EnsureRowCountAsync(
            connection,
            transaction,
            "sentence_examples",
            exampleCount,
            cancellationToken).ConfigureAwait(false);
        await EnsureRowCountAsync(
            connection,
            transaction,
            "entry_examples",
            linkCount,
            cancellationToken).ConfigureAwait(false);
        await EnsureForeignKeysValidAsync(connection, transaction, cancellationToken).ConfigureAwait(false);
    }

    private static async Task EnsureExpectedVersionOneSchemaAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        CancellationToken cancellationToken)
    {
        foreach (var temporaryObject in new[]
                 {
                     "sentence_examples_m002_new",
                     "entry_examples_m002_backup"
                 })
        {
            await using var objectCommand = connection.CreateCommand();
            objectCommand.Transaction = transaction;
            objectCommand.CommandText =
                "SELECT COUNT(*) FROM sqlite_master WHERE name = $name;";
            objectCommand.Parameters.AddWithValue("$name", temporaryObject);
            var objectCount = (long)(await objectCommand
                .ExecuteScalarAsync(cancellationToken)
                .ConfigureAwait(false) ?? 0L);
            if (objectCount != 0)
            {
                throw new InvalidOperationException(
                    "Migration002 cannot run while a reserved temporary object exists.");
            }
        }

        await using var columnCommand = connection.CreateCommand();
        columnCommand.Transaction = transaction;
        columnCommand.CommandText = "PRAGMA table_info(sentence_examples);";
        await using var reader = await columnCommand
            .ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);
        var captureColumnFound = false;
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            if (!string.Equals(reader.GetString(1), "capture_id", StringComparison.Ordinal))
            {
                continue;
            }

            captureColumnFound = true;
            if (reader.GetInt32(3) != 1)
            {
                throw new InvalidOperationException(
                    "Migration002 requires the version 1 sentence_examples schema.");
            }
        }

        if (!captureColumnFound)
        {
            throw new InvalidOperationException(
                "Migration002 requires the version 1 sentence_examples schema.");
        }
    }

    private static async Task<long> CountRowsAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string tableName,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"SELECT COUNT(*) FROM {tableName};";
        return (long)(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) ?? 0L);
    }

    private static async Task EnsureRowCountAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string tableName,
        long expected,
        CancellationToken cancellationToken)
    {
        var actual = await CountRowsAsync(
            connection,
            transaction,
            tableName,
            cancellationToken).ConfigureAwait(false);
        if (actual != expected)
        {
            throw new InvalidOperationException(
                $"Migration002 row-count validation failed for {tableName}.");
        }
    }

    private static async Task EnsureForeignKeysValidAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "PRAGMA foreign_key_check;";
        await using var reader = await command
            .ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);
        if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException(
                "Migration002 foreign-key validation failed.");
        }
    }

    private static async Task ExecuteAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private const string RebuildSql = """
        CREATE TABLE entry_examples_m002_backup (
            entry_id TEXT NOT NULL,
            example_id TEXT NOT NULL,
            is_primary INTEGER NOT NULL,
            sort_order INTEGER NOT NULL,
            PRIMARY KEY (entry_id, example_id)
        );

        INSERT INTO entry_examples_m002_backup
            (entry_id, example_id, is_primary, sort_order)
        SELECT entry_id, example_id, is_primary, sort_order
        FROM entry_examples;

        DROP TABLE entry_examples;

        CREATE TABLE sentence_examples_m002_new (
            id TEXT PRIMARY KEY,
            capture_id TEXT,
            ocr_region_id TEXT,
            sentence_text TEXT NOT NULL,
            normalized_sentence TEXT NOT NULL,
            target_start INTEGER NOT NULL,
            target_length INTEGER NOT NULL,
            screenshot_crop_path TEXT NOT NULL DEFAULT '',
            game_title TEXT,
            created_at_utc TEXT NOT NULL,
            CHECK (ocr_region_id IS NULL OR capture_id IS NOT NULL),
            FOREIGN KEY (capture_id) REFERENCES captures(id) ON DELETE RESTRICT,
            FOREIGN KEY (ocr_region_id) REFERENCES ocr_regions(id) ON DELETE SET NULL
        );

        INSERT INTO sentence_examples_m002_new
            (id, capture_id, ocr_region_id, sentence_text, normalized_sentence,
             target_start, target_length, screenshot_crop_path, game_title, created_at_utc)
        SELECT id, capture_id, ocr_region_id, sentence_text, normalized_sentence,
               target_start, target_length, screenshot_crop_path, game_title, created_at_utc
        FROM sentence_examples;

        DROP TABLE sentence_examples;
        ALTER TABLE sentence_examples_m002_new RENAME TO sentence_examples;

        CREATE TABLE entry_examples (
            entry_id TEXT NOT NULL,
            example_id TEXT NOT NULL,
            is_primary INTEGER NOT NULL DEFAULT 0,
            sort_order INTEGER NOT NULL DEFAULT 0,
            PRIMARY KEY (entry_id, example_id),
            FOREIGN KEY (entry_id) REFERENCES vocabulary_entries(id) ON DELETE CASCADE,
            FOREIGN KEY (example_id) REFERENCES sentence_examples(id) ON DELETE CASCADE
        );

        INSERT INTO entry_examples (entry_id, example_id, is_primary, sort_order)
        SELECT entry_id, example_id, is_primary, sort_order
        FROM entry_examples_m002_backup;

        DROP TABLE entry_examples_m002_backup;

        CREATE INDEX ix_vocabulary_entries_archive_updated
        ON vocabulary_entries(is_archived, updated_at_utc DESC, id ASC);

        CREATE INDEX ix_vocabulary_entries_archive_type_updated
        ON vocabulary_entries(is_archived, entry_type, updated_at_utc DESC, id ASC);

        CREATE INDEX ix_entry_examples_entry_sort
        ON entry_examples(entry_id, sort_order, example_id);

        CREATE INDEX ix_entry_examples_example_entry
        ON entry_examples(example_id, entry_id);

        CREATE INDEX ix_entry_tags_tag_entry
        ON entry_tags(tag_id, entry_id);

        CREATE INDEX ix_sentence_examples_game_created
        ON sentence_examples(game_title COLLATE NOCASE, created_at_utc DESC, id ASC);
        """;
}
