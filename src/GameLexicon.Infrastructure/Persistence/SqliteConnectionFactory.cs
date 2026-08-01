using Microsoft.Data.Sqlite;

namespace GameLexicon.Infrastructure.Persistence;

public sealed class SqliteConnectionFactory
{
    private readonly bool _enableWriteAheadLogging;
    private readonly int _busyTimeoutMilliseconds;
    private readonly SemaphoreSlim _walInitializationLock = new(1, 1);
    private bool _walInitialized;

    public SqliteConnectionFactory(DatabaseOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.DatabasePath);

        if (options.BusyTimeoutMilliseconds < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                "Busy timeout must not be negative.");
        }

        DatabasePath = Path.GetFullPath(options.DatabasePath);
        if (Directory.Exists(DatabasePath))
        {
            throw new ArgumentException("Database path must identify a file.", nameof(options));
        }

        _enableWriteAheadLogging = options.EnableWriteAheadLogging;
        _busyTimeoutMilliseconds = options.BusyTimeoutMilliseconds;
    }

    public string DatabasePath { get; }

    public async Task<SqliteConnection> OpenConnectionAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var directory = Path.GetDirectoryName(DatabasePath)
            ?? throw new InvalidOperationException("Database path must have a parent directory.");
        Directory.CreateDirectory(directory);

        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = DatabasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            ForeignKeys = true,
            Pooling = false
        }.ToString();

        var connection = new SqliteConnection(connectionString);

        try
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await ConfigureConnectionAsync(connection, cancellationToken).ConfigureAwait(false);
            await EnsureWriteAheadLoggingAsync(connection, cancellationToken).ConfigureAwait(false);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    private async Task ConfigureConnectionAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            PRAGMA foreign_keys = ON;
            PRAGMA busy_timeout = {_busyTimeoutMilliseconds};
            """;
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task EnsureWriteAheadLoggingAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        if (!_enableWriteAheadLogging || _walInitialized)
        {
            return;
        }

        await _walInitializationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_walInitialized)
            {
                return;
            }

            await using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA journal_mode = WAL;";
            var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            if (!string.Equals(result?.ToString(), "wal", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("SQLite did not enable write-ahead logging.");
            }

            _walInitialized = true;
        }
        finally
        {
            _walInitializationLock.Release();
        }
    }
}
