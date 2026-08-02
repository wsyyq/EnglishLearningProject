using GameLexicon.Application.Abstractions.Persistence;
using GameLexicon.Application.Entries.Queries;
using GameLexicon.Domain.Entries;
using Microsoft.Data.Sqlite;

namespace GameLexicon.Infrastructure.Persistence.Repositories;

public sealed partial class SqliteVocabularyRepository : IVocabularyRepository
{
    public async Task<VocabularyEntry?> FindByNormalizedHeadwordAsync(
        string normalizedHeadword,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(normalizedHeadword);
        if (string.IsNullOrWhiteSpace(normalizedHeadword))
        {
            throw new ArgumentException(
                "Normalized headword must not be empty or whitespace.",
                nameof(normalizedHeadword));
        }

        cancellationToken.ThrowIfCancellationRequested();
        await using var connection = await _connectionFactory
            .OpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, headword, normalized_headword, entry_type, part_of_speech,
                   phonetic, definition_english, translation_chinese, notes,
                   is_archived, created_at_utc, updated_at_utc
            FROM vocabulary_entries
            WHERE normalized_headword = $normalizedHeadword
              AND is_archived = 0;
            """;
        AddText(command, "$normalizedHeadword", normalizedHeadword);
        await using var reader = await command
            .ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        var result = MapVocabularyEntry(reader);
        if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidDataException("Multiple active entries share one normalized headword.");
        }

        return result;
    }

    public async Task<VocabularyEntryDetails?> GetDetailsAsync(
        Guid entryId,
        CancellationToken cancellationToken)
    {
        EnsureIdentifier(entryId, nameof(entryId));
        cancellationToken.ThrowIfCancellationRequested();
        await using var connection = await _connectionFactory
            .OpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);

        return await ExecuteReadTransactionAsync(
            connection,
            cancellationToken,
            async transaction =>
            {
                var entry = await ReadEntryAsync(
                    connection,
                    transaction,
                    entryId,
                    cancellationToken).ConfigureAwait(false);
                if (entry is null)
                {
                    return null;
                }

                var examples = await ReadExamplesAsync(
                    connection,
                    transaction,
                    entryId,
                    cancellationToken).ConfigureAwait(false);
                var tags = await ReadTagsAsync(
                    connection,
                    transaction,
                    entryId,
                    cancellationToken).ConfigureAwait(false);

                try
                {
                    return new VocabularyEntryDetails(entry, examples, tags);
                }
                catch (Exception exception) when (exception is not OperationCanceledException and not InvalidDataException)
                {
                    throw new InvalidDataException("Stored vocabulary entry details are invalid.", exception);
                }
            }).ConfigureAwait(false);
    }

    public async Task<PagedResult<VocabularyEntrySummary>> SearchAsync(
        VocabularySearchQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();
        var offset = checked(((long)query.PageNumber - 1L) * query.PageSize);
        await using var connection = await _connectionFactory
            .OpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);

        return await ExecuteReadTransactionAsync(
            connection,
            cancellationToken,
            async transaction =>
            {
                var totalCount = await CountMatchesAsync(
                    connection,
                    transaction,
                    query,
                    cancellationToken).ConfigureAwait(false);
                var entries = await ReadPageAsync(
                    connection,
                    transaction,
                    query,
                    offset,
                    cancellationToken).ConfigureAwait(false);
                if (entries.Count == 0)
                {
                    return new PagedResult<VocabularyEntrySummary>(
                        Array.Empty<VocabularyEntrySummary>(),
                        query.PageNumber,
                        query.PageSize,
                        totalCount);
                }

                var entryIds = entries.Select(static entry => entry.Id).ToArray();
                var primaryByEntry = await ReadPrimaryExamplesAsync(
                    connection,
                    transaction,
                    entryIds,
                    cancellationToken).ConfigureAwait(false);
                var tagsByEntry = await ReadTagsForPageAsync(
                    connection,
                    transaction,
                    entryIds,
                    cancellationToken).ConfigureAwait(false);

                var summaries = entries.Select(entry =>
                {
                    primaryByEntry.TryGetValue(entry.Id, out var primary);
                    tagsByEntry.TryGetValue(entry.Id, out var tags);
                    return new VocabularyEntrySummary(
                        entry.Id,
                        entry.Headword,
                        entry.EntryType,
                        entry.TranslationChinese,
                        primary?.Text,
                        primary?.GameTitle,
                        tags ?? Array.Empty<TagSummary>(),
                        entry.IsArchived,
                        entry.CreatedAt,
                        entry.UpdatedAt);
                }).ToArray();

                return new PagedResult<VocabularyEntrySummary>(
                    summaries,
                    query.PageNumber,
                    query.PageSize,
                    totalCount);
            }).ConfigureAwait(false);
    }

    private static async Task<VocabularyEntry?> ReadEntryAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Guid entryId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT id, headword, normalized_headword, entry_type, part_of_speech,
                   phonetic, definition_english, translation_chinese, notes,
                   is_archived, created_at_utc, updated_at_utc
            FROM vocabulary_entries
            WHERE id = $entryId;
            """;
        AddGuid(command, "$entryId", entryId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        return await reader.ReadAsync(cancellationToken).ConfigureAwait(false)
            ? MapVocabularyEntry(reader)
            : null;
    }

    private static async Task<IReadOnlyList<SentenceExampleDetails>> ReadExamplesAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Guid entryId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
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
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        var results = new List<SentenceExampleDetails>();
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var link = new EntryExampleLink(
                    ParseStoredGuid(reader.GetString(0), "entry identifier"),
                    ParseStoredGuid(reader.GetString(1), "example identifier"),
                    ReadStoredBoolean(reader, 2, "primary state"),
                    reader.GetInt32(3));
                var example = MapSentenceExample(reader, 4);
                results.Add(new SentenceExampleDetails(example, link));
            }
            catch (Exception exception) when (exception is not OperationCanceledException and not InvalidDataException)
            {
                throw new InvalidDataException("Stored entry example is invalid.", exception);
            }
        }

        return Array.AsReadOnly(results.ToArray());
    }

    private static async Task<IReadOnlyList<TagSummary>> ReadTagsAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Guid entryId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT tags.id, tags.name, tags.normalized_name
            FROM entry_tags
            INNER JOIN tags ON tags.id = entry_tags.tag_id
            WHERE entry_tags.entry_id = $entryId
            ORDER BY tags.normalized_name ASC, tags.id ASC;
            """;
        AddGuid(command, "$entryId", entryId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        var results = new List<TagSummary>();
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            results.Add(MapTagSummary(reader, 0));
        }

        return Array.AsReadOnly(results.ToArray());
    }

    private static async Task<long> CountMatchesAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        VocabularySearchQuery query,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"SELECT COUNT(*) FROM vocabulary_entries AS ve{BuildWhereClause(command, query)};";
        var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return Convert.ToInt64(result, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static async Task<IReadOnlyList<VocabularyEntry>> ReadPageAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        VocabularySearchQuery query,
        long offset,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""
            SELECT ve.id, ve.headword, ve.normalized_headword, ve.entry_type,
                   ve.part_of_speech, ve.phonetic, ve.definition_english,
                   ve.translation_chinese, ve.notes, ve.is_archived,
                   ve.created_at_utc, ve.updated_at_utc
            FROM vocabulary_entries AS ve{BuildWhereClause(command, query)}
            {GetOrderByClause(query.SortOrder)}
            LIMIT $pageSize OFFSET $offset;
            """;
        AddInteger(command, "$pageSize", query.PageSize);
        var offsetParameter = command.Parameters.Add("$offset", SqliteType.Integer);
        offsetParameter.Value = offset;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        var results = new List<VocabularyEntry>();
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            results.Add(MapVocabularyEntry(reader));
        }

        return Array.AsReadOnly(results.ToArray());
    }

    private static async Task<IReadOnlyDictionary<Guid, PrimaryExample>> ReadPrimaryExamplesAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        IReadOnlyList<Guid> entryIds,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        var inClause = AddEntryIdParameters(command, entryIds);
        command.CommandText = $"""
            SELECT ee.entry_id, ee.is_primary, se.sentence_text, se.game_title
            FROM entry_examples AS ee
            INNER JOIN sentence_examples AS se ON se.id = ee.example_id
            WHERE ee.entry_id IN ({inClause})
              AND ee.is_primary <> 0
            ORDER BY ee.entry_id ASC, ee.example_id ASC;
            """;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        var results = new Dictionary<Guid, PrimaryExample>();
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var entryId = ParseStoredGuid(reader.GetString(0), "entry identifier");
            if (!ReadStoredBoolean(reader, 1, "primary state"))
            {
                continue;
            }

            var text = ReadRequiredText(reader, 2, "sentence text");
            var primary = new PrimaryExample(text, ReadNullableText(reader, 3));
            if (!results.TryAdd(entryId, primary))
            {
                throw new InvalidDataException("A vocabulary entry has multiple primary examples.");
            }
        }

        return results;
    }

    private static async Task<IReadOnlyDictionary<Guid, IReadOnlyList<TagSummary>>> ReadTagsForPageAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        IReadOnlyList<Guid> entryIds,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        var inClause = AddEntryIdParameters(command, entryIds);
        command.CommandText = $"""
            SELECT et.entry_id, tags.id, tags.name, tags.normalized_name
            FROM entry_tags AS et
            INNER JOIN tags ON tags.id = et.tag_id
            WHERE et.entry_id IN ({inClause})
            ORDER BY et.entry_id ASC, tags.normalized_name ASC, tags.id ASC;
            """;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        var lists = new Dictionary<Guid, List<TagSummary>>();
        var seenIds = new Dictionary<Guid, HashSet<Guid>>();
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var entryId = ParseStoredGuid(reader.GetString(0), "entry identifier");
            var tag = MapTagSummary(reader, 1);
            if (!seenIds.TryGetValue(entryId, out var seen))
            {
                seen = new HashSet<Guid>();
                seenIds.Add(entryId, seen);
                lists.Add(entryId, []);
            }

            if (!seen.Add(tag.Id))
            {
                throw new InvalidDataException("A vocabulary entry has duplicate tag links.");
            }

            lists[entryId].Add(tag);
        }

        return lists.ToDictionary(
            static pair => pair.Key,
            static pair => (IReadOnlyList<TagSummary>)Array.AsReadOnly(pair.Value.ToArray()));
    }

    private static string BuildWhereClause(SqliteCommand command, VocabularySearchQuery query)
    {
        var conditions = new List<string>();
        switch (query.ArchiveFilter)
        {
            case VocabularyArchiveFilter.ActiveOnly:
                conditions.Add("ve.is_archived = 0");
                break;
            case VocabularyArchiveFilter.ArchivedOnly:
                conditions.Add("ve.is_archived = 1");
                break;
            case VocabularyArchiveFilter.All:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(query), "Archive filter must be defined.");
        }

        if (query.EntryType.HasValue)
        {
            conditions.Add("ve.entry_type = $entryType");
            AddInteger(command, "$entryType", MapEntryType(query.EntryType.Value));
        }

        if (query.SearchText is not null)
        {
            conditions.Add("""
                (ve.headword LIKE $searchText ESCAPE '\'
                 OR ve.normalized_headword LIKE $searchText ESCAPE '\'
                 OR ve.part_of_speech LIKE $searchText ESCAPE '\'
                 OR ve.phonetic LIKE $searchText ESCAPE '\'
                 OR ve.definition_english LIKE $searchText ESCAPE '\'
                 OR ve.translation_chinese LIKE $searchText ESCAPE '\'
                 OR ve.notes LIKE $searchText ESCAPE '\')
                """);
            AddText(command, "$searchText", $"%{EscapeLikeLiteral(query.SearchText)}%");
        }

        if (query.GameTitle is not null)
        {
            conditions.Add("""
                EXISTS (
                    SELECT 1
                    FROM entry_examples AS game_links
                    INNER JOIN sentence_examples AS game_examples
                        ON game_examples.id = game_links.example_id
                    WHERE game_links.entry_id = ve.id
                      AND game_examples.game_title = $gameTitle COLLATE NOCASE)
                """);
            AddText(command, "$gameTitle", query.GameTitle);
        }

        for (var index = 0; index < query.TagIds.Count; index++)
        {
            var parameterName = $"$tagId{index}";
            conditions.Add($"EXISTS (SELECT 1 FROM entry_tags AS tag_links_{index} WHERE tag_links_{index}.entry_id = ve.id AND tag_links_{index}.tag_id = {parameterName})");
            AddGuid(command, parameterName, query.TagIds[index]);
        }

        return conditions.Count == 0
            ? string.Empty
            : $" WHERE {string.Join(" AND ", conditions)}";
    }

    private static string GetOrderByClause(VocabularySortOrder sortOrder) => sortOrder switch
    {
        VocabularySortOrder.UpdatedAtDescending => "ORDER BY ve.updated_at_utc DESC, ve.id ASC",
        VocabularySortOrder.HeadwordAscending => "ORDER BY ve.headword COLLATE NOCASE ASC, ve.id ASC",
        VocabularySortOrder.CreatedAtDescending => "ORDER BY ve.created_at_utc DESC, ve.id ASC",
        _ => throw new ArgumentOutOfRangeException(nameof(sortOrder), "Sort order must be defined.")
    };

    private static string EscapeLikeLiteral(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("%", "\\%", StringComparison.Ordinal)
        .Replace("_", "\\_", StringComparison.Ordinal);

    private static string AddEntryIdParameters(SqliteCommand command, IReadOnlyList<Guid> entryIds)
    {
        var names = new string[entryIds.Count];
        for (var index = 0; index < entryIds.Count; index++)
        {
            names[index] = $"$entryId{index}";
            AddGuid(command, names[index], entryIds[index]);
        }

        return string.Join(", ", names);
    }

    private static VocabularyEntry MapVocabularyEntry(SqliteDataReader reader)
    {
        try
        {
            var createdAt = ParseUtc(ReadRequiredText(reader, 10, "creation timestamp"), "creation timestamp");
            var updatedAt = ParseUtc(ReadRequiredText(reader, 11, "update timestamp"), "update timestamp");
            return new VocabularyEntry(
                ParseStoredGuid(reader.GetString(0), "entry identifier"),
                ReadRequiredText(reader, 1, "headword"),
                ReadRequiredText(reader, 2, "normalized headword"),
                ReadEntryType(reader, 3),
                ReadNullableText(reader, 4),
                ReadNullableText(reader, 5),
                ReadNullableText(reader, 6),
                ReadNullableText(reader, 7),
                ReadNullableText(reader, 8),
                ReadStoredBoolean(reader, 9, "archive state"),
                createdAt,
                updatedAt);
        }
        catch (InvalidDataException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new InvalidDataException("Stored vocabulary entry is invalid.", exception);
        }
    }

    private static SentenceExample MapSentenceExample(SqliteDataReader reader, int offset)
    {
        try
        {
            return new SentenceExample(
                ParseStoredGuid(reader.GetString(offset), "example identifier"),
                ReadNullableStoredGuid(reader, offset + 1, "capture identifier"),
                ReadNullableStoredGuid(reader, offset + 2, "OCR region identifier"),
                ReadRequiredText(reader, offset + 3, "sentence text"),
                ReadRequiredText(reader, offset + 4, "normalized sentence"),
                reader.GetInt32(offset + 5),
                reader.GetInt32(offset + 6),
                ReadRequiredTextAllowEmpty(reader, offset + 7, "screenshot crop path"),
                ReadNullableText(reader, offset + 8),
                ParseUtc(ReadRequiredText(reader, offset + 9, "creation timestamp"), "creation timestamp"));
        }
        catch (InvalidDataException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new InvalidDataException("Stored sentence example is invalid.", exception);
        }
    }

    private static TagSummary MapTagSummary(SqliteDataReader reader, int offset)
    {
        try
        {
            return new TagSummary(
                ParseStoredGuid(reader.GetString(offset), "tag identifier"),
                ReadRequiredText(reader, offset + 1, "tag name"),
                ReadRequiredText(reader, offset + 2, "normalized tag name"));
        }
        catch (InvalidDataException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new InvalidDataException("Stored tag is invalid.", exception);
        }
    }

    private static Guid ParseStoredGuid(string value, string fieldName)
    {
        if (!Guid.TryParse(value, out var result) || result == Guid.Empty)
        {
            throw new InvalidDataException($"Stored {fieldName} is invalid.");
        }

        return result;
    }

    private static Guid? ReadNullableStoredGuid(SqliteDataReader reader, int ordinal, string fieldName) =>
        reader.IsDBNull(ordinal) ? null : ParseStoredGuid(reader.GetString(ordinal), fieldName);

    private static EntryType ReadEntryType(SqliteDataReader reader, int ordinal) => reader.GetInt32(ordinal) switch
    {
        0 => EntryType.Word,
        1 => EntryType.Phrase,
        2 => EntryType.Expression,
        3 => EntryType.SentencePattern,
        _ => throw new InvalidDataException("Stored entry type is invalid.")
    };

    private static bool ReadStoredBoolean(SqliteDataReader reader, int ordinal, string fieldName) =>
        reader.GetInt32(ordinal) switch
        {
            0 => false,
            1 => true,
            _ => throw new InvalidDataException($"Stored {fieldName} is invalid.")
        };

    private static string ReadRequiredText(SqliteDataReader reader, int ordinal, string fieldName)
    {
        if (reader.IsDBNull(ordinal))
        {
            throw new InvalidDataException($"Stored {fieldName} is missing.");
        }

        var value = reader.GetString(ordinal);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidDataException($"Stored {fieldName} is invalid.");
        }

        return value;
    }

    private static string ReadRequiredTextAllowEmpty(SqliteDataReader reader, int ordinal, string fieldName)
    {
        if (reader.IsDBNull(ordinal))
        {
            throw new InvalidDataException($"Stored {fieldName} is missing.");
        }

        return reader.GetString(ordinal);
    }

    private static string? ReadNullableText(SqliteDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

    private static void EnsureIdentifier(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Identifier must not be empty.", parameterName);
        }
    }

    private static async Task<T> ExecuteReadTransactionAsync<T>(
        SqliteConnection connection,
        CancellationToken cancellationToken,
        Func<SqliteTransaction, Task<T>> operation)
    {
        await using var transaction = (SqliteTransaction)await connection
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);
        try
        {
            var result = await operation(transaction).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return result;
        }
        catch
        {
            try
            {
                await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch
            {
                // Preserve the query failure rather than replacing it with rollback failure.
            }

            throw;
        }
    }

    private sealed record PrimaryExample(string Text, string? GameTitle);
}
