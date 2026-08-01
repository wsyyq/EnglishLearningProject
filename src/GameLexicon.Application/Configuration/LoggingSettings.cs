namespace GameLexicon.Application.Configuration;

public sealed class LoggingSettings
{
    public int RetentionDays { get; set; } = 14;

    public int MaxFileSizeMb { get; set; } = 10;
}
