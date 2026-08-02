using System.Globalization;
using GameLexicon.Application.Abstractions.Persistence;
using GameLexicon.Application.Entries.Queries;
using GameLexicon.Domain.Entries;
using Microsoft.Data.Sqlite;

namespace GameLexicon.Infrastructure.Persistence.Repositories;

public sealed class SqliteSentenceExampleRepository : ISentenceExampleRepository
{
    private const string TimestampFormat = "yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'";
    private readonly SqliteConnectionFactory _connectionFactory;

    public SqliteSentenceExampleRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory
            ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<SentenceExample?> GetByIdAsync(
        Guid exampleId,
        CancellationToken cancellationToken)
    {
        EnsureIdentifier(exampleId, nameof(exampleId));
        cancellationToken.ThrowIfCancellationRequested();
        await using var connection = await _connectionFactory
            .OpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, capture_id, ocr_region_id, sentence_text, normalized_sentence,
                   target_start, target_length, screenshot_crop_path, game_title,
                   created_at_utc
            FROM sentence_examples
            WHERE id = $exampleId;
            """;
        AddGuid(command, "$exampleId", exampleId);
        await using var reader = await command
            .ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        return MapExample(reader, 0);
    }

    public async Task<IReadOnlyList<SentenceExampleDetails>> GetForEntryAsync(
        Guid entryId,
        CancellationToken cancellationToken)
    {
        EnsureIdentifier(entryId, nameof(entryId));
        cancellationToken.ThrowIfCancellationRequested();
        await using var connection = await _connectionFactory
            .OpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT ee.entry_id, ee.example_id, ee.is_primary, ee.sort_order,
                   se.id, se.capture_id, se.ocr_region_id, se.sentence_text,
                   se.normalized_sentence, se.target_start, se.target_length,
                   se.screenshot_crop_path, se.game_title, se.created_at_utc
            FROM entry_examples AS ee
            INNER JOIN sentence_examples AS se ON se.id = ee.example_id
            WHERE ee.entry_id = $entryId
            ORDER BY ee.sort_order ASC, ee.example_id ASC;
            """;
        AddGuid(command, "$entryId", entryId);
        await using var reader = await command
            .ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);
        var details = new List<SentenceExampleDetails>();
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var link = MapLink(reader, 0);
            var example = MapExample(reader, 4);
            details.Add(new SentenceExampleDetails(example, link));
        }

        return Array.AsReadOnly(details.ToArray());
    }

    public async Task SaveAsync(
        SentenceExample example,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(example);
        cancellationToken.ThrowIfCancellationRequested();
        await using var connection = await _connectionFactory
            .OpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);
        await ExecuteTransactionAsync(connection, cancellationToken, async transaction =>
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO sentence_examples
                    (id, capture_id, ocr_region_id, sentence_text, normalized_sentence,
                     target_start, target_length, screenshot_crop_path, game_title,
                     created_at_utc)
                VALUES
                    ($id, $captureId, $ocrRegionId, $sentenceText, $normalizedSentence,
                     $targetStart, $targetLength, $screenshotCropPath, $gameTitle,
                     $createdAtUtc)
                ON CONFLICT(id) DO UPDATE SET
                    capture_id = excluded.capture_id,
                    ocr_region_id = excluded.ocr_region_id,
                    sentence_text = excluded.sentence_text,
                    normalized_sentence = excluded.normalized_sentence,
                    target_start = excluded.target_start,
                    target_length = excluded.target_length,
                    screenshot_crop_path = excluded.screenshot_crop_path,
                    game_title = excluded.game_title,
                    created_at_utc = excluded.created_at_utc;
                """;
            AddGuid(command, "$id", example.Id);
            AddNullableGuid(command, "$captureId", example.CaptureId);
            AddNullableGuid(command, "$ocrRegionId", example.OcrRegionId);
            AddText(command, "$sentenceText", example.SentenceText);
            AddText(command, "$normalizedSentence", example.NormalizedSentence);
            AddInteger(command, "$targetStart", example.TargetStart);
            AddInteger(command, "$targetLength", example.TargetLength);
            AddText(command, "$screenshotCropPath", example.ScreenshotCropPath ?? string.Empty);
            AddNullableText(command, "$gameTitle", example.GameTitle);
            AddText(command, "$createdAtUtc", FormatUtc(example.CreatedAt));
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    public async Task SaveLinkAsync(
        EntryExampleLink link,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(link);
        cancellationToken.ThrowIfCancellationRequested();
        await using var connection = await _connectionFactory
            .OpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);
        await ExecuteTransactionAsync(connection, cancellationToken, async transaction =>
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO entry_examples
                    (entry_id, example_id, is_primary, sort_order)
                VALUES ($entryId, $exampleId, $isPrimary, $sortOrder)
                ON CONFLICT(entry_id, example_id) DO UPDATE SET
                    is_primary = excluded.is_primary,
                    sort_order = excluded.sort_order;
                """;
            AddGuid(command, "$entryId", link.EntryId);
            AddGuid(command, "$exampleId", link.ExampleId);
            AddInteger(command, "$isPrimary", link.IsPrimary ? 1 : 0);
            AddInteger(command, "$sortOrder", link.SortOrder);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    public async Task SetPrimaryAsync(
        Guid entryId,
        Guid exampleId,
        CancellationToken cancellationToken)
    {
        EnsureIdentifier(entryId, nameof(entryId));
        EnsureIdentifier(exampleId, nameof(exampleId));
        cancellationToken.ThrowIfCancellationRequested();
        await using var connection = await _connectionFactory
            .OpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);
        await ExecuteTransactionAsync(connection, cancellationToken, async transaction =>
        {
            await using (var existsCommand = connection.CreateCommand())
            {
                existsCommand.Transaction = transaction;
                existsCommand.CommandText = """
                    SELECT 1
                    FROM entry_examples
                    WHERE entry_id = $entryId AND example_id = $exampleId;
                    """;
                AddGuid(existsCommand, "$entryId", entryId);
                AddGuid(existsCommand, "$exampleId", exampleId);
                if (await existsCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) is null)
                {
                    throw new KeyNotFoundException("The requested entry-example link does not exist.");
                }
            }

            await using (var clearCommand = connection.CreateCommand())
            {
                clearCommand.Transaction = transaction;
                clearCommand.CommandText = """
                    UPDATE entry_examples
                    SET is_primary = 0
                    WHERE entry_id = $entryId;
                    """;
                AddGuid(clearCommand, "$entryId", entryId);
                await clearCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            await using var setCommand = connection.CreateCommand();
            setCommand.Transaction = transaction;
            setCommand.CommandText = """
                UPDATE entry_examples
                SET is_primary = 1
                WHERE entry_id = $entryId AND example_id = $exampleId;
                """;
            AddGuid(setCommand, "$entryId", entryId);
            AddGuid(setCommand, "$exampleId", exampleId);
            var updated = await setCommand
                .ExecuteNonQueryAsync(cancellationToken)
                .ConfigureAwait(false);
            if (updated != 1)
            {
                throw new InvalidDataException("Primary example update affected an unexpected row count.");
            }
        }).ConfigureAwait(false);
    }

    public async Task RemoveLinkAsync(
        Guid entryId,
        Guid exampleId,
        CancellationToken cancellationToken)
    {
        EnsureIdentifier(entryId, nameof(entryId));
        EnsureIdentifier(exampleId, nameof(exampleId));
        cancellationToken.ThrowIfCancellationRequested();
        await using var connection = await _connectionFactory
            .OpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);
        await ExecuteTransactionAsync(connection, cancellationToken, async transaction =>
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                DELETE FROM entry_examples
                WHERE entry_id = $entryId AND example_id = $exampleId;
                """;
            AddGuid(command, "$entryId", entryId);
            AddGuid(command, "$exampleId", exampleId);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    private static SentenceExample MapExample(SqliteDataReader reader, int offset)
    {
        try
        {
            return new SentenceExample(
                ParseGuid(reader.GetString(offset), "example identifier"),
                ReadNullableGuid(reader, offset + 1, "capture identifier"),
                ReadNullableGuid(reader, offset + 2, "OCR region identifier"),
                reader.GetString(offset + 3),
                reader.GetString(offset + 4),
                reader.GetInt32(offset + 5),
                reader.GetInt32(offset + 6),
                reader.GetString(offset + 7),
                reader.IsDBNull(offset + 8) ? null : reader.GetString(offset + 8),
                ParseUtc(reader.GetString(offset + 9)));
        }
        catch (InvalidDataException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new InvalidDataException("Stored sentence example is invalid.");
        }
    }

    private static EntryExampleLink MapLink(SqliteDataReader reader, int offset)
    {
        try
        {
            var primaryValue = reader.GetInt32(offset + 2);
            if (primaryValue is not 0 and not 1)
            {
                throw new InvalidDataException("Stored primary state is invalid.");
            }

            return new EntryExampleLink(
                ParseGuid(reader.GetString(offset), "entry identifier"),
                ParseGuid(reader.GetString(offset + 1), "example identifier"),
                primaryValue == 1,
                reader.GetInt32(offset + 3));
        }
        catch (InvalidDataException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new InvalidDataException("Stored entry-example link is invalid.");
        }
    }

    private static Guid ParseGuid(string value, string fieldName)
    {
        if (!Guid.TryParseExact(value, "D", out var result) || result == Guid.Empty)
        {
            throw new InvalidDataException($"Stored {fieldName} is invalid.");
        }

        return result;
    }

    private static Guid? ReadNullableGuid(SqliteDataReader reader, int ordinal, string fieldName) =>
        reader.IsDBNull(ordinal) ? null : ParseGuid(reader.GetString(ordinal), fieldName);

    private static DateTimeOffset ParseUtc(string value)
    {
        if (!DateTimeOffset.TryParseExact(
                value,
                TimestampFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var result) ||
            result.Offset != TimeSpan.Zero)
        {
            throw new InvalidDataException("Stored creation timestamp is invalid.");
        }

        return result;
    }

    private static string FormatUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Creation timestamp must use UTC.", nameof(value));
        }

        return value.UtcDateTime.ToString(TimestampFormat, CultureInfo.InvariantCulture);
    }

    private static async Task ExecuteTransactionAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken,
        Func<SqliteTransaction, Task> operation)
    {
        await using var transaction = (SqliteTransaction)await connection
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);
        try
        {
            await operation(transaction).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            try
            {
                await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch
            {
                // Preserve the operation failure rather than replacing it with rollback failure.
            }

            throw;
        }
    }

    private static void EnsureIdentifier(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Identifier must not be empty.", parameterName);
        }
    }

    private static string FormatGuid(Guid value) => value.ToString("D").ToLowerInvariant();

    private static void AddGuid(SqliteCommand command, string name, Guid value) =>
        AddText(command, name, FormatGuid(value));

    private static void AddNullableGuid(SqliteCommand command, string name, Guid? value)
    {
        var parameter = command.Parameters.Add(name, SqliteType.Text);
        parameter.Value = value.HasValue ? FormatGuid(value.Value) : DBNull.Value;
    }

    private static void AddText(SqliteCommand command, string name, string value)
    {
        var parameter = command.Parameters.Add(name, SqliteType.Text);
        parameter.Value = value;
    }

    private static void AddNullableText(SqliteCommand command, string name, string? value)
    {
        var parameter = command.Parameters.Add(name, SqliteType.Text);
        parameter.Value = value ?? (object)DBNull.Value;
    }

    private static void AddInteger(SqliteCommand command, string name, int value)
    {
        var parameter = command.Parameters.Add(name, SqliteType.Integer);
        parameter.Value = value;
    }
}
