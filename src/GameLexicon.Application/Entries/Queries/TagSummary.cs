namespace GameLexicon.Application.Entries.Queries;

public sealed class TagSummary
{
    public TagSummary(Guid id, string name, string normalizedName)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Identifier must not be empty.", nameof(id));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedName);

        Id = id;
        Name = name;
        NormalizedName = normalizedName;
    }

    public Guid Id { get; }
    public string Name { get; }
    public string NormalizedName { get; }
}
