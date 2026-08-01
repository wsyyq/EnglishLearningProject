using GameLexicon.Domain.Entries;

namespace GameLexicon.Domain.Tests.Entries;

public sealed class EntryTypeTests
{
    [Theory]
    [InlineData(EntryType.Word, 0)]
    [InlineData(EntryType.Phrase, 1)]
    [InlineData(EntryType.Expression, 2)]
    [InlineData(EntryType.SentencePattern, 3)]
    public void Values_AreStable(EntryType entryType, int expected)
    {
        Assert.Equal(expected, (int)entryType);
    }
}
