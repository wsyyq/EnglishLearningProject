using GameLexicon.Domain.Entries;

namespace GameLexicon.Domain.Tests.Entries;

public sealed class TagTests
{
    [Fact]
    public void Constructor_StoresValues()
    {
        var id = Guid.NewGuid();

        var tag = new Tag(id, "Boss Fight", "boss fight");

        Assert.Equal(id, tag.Id);
        Assert.Equal("Boss Fight", tag.Name);
        Assert.Equal("boss fight", tag.NormalizedName);
    }

    [Fact]
    public void Constructor_RejectsEmptyId()
    {
        Assert.Throws<ArgumentException>(() =>
            new Tag(Guid.Empty, "Tag", "tag"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsMissingName(string? value)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new Tag(Guid.NewGuid(), value!, "normalized"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsMissingNormalizedName(string? value)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new Tag(Guid.NewGuid(), "Tag", value!));
    }

    [Fact]
    public void Rename_StoresProvidedValuesWithoutNormalizing()
    {
        var tag = new Tag(Guid.NewGuid(), "Old", "old");

        tag.Rename("  PROVIDED  ", "CUSTOM KEY");

        Assert.Equal("  PROVIDED  ", tag.Name);
        Assert.Equal("CUSTOM KEY", tag.NormalizedName);
    }

    [Fact]
    public void Rename_InvalidNormalizedNameLeavesObjectUnchanged()
    {
        var tag = new Tag(Guid.NewGuid(), "Old", "old");

        Assert.Throws<ArgumentException>(() => tag.Rename("Changed", " "));

        Assert.Equal("Old", tag.Name);
        Assert.Equal("old", tag.NormalizedName);
    }
}
