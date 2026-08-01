namespace GameLexicon.Domain.Entries;

public sealed class Tag
{
    public Tag(Guid id, string name, string normalizedName)
    {
        EntryGuard.NotEmpty(id, nameof(id));
        EntryGuard.Required(name, nameof(name));
        EntryGuard.Required(normalizedName, nameof(normalizedName));

        Id = id;
        Name = name;
        NormalizedName = normalizedName;
    }

    public Guid Id { get; }
    public string Name { get; private set; }
    public string NormalizedName { get; private set; }

    public void Rename(string name, string normalizedName)
    {
        EntryGuard.Required(name, nameof(name));
        EntryGuard.Required(normalizedName, nameof(normalizedName));

        Name = name;
        NormalizedName = normalizedName;
    }
}
