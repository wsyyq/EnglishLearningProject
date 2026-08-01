#nullable enable

using GameLexicon.Application.Abstractions;
using GameLexicon.Infrastructure.Configuration;
using GameLexicon.Infrastructure.Logging;
using GameLexicon.Infrastructure.Persistence;
using GameLexicon.Infrastructure.Persistence.Migrations;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

public static class AppServices
{
    private static IAppSettingsService? _settingsService;
    private static IAppLogger? _logger;

    public static IAppSettingsService SettingsService =>
        _settingsService ?? throw new InvalidOperationException("Application services are not initialized.");

    public static IAppLogger Logger =>
        _logger ?? throw new InvalidOperationException("Application services are not initialized.");

    public static async Task InitializeAsync(
        string userDataPath,
        string databasePath,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userDataPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);

        if (_settingsService is not null || _logger is not null)
        {
            throw new InvalidOperationException("Application services are already initialized.");
        }

        IAppLogger? logger = null;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
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

            logger.Information("App", "Startup", "Application started. Version=0.1.0");
            logger.Information(
                "Configuration",
                "Loaded",
                $"Settings loaded. DevelopmentMode={(settings.DevelopmentMode ? "enabled" : "disabled")}");
            logger.Information("Database", "InitializationStarted", "Database initialization started.");

            var connectionFactory = new SqliteConnectionFactory(new DatabaseOptions
            {
                DatabasePath = databasePath,
                EnableWriteAheadLogging = true,
                BusyTimeoutMilliseconds = 5000
            });
            var migrationRunner = new MigrationRunner(
                connectionFactory,
                [new Migration001_Initial()],
                logger: logger);
            await migrationRunner.RunAsync(cancellationToken);
            logger.Information("Database", "InitializationCompleted", "Database initialization completed.");

            cancellationToken.ThrowIfCancellationRequested();
            _settingsService = settingsService;
            _logger = logger;
        }
        catch (Exception exception)
        {
            logger?.Error(
                "Database",
                "InitializationFailed",
                "Database initialization failed.",
                exception);
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
