using System.Globalization;
using System.Text;

namespace GameLexicon.Domain.Text;

public sealed class EnglishExpressionNormalizer : ITextNormalizer
{
    public string Normalize(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var normalized = value.Normalize(NormalizationForm.FormKC);
        var builder = new StringBuilder(normalized.Length);
        var previousWasWhitespace = false;

        foreach (var character in normalized)
        {
            if (char.IsWhiteSpace(character))
            {
                if (!previousWasWhitespace)
                {
                    builder.Append(' ');
                    previousWasWhitespace = true;
                }

                continue;
            }

            builder.Append(NormalizeApostrophe(character));
            previousWasWhitespace = false;
        }

        return TrimBoundaryPunctuation(builder.ToString().ToLowerInvariant().Trim()).Trim();
    }

    private static char NormalizeApostrophe(char character) => character switch
    {
        '\u2018' or '\u2019' or '\u02BC' or '\uFF07' => '\'',
        _ => character
    };

    private static string TrimBoundaryPunctuation(string value)
    {
        var start = 0;
        var end = value.Length;

        while (start < end)
        {
            var rune = Rune.GetRuneAt(value, start);
            if (!IsBoundaryNoise(rune))
            {
                break;
            }

            start += rune.Utf16SequenceLength;
        }

        while (end > start)
        {
            Rune.DecodeLastFromUtf16(value.AsSpan(start, end - start), out var rune, out var consumed);
            if (!IsBoundaryNoise(rune))
            {
                break;
            }

            end -= consumed;
        }

        return value[start..end];
    }

    private static bool IsBoundaryNoise(Rune rune) =>
        Rune.IsWhiteSpace(rune) ||
        Rune.GetUnicodeCategory(rune) is
            UnicodeCategory.ConnectorPunctuation or
            UnicodeCategory.DashPunctuation or
            UnicodeCategory.OpenPunctuation or
            UnicodeCategory.ClosePunctuation or
            UnicodeCategory.InitialQuotePunctuation or
            UnicodeCategory.FinalQuotePunctuation or
            UnicodeCategory.OtherPunctuation;
}
