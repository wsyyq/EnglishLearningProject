using System.Globalization;
using GameLexicon.Domain.Text;

namespace GameLexicon.Domain.Tests.Text;

public sealed class EnglishExpressionNormalizerTests
{
    private readonly EnglishExpressionNormalizer _normalizer = new();

    [Theory]
    [InlineData(" Get   Out! ", "get out")]
    [InlineData("Don't", "don't")]
    [InlineData("well-known", "well-known")]
    [InlineData("  Get   Out of Here! ", "get out of here")]
    [InlineData("Ｇｅｔ　Ｏｕｔ！", "get out")]
    [InlineData("① Choice", "1 choice")]
    [InlineData("Don’t", "don't")]
    [InlineData("‘Don’t’", "don't")]
    [InlineData("rock ’n’ roll", "rock 'n' roll")]
    [InlineData("rock ʼnʼ roll", "rock 'n' roll")]
    [InlineData("get\tout", "get out")]
    [InlineData("get\r\nout", "get out")]
    [InlineData("get\u00A0out", "get out")]
    [InlineData("  get \t  out  ", "get out")]
    [InlineData("(Get out!)", "get out")]
    [InlineData("...Get out?!", "get out")]
    [InlineData("“Get out.”", "get out")]
    [InlineData("[well-known]", "well-known")]
    [InlineData("-well-known-", "well-known")]
    [InlineData("don't", "don't")]
    [InlineData("rock 'n' roll", "rock 'n' roll")]
    [InlineData("word,word", "word,word")]
    [InlineData("Running Games", "running games")]
    [InlineData("children", "children")]
    [InlineData("get out", "get out")]
    [InlineData("can't", "can't")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    [InlineData("\t\r\n", "")]
    [InlineData("  ( Get out! )  ", "get out")]
    [InlineData("éclair", "éclair")]
    [InlineData("🙂 hello 🙂", "🙂 hello 🙂")]
    public void Normalize_ReturnsExpectedValue(string input, string expected)
    {
        Assert.Equal(expected, _normalizer.Normalize(input));
    }

    [Fact]
    public void Normalize_WithNull_ThrowsArgumentNullExceptionWithoutInputContent()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => _normalizer.Normalize(null!));

        Assert.Equal("value", exception.ParamName);
    }

    [Theory]
    [InlineData(" Get   Out! ")]
    [InlineData("‘Don’t’")]
    [InlineData("Ｇｅｔ　Ｏｕｔ！")]
    [InlineData("-well-known-")]
    [InlineData("...Get out?!")]
    public void Normalize_IsIdempotent(string input)
    {
        var once = _normalizer.Normalize(input);

        Assert.Equal(once, _normalizer.Normalize(once));
    }

    [Fact]
    public void Normalize_UsesInvariantCaseConversion()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");

            Assert.Equal("title", _normalizer.Normalize("TITLE"));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [Fact]
    public void Normalize_DoesNotChangeOriginalString()
    {
        const string input = " Get   Out! ";

        _normalizer.Normalize(input);

        Assert.Equal(" Get   Out! ", input);
    }
}
