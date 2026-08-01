using System.Text;
using System.Text.Json;
using GameLexicon.Application.Configuration;
using GameLexicon.Infrastructure.Configuration;

namespace GameLexicon.Infrastructure.Tests.Configuration;

public sealed class JsonAppSettingsServiceTests
{
    [Fact]
    public void LoadCreatesFormattedUtf8DefaultsWhenFileDoesNotExist()
    {
        using var directory = new TestDirectory();
        var path = Path.Combine(directory.Path, "config", "settings.json");
        var service = new JsonAppSettingsService(path);

        var settings = service.Load();

        Assert.False(settings.DevelopmentMode);
        Assert.Equal(1, settings.SchemaVersion);
        Assert.Equal(14, settings.Logging.RetentionDays);
        Assert.Equal(10, settings.Logging.MaxFileSizeMb);
        Assert.True(File.Exists(path));
        var bytes = File.ReadAllBytes(path);
        Assert.False(bytes.AsSpan().StartsWith(Encoding.UTF8.Preamble));
        var json = Encoding.UTF8.GetString(bytes);
        Assert.Contains("\n  \"schema_version\"", json);
        Assert.Contains("\"development_mode\": false", json);
        using var document = JsonDocument.Parse(json);
        Assert.Equal(1, document.RootElement.GetProperty("schema_version").GetInt32());
    }

    [Fact]
    public void SavePersistsDevelopmentModeAndLeavesNoTemporaryFile()
    {
        using var directory = new TestDirectory();
        var path = Path.Combine(directory.Path, "config", "settings.json");
        var service = new JsonAppSettingsService(path);
        var settings = service.Load();
        settings.DevelopmentMode = true;

        service.Save(settings);
        var reloaded = new JsonAppSettingsService(path).Load();

        Assert.True(service.Current.DevelopmentMode);
        Assert.True(reloaded.DevelopmentMode);
        Assert.False(File.Exists(path + ".tmp"));
    }

    [Fact]
    public void LoadPreservesCorruptJsonAndRecreatesDefaultsWithoutEchoingContent()
    {
        using var directory = new TestDirectory();
        var configDirectory = Path.Combine(directory.Path, "config");
        Directory.CreateDirectory(configDirectory);
        var path = Path.Combine(configDirectory, "settings.json");
        const string corruptContent = "{ api_key=CORRUPT_SECRET";
        File.WriteAllText(path, corruptContent);
        var time = new FixedTimeProvider(new DateTimeOffset(2026, 8, 1, 12, 34, 56, TimeSpan.Zero));

        var settings = new JsonAppSettingsService(path, time).Load();

        Assert.False(settings.DevelopmentMode);
        Assert.DoesNotContain(corruptContent, File.ReadAllText(path));
        var backup = Assert.Single(Directory.GetFiles(configDirectory, "settings.corrupt-*.json"));
        Assert.Equal(corruptContent, File.ReadAllText(backup));
    }

    [Fact]
    public void LoadClampsUnsafeLoggingValuesToDefaultsAndAllowsUnknownFields()
    {
        using var directory = new TestDirectory();
        var path = Path.Combine(directory.Path, "config", "settings.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, """
            {
              "schema_version": 1,
              "development_mode": true,
              "future_value": "ignored",
              "logging": {
                "retention_days": 0,
                "max_file_size_mb": 2048
              }
            }
            """);

        var settings = new JsonAppSettingsService(path).Load();

        Assert.True(settings.DevelopmentMode);
        Assert.Equal(14, settings.Logging.RetentionDays);
        Assert.Equal(10, settings.Logging.MaxFileSizeMb);
    }

    [Fact]
    public void SavingDevelopmentModePreservesOtherSettings()
    {
        using var directory = new TestDirectory();
        var path = Path.Combine(directory.Path, "config", "settings.json");
        var service = new JsonAppSettingsService(path);
        var settings = new AppSettings
        {
            SchemaVersion = 7,
            DevelopmentMode = true,
            Logging = new LoggingSettings { RetentionDays = 30, MaxFileSizeMb = 20 }
        };

        service.Save(settings);

        Assert.Equal(7, service.Current.SchemaVersion);
        Assert.Equal(30, service.Current.Logging.RetentionDays);
        Assert.Equal(20, service.Current.Logging.MaxFileSizeMb);
    }
}
