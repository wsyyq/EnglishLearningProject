using GameLexicon.Application.Abstractions.Persistence;
using GameLexicon.Domain.Entries;
using Microsoft.Data.Sqlite;

namespace GameLexicon.Infrastructure.Persistence.Repositories;

public sealed class SqliteTagRepository : ITagRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public SqliteTagRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory
            ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<Tag?> FindByNormalizedNameAsync(
        string normalizedName,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(normalizedName);
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            throw new ArgumentException("Normalized name must not be empty or whitespace.", nameof(normalizedName));
        }

        cancellationToken.ThrowIfCancellationRequested();
        await using var connection = await _connectionFactory
            .OpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, name, normalized_name
            FROM tags
            WHERE normalized_name = $normalizedName;
            """;
        AddText(command, "$normalizedName", normalizedName);
        await using var reader = await command
            .ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);
        return await reader.ReadAsync(cancellationToken).ConfigureAwait(false)
            ? MapTag(reader)
            : null;
    }

    public async Task<Tag> GetOrCreateAsync(
        Tag candidate,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        cancellationToken.ThrowIfCancellationRequested();
        await using var connection = await _connectionFactory
            .OpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);

        Tag? persisted = null;
        await ExecuteTransactionAsync(connection, cancellationToken, async transaction =>
        {
            await using (var insertCommand = connection.CreateCommand())
            {
                insertCommand.Transaction = transaction;
                insertCommand.CommandText = """
                    INSERT INTO tags (id, name, normalized_name)
                    VALUES ($id, $name, $normalizedName)
                    ON CONFLICT(normalized_name) DO NOTHING;
                    """;
                AddGuid(insertCommand, "$id", candidate.Id);
                AddText(insertCommand, "$name", candidate.Name);
                AddText(insertCommand, "$normalizedName", candidate.NormalizedName);
                await insertCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            await using var selectCommand = connection.CreateCommand();
            selectCommand.Transaction = transaction;
            selectCommand.CommandText = """
                SELECT id, name, normalized_name
                FROM tags
                WHERE normalized_name = $normalizedName;
                """;
            AddText(selectCommand, "$normalizedName", candidate.NormalizedName);
            await using var reader = await selectCommand
                .ExecuteReaderAsync(cancellationToken)
                .ConfigureAwait(false);
            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                throw new InvalidDataException("Persisted tag was not found after creation.");
            }

            persisted = MapTag(reader);
        }).ConfigureAwait(false);

        return persisted
            ?? throw new InvalidDataException("Persisted tag result is unavailable.");
    }

    public async Task<IReadOnlyList<Tag>> GetForEntryAsync(
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
            SELECT tags.id, tags.name, tags.normalized_name
            FROM entry_tags
            INNER JOIN tags ON tags.id = entry_tags.tag_id
            WHERE entry_tags.entry_id = $entryId
            ORDER BY tags.normalized_name ASC, tags.id ASC;
            """;
        AddGuid(command, "$entryId", entryId);
        await using var reader = await command
            .ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);
        var tags = new List<Tag>();
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            tags.Add(MapTag(reader));
        }

        return Array.AsReadOnly(tags.ToArray());
    }

    public async Task SetForEntryAsync(
        Guid entryId,
        IReadOnlyList<Guid> tagIds,
        CancellationToken cancellationToken)
    {
        EnsureIdentifier(entryId, nameof(entryId));
        ArgumentNullException.ThrowIfNull(tagIds);
        var snapshot = tagIds.ToArray();
        var uniqueIds = new HashSet<Guid>();
        foreach (var tagId in snapshot)
        {
            EnsureIdentifier(tagId, nameof(tagIds));
            if (!uniqueIds.Add(tagId))
            {
                throw new ArgumentException("Tag identifiers must be distinct.", nameof(tagIds));
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        await using var connection = await _connectionFactory
            .OpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);
        await ExecuteTransactionAsync(connection, cancellationToken, async transaction =>
        {
            await EnsureRowExistsAsync(
                connection,
                transaction,
                "SELECT 1 FROM vocabulary_entries WHERE id = $id;",
                entryId,
                "The requested vocabulary entry does not exist.",
                cancellationToken).ConfigureAwait(false);

            foreach (var tagId in snapshot)
            {
                await EnsureRowExistsAsync(
                    connection,
                    transaction,
                    "SELECT 1 FROM tags WHERE id = $id;",
                    tagId,
                    "A requested tag does not exist.",
                    cancellationToken).ConfigureAwait(false);
            }

            await using (var deleteCommand = connection.CreateCommand())
            {
                deleteCommand.Transaction = transaction;
                deleteCommand.CommandText = "DELETE FROM entry_tags WHERE entry_id = $entryId;";
                AddGuid(deleteCommand, "$entryId", entryId);
                await deleteCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            foreach (var tagId in snapshot)
            {
                await using var insertCommand = connection.CreateCommand();
                insertCommand.Transaction = transaction;
                insertCommand.CommandText = """
                    INSERT INTO entry_tags (entry_id, tag_id)
                    VALUES ($entryId, $tagId);
                    """;
                AddGuid(insertCommand, "$entryId", entryId);
                AddGuid(insertCommand, "$tagId", tagId);
                await insertCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
        }).ConfigureAwait(false);
    }

    private static Tag MapTag(SqliteDataReader reader)
    {
        try
        {
            var id = ParseGuid(reader.GetString(0));
            var name = reader.GetString(1);
            var normalizedName = reader.GetString(2);
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(normalizedName))
            {
                throw new InvalidDataException("Stored tag text is invalid.");
            }

            return new Tag(id, name, normalizedName);
        }
        catch (InvalidDataException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new InvalidDataException("Stored tag is invalid.");
        }
    }

    private static Guid ParseGuid(string value)
    {
        if (!Guid.TryParseExact(value, "D", out var result) || result == Guid.Empty)
        {
            throw new InvalidDataException("Stored tag identifier is invalid.");
        }

        return result;
    }

    private static async Task EnsureRowExistsAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string sql,
        Guid id,
        string message,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        AddGuid(command, "$id", id);
        if (await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) is null)
        {
            throw new KeyNotFoundException(message);
        }
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

    private static void AddText(SqliteCommand command, string name, string value)
    {
        var parameter = command.Parameters.Add(name, SqliteType.Text);
        parameter.Value = value;
    }
}
