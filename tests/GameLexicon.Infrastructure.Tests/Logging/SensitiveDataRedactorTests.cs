using GameLexicon.Infrastructure.Logging;

namespace GameLexicon.Infrastructure.Tests.Logging;

public sealed class SensitiveDataRedactorTests
{
    [Theory]
    [InlineData("api_key=TEST_SECRET_123")]
    [InlineData("Authorization: Bearer TEST_TOKEN_456")]
    [InlineData("password=TEST_PASSWORD_789")]
    [InlineData("Cookie: TEST_COOKIE")]
    [InlineData("access_token=TEST_ACCESS_TOKEN")]
    public void RedactRemovesCommonSensitiveValues(string input)
    {
        var result = SensitiveDataRedactor.Redact(input);

        Assert.Contains("<redacted>", result);
        Assert.DoesNotContain(input.Split(['=', ':'], 2)[1].Trim(), result);
    }
}
