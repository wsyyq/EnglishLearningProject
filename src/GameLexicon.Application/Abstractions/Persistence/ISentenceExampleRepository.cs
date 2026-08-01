using GameLexicon.Application.Entries.Queries;
using GameLexicon.Domain.Entries;

namespace GameLexicon.Application.Abstractions.Persistence;

public interface ISentenceExampleRepository
{
    Task<SentenceExample?> GetByIdAsync(
        Guid exampleId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns linked examples ordered by SortOrder and then ExampleId.
    /// </summary>
    Task<IReadOnlyList<SentenceExampleDetails>> GetForEntryAsync(
        Guid entryId,
        CancellationToken cancellationToken);

    Task SaveAsync(
        SentenceExample example,
        CancellationToken cancellationToken);

    Task SaveLinkAsync(
        EntryExampleLink link,
        CancellationToken cancellationToken);

    /// <summary>
    /// Atomically verifies the target link, clears the other primary links
    /// for the entry, and marks the target link as primary in one transaction.
    /// </summary>
    Task SetPrimaryAsync(
        Guid entryId,
        Guid exampleId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Removes only the link and does not delete the example or its files.
    /// </summary>
    Task RemoveLinkAsync(
        Guid entryId,
        Guid exampleId,
        CancellationToken cancellationToken);
}
