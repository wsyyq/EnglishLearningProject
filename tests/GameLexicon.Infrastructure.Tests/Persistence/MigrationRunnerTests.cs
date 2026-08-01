using GameLexicon.Infrastructure.Persistence.Migrations;
using Microsoft.Data.Sqlite;

namespace GameLexicon.Infrastructure.Tests.Persistence;

public sealed class MigrationRunnerTests
{
    [Fact]
    public async Task FirstRunAppliesVersionOneAndSecondRunIsIdempotent()
    {
        using var directory = new TestDirectory();
        var factory = SqliteConnectionFactoryTests.CreateFactory(
            Path.Combine(directory.Path, "gamelexicon.db"));
        var runner = new MigrationRunner(factory, [new Migration001_Initial()]);

        var first = await runner.RunAsync();
        var second = await runner.RunAsync();

        Assert.Equal(1, first.CurrentVersion);
        Assert.Equal([1], first.AppliedVersions);
        Assert.Equal(1, second.CurrentVersion);
        Assert.Empty(second.AppliedVersions);
        await using var connection = await factory.OpenConnectionAsync();
        Assert.Equal(
            1L,
            await SqliteConnectionFactoryTests.ExecuteScalarAsync<long>(
                connection,
                "SELECT COUNT(*) FROM schema_migrations WHERE version = 1;"));
    }

    [Fact]
    public async Task MigrationsExecuteInVersionOrder()
    {
        using var directory = new TestDirectory();
        var calls = new List<int>();
        var factory = SqliteConnectionFactoryTests.CreateFactory(
            Path.Combine(directory.Path, "gamelexicon.db"));
        var runner = new MigrationRunner(
            factory,
            [new RecordingMigration(2, calls), new RecordingMigration(1, calls)]);

        var result = await runner.RunAsync();

        Assert.Equal([1, 2], calls);
        Assert.Equal([1, 2], result.AppliedVersions);
    }

    [Fact]
    public void DuplicateVersionsFailBeforeOpeningDatabase()
    {
        using var directory = new TestDirectory();
        var databasePath = Path.Combine(directory.Path, "gamelexicon.db");
        var factory = SqliteConnectionFactoryTests.CreateFactory(databasePath);

        Assert.Throws<ArgumentException>(() => new MigrationRunner(
            factory,
            [new RecordingMigration(1, []), new RecordingMigration(1, [])]));
        Assert.False(File.Exists(databasePath));
    }

    [Fact]
    public async Task FailedMigrationRollsBackAndStopsLaterMigrations()
    {
        using var directory = new TestDirectory();
        var calls = new List<int>();
        var factory = SqliteConnectionFactoryTests.CreateFactory(
            Path.Combine(directory.Path, "gamelexicon.db"));
        var runner = new MigrationRunner(
            factory,
            [new FailingMigration(), new RecordingMigration(2, calls)]);

        await Assert.ThrowsAsync<InvalidOperationException>(() => runner.RunAsync());

        Assert.Empty(calls);
        await using var connection = await factory.OpenConnectionAsync();
        Assert.Equal(
            0L,
            await SqliteConnectionFactoryTests.ExecuteScalarAsync<long>(
                connection,
                "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='should_rollback';"));
        Assert.Equal(
            0L,
            await SqliteConnectionFactoryTests.ExecuteScalarAsync<long>(
                connection,
                "SELECT COUNT(*) FROM schema_migrations WHERE version=1;"));
    }

    [Fact]
    public async Task DatabaseNewerThanApplicationIsRejectedWithoutDeletingVersion()
    {
        using var directory = new TestDirectory();
        var factory = SqliteConnectionFactoryTests.CreateFactory(
            Path.Combine(directory.Path, "gamelexicon.db"));
        await new MigrationRunner(factory, [new Migration001_Initial()]).RunAsync();
        await using (var connection = await factory.OpenConnectionAsync())
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO schema_migrations VALUES (2, '2026-08-01T00:00:00.0000000Z');";
            await command.ExecuteNonQueryAsync();
        }

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new MigrationRunner(factory, [new Migration001_Initial()]).RunAsync());

        Assert.Contains("newer", exception.Message, StringComparison.OrdinalIgnoreCase);
        await using var verification = await factory.OpenConnectionAsync();
        Assert.Equal(
            1L,
            await SqliteConnectionFactoryTests.ExecuteScalarAsync<long>(
                verification,
                "SELECT COUNT(*) FROM schema_migrations WHERE version=2;"));
    }

    [Fact]
    public async Task CancellationStopsBeforeDatabaseIsCreated()
    {
        using var directory = new TestDirectory();
        var databasePath = Path.Combine(directory.Path, "gamelexicon.db");
        var factory = SqliteConnectionFactoryTests.CreateFactory(databasePath);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            new MigrationRunner(factory, [new Migration001_Initial()])
                .RunAsync(cancellation.Token));
        Assert.False(File.Exists(databasePath));
    }

    private sealed class RecordingMigration(int version, List<int> calls) : IDatabaseMigration
    {
        public int Version => version;

        public Task ApplyAsync(
            SqliteConnection connection,
            SqliteTransaction transaction,
            CancellationToken cancellationToken)
        {
            calls.Add(Version);
            return Task.CompletedTask;
        }
    }

    private sealed class FailingMigration : IDatabaseMigration
    {
        public int Version => 1;

        public async Task ApplyAsync(
            SqliteConnection connection,
            SqliteTransaction transaction,
            CancellationToken cancellationToken)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "CREATE TABLE should_rollback (id INTEGER PRIMARY KEY);";
            await command.ExecuteNonQueryAsync(cancellationToken);
            throw new InvalidOperationException("Intentional migration failure.");
        }
    }
}
