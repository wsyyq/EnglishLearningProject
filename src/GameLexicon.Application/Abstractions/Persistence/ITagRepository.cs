using GameLexicon.Domain.Entries;

namespace GameLexicon.Application.Abstractions.Persistence;

public interface ITagRepository
{
    Task<Tag?> FindByNormalizedNameAsync(
        string normalizedName,
        CancellationToken cancellationToken);

    Task<Tag> GetOrCreateAsync(
        Tag candidate,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Tag>> GetForEntryAsync(
        Guid entryId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Atomically replaces all tag links for an entry with the supplied
    /// distinct, nonempty tag IDs. An empty list clears the links.
    /// </summary>
    Task SetForEntryAsync(
        Guid entryId,
        IReadOnlyList<Guid> tagIds,
        CancellationToken cancellationToken);
}
