using GameLexicon.Application.Entries.Queries;
using GameLexicon.Domain.Entries;

namespace GameLexicon.Application.Abstractions.Persistence;

public interface IVocabularyRepository
{
    /// <summary>
    /// Finds an active entry by a caller-provided normalized headword.
    /// Archived entries are excluded and the value is not normalized again.
    /// </summary>
    Task<VocabularyEntry?> FindByNormalizedHeadwordAsync(
        string normalizedHeadword,
        CancellationToken cancellationToken);

    Task<VocabularyEntryDetails?> GetDetailsAsync(
        Guid entryId,
        CancellationToken cancellationToken);

    Task<PagedResult<VocabularyEntrySummary>> SearchAsync(
        VocabularySearchQuery query,
        CancellationToken cancellationToken);

    /// <summary>
    /// Creates or updates one entry by ID without normalizing its text or
    /// implicitly saving example and tag links.
    /// </summary>
    Task SaveAsync(
        VocabularyEntry entry,
        CancellationToken cancellationToken);
}
