using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using GameLexicon.Application.Abstractions;
using GameLexicon.Application.Logging;

namespace GameLexicon.Infrastructure.Logging;

public sealed partial class RollingFileLogger : IAppLogger
{
    private readonly object _sync = new();
    private readonly string _logDirectory;
    private readonly int _retentionDays;
    private readonly long _maxFileSizeBytes;
    private readonly TimeProvider _timeProvider;
    private bool _disposed;

    public RollingFileLogger(RollingFileLoggerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.LogDirectory);

        if (options.RetentionDays is < 1 or > 365)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Retention days must be between 1 and 365.");
        }

        if (options.MaxFileSizeBytes < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Maximum file size must be positive.");
        }

        _logDirectory = Path.GetFullPath(options.LogDirectory);
        _retentionDays = options.RetentionDays;
        _maxFileSizeBytes = options.MaxFileSizeBytes;
        _timeProvider = options.TimeProvider;
        DevelopmentMode = options.DevelopmentMode;

        Directory.CreateDirectory(_logDirectory);
        TryCleanExpiredLogs();
    }

    public bool DevelopmentMode { get; private set; }

    public void SetDevelopmentMode(bool enabled)
    {
        lock (_sync)
        {
            ThrowIfDisposed();
            DevelopmentMode = enabled;
        }
    }

    public void Debug(string category, string eventName, string message)
    {
        if (DevelopmentMode)
        {
            Write(AppLogLevel.Debug, category, eventName, message, null);
        }
    }

    public void Information(string category, string eventName, string message) =>
        Write(AppLogLevel.Information, category, eventName, message, null);

    public void Warning(string category, string eventName, string message) =>
        Write(AppLogLevel.Warning, category, eventName, message, null);

    public void Error(string category, string eventName, string message, Exception? exception = null) =>
        Write(AppLogLevel.Error, category, eventName, message, exception);

    public void Dispose()
    {
        lock (_sync)
        {
            _disposed = true;
        }
    }

    private void Write(
        AppLogLevel level,
        string category,
        string eventName,
        string message,
        Exception? exception)
    {
        lock (_sync)
        {
            ThrowIfDisposed();

            var now = _timeProvider.GetUtcNow();
            var safeCategory = MakeSingleLine(SensitiveDataRedactor.Redact(category));
            var safeEventName = MakeSingleLine(SensitiveDataRedactor.Redact(eventName));
            var safeMessage = MakeSingleLine(SensitiveDataRedactor.Redact(message));
            var exceptionSummary = exception is null
                ? string.Empty
                : $" Exception={exception.GetType().Name}: {MakeSingleLine(SensitiveDataRedactor.Redact(exception.Message))}";
            var line = $"{now:O} [{level}] {safeCategory}/{safeEventName} {safeMessage}{exceptionSummary}{Environment.NewLine}";
            var bytes = Encoding.UTF8.GetByteCount(line);
            var path = ResolveWritablePath(now, bytes);
            File.AppendAllText(path, line, new UTF8Encoding(false));
        }
    }

    private string ResolveWritablePath(DateTimeOffset now, int incomingBytes)
    {
        var date = now.UtcDateTime.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var sequence = 0;

        while (true)
        {
            var fileName = sequence == 0
                ? $"gamelexicon-{date}.log"
                : $"gamelexicon-{date}.{sequence}.log";
            var path = Path.Combine(_logDirectory, fileName);
            var existingLength = File.Exists(path) ? new FileInfo(path).Length : 0;

            if (existingLength == 0 || existingLength + incomingBytes <= _maxFileSizeBytes)
            {
                return path;
            }

            sequence++;
        }
    }

    private void TryCleanExpiredLogs()
    {
        try
        {
            var cutoffDate = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime.Date)
                .AddDays(-_retentionDays);

            foreach (var path in Directory.EnumerateFiles(_logDirectory, "gamelexicon-*.log"))
            {
                var match = LogFileNamePattern().Match(Path.GetFileName(path));
                if (!match.Success ||
                    !DateOnly.TryParseExact(
                        match.Groups[1].Value,
                        "yyyyMMdd",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out var fileDate))
                {
                    continue;
                }

                if (fileDate < cutoffDate)
                {
                    try
                    {
                        File.Delete(path);
                    }
                    catch (IOException)
                    {
                    }
                    catch (UnauthorizedAccessException)
                    {
                    }
                }
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static string MakeSingleLine(string value) =>
        value.Replace('\r', ' ').Replace('\n', ' ');

    private void ThrowIfDisposed() =>
        ObjectDisposedException.ThrowIf(_disposed, this);

    [GeneratedRegex(@"^gamelexicon-(\d{8})(?:\.\d+)?\.log$", RegexOptions.CultureInvariant)]
    private static partial Regex LogFileNamePattern();
}
