using GameLexicon.Infrastructure.Logging;

namespace GameLexicon.Infrastructure.Tests.Logging;

public sealed class RollingFileLoggerTests
{
    private static readonly DateTimeOffset TestNow =
        new(2026, 8, 1, 11, 8, 0, TimeSpan.Zero);

    [Fact]
    public void ConstructorCreatesDirectoryAndInformationUsesExpectedFileName()
    {
        using var directory = new TestDirectory();
        var logDirectory = Path.Combine(directory.Path, "logs");
        using var logger = CreateLogger(logDirectory);

        logger.Information("App", "Startup", "Application started.");

        var path = Path.Combine(logDirectory, "gamelexicon-20260801.log");
        Assert.True(File.Exists(path));
        Assert.Contains("[Information] App/Startup Application started.", File.ReadAllText(path));
    }

    [Fact]
    public void SizeLimitCreatesNumberedRollFile()
    {
        using var directory = new TestDirectory();
        using var logger = CreateLogger(directory.Path, maxBytes: 100);

        logger.Information("Test", "One", new string('A', 40));
        logger.Information("Test", "Two", new string('B', 40));

        Assert.True(File.Exists(Path.Combine(directory.Path, "gamelexicon-20260801.log")));
        Assert.True(File.Exists(Path.Combine(directory.Path, "gamelexicon-20260801.1.log")));
    }

    [Fact]
    public async Task ConcurrentWritesRemainOneRecordPerLine()
    {
        using var directory = new TestDirectory();
        using var logger = CreateLogger(directory.Path, maxBytes: 100_000);

        await Task.WhenAll(Enumerable.Range(0, 30).Select(index => Task.Run(() =>
            logger.Information("Concurrent", "Write", $"Record-{index}"))));

        var lines = File.ReadAllLines(Path.Combine(directory.Path, "gamelexicon-20260801.log"));
        Assert.Equal(30, lines.Length);
        Assert.All(lines, line => Assert.Contains("Concurrent/Write Record-", line));
    }

    [Fact]
    public void DisposeReleasesFilesAndRejectsFurtherWrites()
    {
        using var directory = new TestDirectory();
        var logger = CreateLogger(directory.Path);
        logger.Information("Test", "Write", "Safe message");
        logger.Dispose();
        var path = Path.Combine(directory.Path, "gamelexicon-20260801.log");

        using (File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
        }

        Assert.Throws<ObjectDisposedException>(() =>
            logger.Information("Test", "Write", "After dispose"));
    }

    [Fact]
    public void CleanupDeletesOnlyExpiredApplicationLogs()
    {
        using var directory = new TestDirectory();
        var expired = Path.Combine(directory.Path, "gamelexicon-20260717.log");
        var retained = Path.Combine(directory.Path, "gamelexicon-20260718.1.log");
        var foreign = Path.Combine(directory.Path, "notes-20260701.log");
        var malformed = Path.Combine(directory.Path, "gamelexicon-old.log");
        File.WriteAllText(expired, "expired");
        File.WriteAllText(retained, "retained");
        File.WriteAllText(foreign, "foreign");
        File.WriteAllText(malformed, "malformed");

        using var logger = CreateLogger(directory.Path, retentionDays: 14);

        Assert.False(File.Exists(expired));
        Assert.True(File.Exists(retained));
        Assert.True(File.Exists(foreign));
        Assert.True(File.Exists(malformed));
    }

    [Fact]
    public void DevelopmentModeControlsDebugWithoutAffectingInformation()
    {
        using var directory = new TestDirectory();
        using var logger = CreateLogger(directory.Path);

        logger.Debug("Mode", "Before", "hidden-before");
        logger.Information("Mode", "Info", "visible-info");
        logger.SetDevelopmentMode(true);
        logger.Debug("Mode", "Enabled", "visible-debug");
        logger.SetDevelopmentMode(false);
        logger.Debug("Mode", "After", "hidden-after");

        var content = ReadAllLogs(directory.Path);
        Assert.DoesNotContain("hidden-before", content);
        Assert.Contains("visible-info", content);
        Assert.Contains("visible-debug", content);
        Assert.DoesNotContain("hidden-after", content);
    }

    [Fact]
    public void LoggerRedactsSecretsAndDoesNotReceiveLearningTextSentinel()
    {
        using var directory = new TestDirectory();
        using var logger = CreateLogger(directory.Path, developmentMode: true);

        logger.Debug(
            "Security",
            "RedactionTest",
            "api_key=TEST_SECRET_123 Authorization: Bearer TEST_TOKEN_456 password=TEST_PASSWORD_789");

        var content = ReadAllLogs(directory.Path);
        Assert.DoesNotContain("TEST_SECRET_123", content);
        Assert.DoesNotContain("TEST_TOKEN_456", content);
        Assert.DoesNotContain("TEST_PASSWORD_789", content);
        Assert.DoesNotContain("LEARNING_TEXT_MUST_NOT_BE_LOGGED", content);
        Assert.Contains("<redacted>", content);
    }

    [Fact]
    public void ErrorWritesOnlyRedactedExceptionTypeAndMessageWithoutStackTrace()
    {
        using var directory = new TestDirectory();
        using var logger = CreateLogger(directory.Path, developmentMode: true);
        var exception = new InvalidOperationException("token=TEST_EXCEPTION_TOKEN");

        logger.Error("App", "Failure", "Safe summary", exception);

        var content = ReadAllLogs(directory.Path);
        Assert.Contains("InvalidOperationException", content);
        Assert.Contains("<redacted>", content);
        Assert.DoesNotContain("TEST_EXCEPTION_TOKEN", content);
        Assert.DoesNotContain(" at ", content);
    }

    private static RollingFileLogger CreateLogger(
        string directory,
        int retentionDays = 14,
        long maxBytes = 10 * 1024 * 1024,
        bool developmentMode = false) =>
        new(new RollingFileLoggerOptions
        {
            LogDirectory = directory,
            RetentionDays = retentionDays,
            MaxFileSizeBytes = maxBytes,
            DevelopmentMode = developmentMode,
            TimeProvider = new FixedTimeProvider(TestNow)
        });

    private static string ReadAllLogs(string directory) =>
        string.Join(Environment.NewLine, Directory.GetFiles(directory, "gamelexicon-*.log")
            .OrderBy(path => path)
            .Select(File.ReadAllText));
}
