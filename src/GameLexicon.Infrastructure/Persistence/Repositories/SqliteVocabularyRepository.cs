using System.Globalization;
using GameLexicon.Domain.Entries;
using Microsoft.Data.Sqlite;

namespace GameLexicon.Infrastructure.Persistence.Repositories;

public sealed partial class SqliteVocabularyRepository
{
    private const string TimestampFormat = "yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'";
    private readonly SqliteConnectionFactory _connectionFactory;

    public SqliteVocabularyRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory
            ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task SaveAsync(
        VocabularyEntry entry,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entry);
        cancellationToken.ThrowIfCancellationRequested();
        await using var connection = await _connectionFactory
            .OpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);
        await ExecuteTransactionAsync(connection, cancellationToken, async transaction =>
        {
            var storedTimes = await ReadStoredTimesAsync(
                connection,
                transaction,
                entry.Id,
                cancellationToken).ConfigureAwait(false);

            if (storedTimes is null)
            {
                await InsertAsync(connection, transaction, entry, cancellationToken).ConfigureAwait(false);
                return;
            }

            if (entry.CreatedAt != storedTimes.Value.CreatedAt)
            {
                throw new InvalidOperationException("Stored creation time does not match the entry.");
            }

            if (entry.UpdatedAt < storedTimes.Value.UpdatedAt)
            {
                throw new InvalidOperationException("Entry update time must not move backwards.");
            }

            await UpdateAsync(connection, transaction, entry, cancellationToken).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    private static async Task<(DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt)?> ReadStoredTimesAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Guid entryId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT created_at_utc, updated_at_utc
            FROM vocabulary_entries
            WHERE id = $id;
            """;
        AddGuid(command, "$id", entryId);
        await using var reader = await command
            .ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        try
        {
            var createdAt = ParseUtc(reader.GetString(0), "creation timestamp");
            var updatedAt = ParseUtc(reader.GetString(1), "update timestamp");
            if (updatedAt < createdAt)
            {
                throw new InvalidDataException("Stored entry timestamps are inconsistent.");
            }

            return (createdAt, updatedAt);
        }
        catch (InvalidDataException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new InvalidDataException("Stored entry timestamps are invalid.");
        }
    }

    private static async Task InsertAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        VocabularyEntry entry,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO vocabulary_entries
                (id, headword, normalized_headword, entry_type, part_of_speech,
                 phonetic, definition_english, translation_chinese, notes,
                 is_archived, created_at_utc, updated_at_utc)
            VALUES
                ($id, $headword, $normalizedHeadword, $entryType, $partOfSpeech,
                 $phonetic, $definitionEnglish, $translationChinese, $notes,
                 $isArchived, $createdAtUtc, $updatedAtUtc);
            """;
        AddEntryParameters(command, entry, includeCreatedAt: true);
        var affected = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        if (affected != 1)
        {
            throw new InvalidDataException("Vocabulary entry insert affected an unexpected row count.");
        }
    }

    private static async Task UpdateAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        VocabularyEntry entry,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            UPDATE vocabulary_entries
            SET headword = $headword,
                normalized_headword = $normalizedHeadword,
                entry_type = $entryType,
                part_of_speech = $partOfSpeech,
                phonetic = $phonetic,
                definition_english = $definitionEnglish,
                translation_chinese = $translationChinese,
                notes = $notes,
                is_archived = $isArchived,
                updated_at_utc = $updatedAtUtc
            WHERE id = $id;
            """;
        AddEntryParameters(command, entry, includeCreatedAt: false);
        var affected = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        if (affected != 1)
        {
            throw new InvalidDataException("Vocabulary entry update affected an unexpected row count.");
        }
    }

    private static void AddEntryParameters(
        SqliteCommand command,
        VocabularyEntry entry,
        bool includeCreatedAt)
    {
        AddGuid(command, "$id", entry.Id);
        AddText(command, "$headword", entry.Headword);
        AddText(command, "$normalizedHeadword", entry.NormalizedHeadword);
        AddInteger(command, "$entryType", MapEntryType(entry.EntryType));
        AddNullableText(command, "$partOfSpeech", entry.PartOfSpeech);
        AddNullableText(command, "$phonetic", entry.Phonetic);
        AddNullableText(command, "$definitionEnglish", entry.DefinitionEnglish);
        AddNullableText(command, "$translationChinese", entry.TranslationChinese);
        AddNullableText(command, "$notes", entry.Notes);
        AddInteger(command, "$isArchived", entry.IsArchived ? 1 : 0);
        if (includeCreatedAt)
        {
            AddText(command, "$createdAtUtc", FormatUtc(entry.CreatedAt));
        }

        AddText(command, "$updatedAtUtc", FormatUtc(entry.UpdatedAt));
    }

    private static int MapEntryType(EntryType entryType) => entryType switch
    {
        EntryType.Word => 0,
        EntryType.Phrase => 1,
        EntryType.Expression => 2,
        EntryType.SentencePattern => 3,
        _ => throw new ArgumentOutOfRangeException(nameof(entryType), "Entry type must be defined.")
    };

    private static DateTimeOffset ParseUtc(string value, string fieldName)
    {
        if (!DateTimeOffset.TryParseExact(
                value,
                TimestampFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var result) ||
            result.Offset != TimeSpan.Zero)
        {
            throw new InvalidDataException($"Stored {fieldName} is invalid.");
        }

        return result;
    }

    private static string FormatUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Timestamp must use UTC.", nameof(value));
        }

        return value.ToString(TimestampFormat, CultureInfo.InvariantCulture);
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

    private static string FormatGuid(Guid value) => value.ToString("D").ToLowerInvariant();

    private static void AddGuid(SqliteCommand command, string name, Guid value) =>
        AddText(command, name, FormatGuid(value));

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
