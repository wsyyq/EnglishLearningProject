namespace GameLexicon.Domain.Entries;

public sealed class EntryExampleLink
{
    public EntryExampleLink(
        Guid entryId,
        Guid exampleId,
        bool isPrimary,
        int sortOrder)
    {
        EntryGuard.NotEmpty(entryId, nameof(entryId));
        EntryGuard.NotEmpty(exampleId, nameof(exampleId));
        ValidateSortOrder(sortOrder);

        EntryId = entryId;
        ExampleId = exampleId;
        IsPrimary = isPrimary;
        SortOrder = sortOrder;
    }

    public Guid EntryId { get; }
    public Guid ExampleId { get; }
    public bool IsPrimary { get; private set; }
    public int SortOrder { get; private set; }

    public void SetPrimary(bool isPrimary)
    {
        IsPrimary = isPrimary;
    }

    public void SetSortOrder(int sortOrder)
    {
        ValidateSortOrder(sortOrder);
        SortOrder = sortOrder;
    }

    private static void ValidateSortOrder(int sortOrder)
    {
        if (sortOrder < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sortOrder),
                "Sort order must not be negative.");
        }
    }
}
