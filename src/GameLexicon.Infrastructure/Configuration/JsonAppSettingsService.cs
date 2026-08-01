using System.Text;
using System.Text.Json;
using GameLexicon.Application.Abstractions;
using GameLexicon.Application.Configuration;

namespace GameLexicon.Infrastructure.Configuration;

public sealed class JsonAppSettingsService : IAppSettingsService
{
    private const int MinimumRetentionDays = 1;
    private const int MaximumRetentionDays = 365;
    private const int MinimumFileSizeMb = 1;
    private const int MaximumFileSizeMb = 1024;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true
    };

    private readonly string _settingsFilePath;
    private readonly TimeProvider _timeProvider;

    public JsonAppSettingsService(string settingsFilePath, TimeProvider? timeProvider = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(settingsFilePath);
        _settingsFilePath = Path.GetFullPath(settingsFilePath);
        _timeProvider = timeProvider ?? TimeProvider.System;
        Current = CreateDefaults();
    }

    public AppSettings Current { get; private set; }

    public AppSettings Load()
    {
        Directory.CreateDirectory(GetSettingsDirectory());

        if (!File.Exists(_settingsFilePath))
        {
            Current = CreateDefaults();
            WriteSafely(Current);
            return Current;
        }

        try
        {
            using var stream = File.OpenRead(_settingsFilePath);
            var settings = JsonSerializer.Deserialize<AppSettings>(stream, SerializerOptions)
                ?? throw new JsonException("Settings document was empty.");
            Current = Validate(settings);
            return Current;
        }
        catch (JsonException)
        {
            PreserveCorruptFile();
            Current = CreateDefaults();
            WriteSafely(Current);
            return Current;
        }
    }

    public void Save(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        Current = Validate(settings);
        Directory.CreateDirectory(GetSettingsDirectory());
        WriteSafely(Current);
    }

    private void WriteSafely(AppSettings settings)
    {
        var temporaryPath = _settingsFilePath + ".tmp";

        try
        {
            using (var stream = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
            {
                writer.Write(JsonSerializer.Serialize(settings, SerializerOptions));
                writer.WriteLine();
                writer.Flush();
                stream.Flush(flushToDisk: true);
            }

            File.Move(temporaryPath, _settingsFilePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private void PreserveCorruptFile()
    {
        var timestamp = _timeProvider.GetUtcNow().ToString("yyyyMMdd-HHmmss");
        var directory = GetSettingsDirectory();
        var backupPath = Path.Combine(directory, $"settings.corrupt-{timestamp}.json");
        var suffix = 0;

        while (File.Exists(backupPath))
        {
            suffix++;
            backupPath = Path.Combine(directory, $"settings.corrupt-{timestamp}-{suffix}.json");
        }

        File.Move(_settingsFilePath, backupPath);
    }

    private string GetSettingsDirectory() =>
        Path.GetDirectoryName(_settingsFilePath)
        ?? throw new InvalidOperationException("Settings file must have a parent directory.");

    private static AppSettings Validate(AppSettings settings)
    {
        settings.SchemaVersion = settings.SchemaVersion > 0 ? settings.SchemaVersion : 1;
        settings.Logging ??= new LoggingSettings();
        settings.Logging.RetentionDays = IsInRange(
            settings.Logging.RetentionDays,
            MinimumRetentionDays,
            MaximumRetentionDays) ? settings.Logging.RetentionDays : 14;
        settings.Logging.MaxFileSizeMb = IsInRange(
            settings.Logging.MaxFileSizeMb,
            MinimumFileSizeMb,
            MaximumFileSizeMb) ? settings.Logging.MaxFileSizeMb : 10;
        return settings;
    }

    private static bool IsInRange(int value, int minimum, int maximum) =>
        value >= minimum && value <= maximum;

    private static AppSettings CreateDefaults() => new();
}
