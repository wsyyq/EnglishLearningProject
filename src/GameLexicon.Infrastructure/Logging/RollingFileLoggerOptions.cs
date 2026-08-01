namespace GameLexicon.Infrastructure.Logging;

public sealed class RollingFileLoggerOptions
{
    public required string LogDirectory { get; init; }

    public int RetentionDays { get; init; } = 14;

    public long MaxFileSizeBytes { get; init; } = 10L * 1024 * 1024;

    public bool DevelopmentMode { get; init; }

    public TimeProvider TimeProvider { get; init; } = TimeProvider.System;
}
