using System.Globalization;
using GameLexicon.Application.Abstractions;
using Microsoft.Data.Sqlite;

namespace GameLexicon.Infrastructure.Persistence.Migrations;

public sealed class MigrationRunner
{
    private const string MigrationTimestampFormat = "yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'";

    private readonly SqliteConnectionFactory _connectionFactory;
    private readonly IReadOnlyList<IDatabaseMigration> _migrations;
    private readonly TimeProvider _timeProvider;
    private readonly IAppLogger? _logger;

    public MigrationRunner(
        SqliteConnectionFactory connectionFactory,
        IEnumerable<IDatabaseMigration> migrations,
        TimeProvider? timeProvider = null,
        IAppLogger? logger = null)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        ArgumentNullException.ThrowIfNull(migrations);
        _migrations = ValidateMigrations(migrations);
        _timeProvider = timeProvider ?? TimeProvider.System;
        _logger = logger;
    }

    public async Task<MigrationResult> RunAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await using var connection = await _connectionFactory
            .OpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);

        await EnsureMigrationTableAsync(connection, cancellationToken).ConfigureAwait(false);
        var applied = await ReadAppliedVersionsAsync(connection, cancellationToken).ConfigureAwait(false);
        var highestKnownVersion = _migrations[^1].Version;

        if (applied.Count > 0 && applied.Max() > highestKnownVersion)
        {
            throw new InvalidOperationException(
                "Database schema is newer than this application supports.");
        }

        var newlyApplied = new List<int>();
        foreach (var migration in _migrations.Where(item => !applied.Contains(item.Version)))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await ApplyMigrationAsync(connection, migration, cancellationToken).ConfigureAwait(false);
            newlyApplied.Add(migration.Version);
            _logger?.Information(
                "Database",
                "MigrationApplied",
                $"Database migration applied: {migration.Version}.");
        }

        var currentVersion = applied.Concat(newlyApplied).DefaultIfEmpty(0).Max();
        _logger?.Information(
            "Database",
            "SchemaCurrent",
            $"Database schema is current: {currentVersion}.");
        return new MigrationResult(currentVersion, newlyApplied);
    }

    private async Task ApplyMigrationAsync(
        SqliteConnection connection,
        IDatabaseMigration migration,
        CancellationToken cancellationToken)
    {
        await using var transaction = connection.BeginTransaction();
        try
        {
            await migration.ApplyAsync(connection, transaction, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO schema_migrations (version, applied_at_utc)
                VALUES ($version, $appliedAtUtc);
                """;
            command.Parameters.AddWithValue("$version", migration.Version);
            command.Parameters.AddWithValue(
                "$appliedAtUtc",
                _timeProvider.GetUtcNow().UtcDateTime.ToString(
                    MigrationTimestampFormat,
                    CultureInfo.InvariantCulture));
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }

    private static async Task EnsureMigrationTableAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS schema_migrations (
                version INTEGER PRIMARY KEY,
                applied_at_utc TEXT NOT NULL
            );
            """;
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<HashSet<int>> ReadAppliedVersionsAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT version FROM schema_migrations ORDER BY version;";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        var versions = new HashSet<int>();
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            versions.Add(reader.GetInt32(0));
        }

        return versions;
    }

    private static IReadOnlyList<IDatabaseMigration> ValidateMigrations(
        IEnumerable<IDatabaseMigration> migrations)
    {
        var ordered = migrations.OrderBy(item => item.Version).ToArray();
        if (ordered.Length == 0 || ordered[0].Version != 1)
        {
            throw new ArgumentException("Migration version 1 must be registered.", nameof(migrations));
        }

        if (ordered.Any(item => item.Version <= 0))
        {
            throw new ArgumentException("Migration versions must be positive.", nameof(migrations));
        }

        if (ordered.GroupBy(item => item.Version).Any(group => group.Count() > 1))
        {
            throw new ArgumentException("Migration versions must be unique.", nameof(migrations));
        }

        return ordered;
    }
}
