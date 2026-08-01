namespace GameLexicon.Infrastructure.Persistence;

public sealed class DatabaseOptions
{
    public required string DatabasePath { get; init; }

    public bool EnableWriteAheadLogging { get; init; } = true;

    public int BusyTimeoutMilliseconds { get; init; } = 5000;
}
