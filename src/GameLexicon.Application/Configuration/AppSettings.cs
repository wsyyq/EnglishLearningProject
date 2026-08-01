namespace GameLexicon.Application.Configuration;

public sealed class AppSettings
{
    public int SchemaVersion { get; set; } = 1;

    public bool DevelopmentMode { get; set; }

    public LoggingSettings Logging { get; set; } = new();
}
