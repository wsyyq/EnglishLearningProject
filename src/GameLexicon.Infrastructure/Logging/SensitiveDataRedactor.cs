using System.Text.RegularExpressions;

namespace GameLexicon.Infrastructure.Logging;

public static partial class SensitiveDataRedactor
{
    public static string Redact(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return SensitiveValuePattern().Replace(value, match => $"{match.Groups[1].Value}=<redacted>");
    }

    [GeneratedRegex(
        @"\b(api[_-]?key|apikey|token|access[_-]?token|authorization|password|cookie|secret)\b\s*[:=]\s*(?:Bearer\s+)?[^\s,;]+",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SensitiveValuePattern();
}
