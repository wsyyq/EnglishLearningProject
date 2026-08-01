#nullable enable

using GameLexicon.Application.Abstractions;
using GameLexicon.Infrastructure.Configuration;
using GameLexicon.Infrastructure.Logging;
using System;
using System.IO;

public static class AppServices
{
    private static IAppSettingsService? _settingsService;
    private static IAppLogger? _logger;

    public static IAppSettingsService SettingsService =>
        _settingsService ?? throw new InvalidOperationException("Application services are not initialized.");

    public static IAppLogger Logger =>
        _logger ?? throw new InvalidOperationException("Application services are not initialized.");

    public static void Initialize(string userDataPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userDataPath);

        if (_settingsService is not null || _logger is not null)
        {
            throw new InvalidOperationException("Application services are already initialized.");
        }

        IAppLogger? logger = null;

        try
        {
            var settingsPath = Path.Combine(userDataPath, "config", "settings.json");
            var settingsService = new JsonAppSettingsService(settingsPath);
            var settings = settingsService.Load();

            logger = new RollingFileLogger(new RollingFileLoggerOptions
            {
                LogDirectory = Path.Combine(userDataPath, "logs"),
                RetentionDays = settings.Logging.RetentionDays,
                MaxFileSizeBytes = settings.Logging.MaxFileSizeMb * 1024L * 1024L,
                DevelopmentMode = settings.DevelopmentMode
            });

            _settingsService = settingsService;
            _logger = logger;
            logger.Information("App", "Startup", "Application started. Version=0.1.0");
            logger.Information(
                "Configuration",
                "Loaded",
                $"Settings loaded. DevelopmentMode={(settings.DevelopmentMode ? "enabled" : "disabled")}");
        }
        catch
        {
            logger?.Dispose();
            _settingsService = null;
            _logger = null;
            throw;
        }
    }

    public static void Shutdown()
    {
        var logger = _logger;
        _logger = null;
        _settingsService = null;

        if (logger is null)
        {
            return;
        }

        try
        {
            logger.Information("App", "Shutdown", "Application closed normally.");
        }
        finally
        {
            logger.Dispose();
        }
    }
}
