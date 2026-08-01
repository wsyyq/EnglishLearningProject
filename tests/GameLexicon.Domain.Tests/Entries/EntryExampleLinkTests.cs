using GameLexicon.Domain.Entries;

namespace GameLexicon.Domain.Tests.Entries;

public sealed class EntryExampleLinkTests
{
    [Fact]
    public void Constructor_StoresValuesAndAllowsZeroSortOrder()
    {
        var entryId = Guid.NewGuid();
        var exampleId = Guid.NewGuid();

        var link = new EntryExampleLink(entryId, exampleId, true, 0);

        Assert.Equal(entryId, link.EntryId);
        Assert.Equal(exampleId, link.ExampleId);
        Assert.True(link.IsPrimary);
        Assert.Equal(0, link.SortOrder);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Constructor_RejectsEmptyIdentifier(bool emptyEntry, bool emptyExample)
    {
        Assert.Throws<ArgumentException>(() => new EntryExampleLink(
            emptyEntry ? Guid.Empty : Guid.NewGuid(),
            emptyExample ? Guid.Empty : Guid.NewGuid(),
            false,
            0));
    }

    [Fact]
    public void Constructor_RejectsNegativeSortOrder()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new EntryExampleLink(
            Guid.NewGuid(),
            Guid.NewGuid(),
            false,
            -1));
    }

    [Fact]
    public void SetPrimary_ChangesOnlyThisLink()
    {
        // Enforcing a single primary link across a collection is a Repository concern.
        var link = new EntryExampleLink(Guid.NewGuid(), Guid.NewGuid(), false, 0);

        link.SetPrimary(true);

        Assert.True(link.IsPrimary);
    }

    [Fact]
    public void SetSortOrder_ChangesOrderAndPreservesIdentifiers()
    {
        var link = new EntryExampleLink(Guid.NewGuid(), Guid.NewGuid(), false, 0);
        var entryId = link.EntryId;
        var exampleId = link.ExampleId;

        link.SetSortOrder(2);

        Assert.Equal(2, link.SortOrder);
        Assert.Equal(entryId, link.EntryId);
        Assert.Equal(exampleId, link.ExampleId);
    }

    [Fact]
    public void SetSortOrder_RejectsNegativeValueWithoutChangingOrder()
    {
        var link = new EntryExampleLink(Guid.NewGuid(), Guid.NewGuid(), false, 1);

        Assert.Throws<ArgumentOutOfRangeException>(() => link.SetSortOrder(-1));

        Assert.Equal(1, link.SortOrder);
    }
}
