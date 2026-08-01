using GameLexicon.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;

namespace GameLexicon.Infrastructure.Tests.Persistence;

public sealed class SqliteConnectionFactoryTests
{
    [Fact]
    public async Task OpenCreatesParentDirectoryAndFileWithRequiredPragmas()
    {
        using var directory = new TestDirectory();
        var databasePath = Path.Combine(directory.Path, "nested", "gamelexicon.db");
        var factory = CreateFactory(databasePath);

        await using var connection = await factory.OpenConnectionAsync();

        Assert.True(File.Exists(databasePath));
        Assert.Equal(1L, await ExecuteScalarAsync<long>(connection, "PRAGMA foreign_keys;"));
        Assert.Equal(5000L, await ExecuteScalarAsync<long>(connection, "PRAGMA busy_timeout;"));
        Assert.Equal("wal", await ExecuteScalarAsync<string>(connection, "PRAGMA journal_mode;"));
    }

    [Fact]
    public async Task EveryOpenReturnsADifferentConnection()
    {
        using var directory = new TestDirectory();
        var factory = CreateFactory(Path.Combine(directory.Path, "gamelexicon.db"));

        await using var first = await factory.OpenConnectionAsync();
        await using var second = await factory.OpenConnectionAsync();

        Assert.NotSame(first, second);
        Assert.Equal(System.Data.ConnectionState.Open, first.State);
        Assert.Equal(System.Data.ConnectionState.Open, second.State);
    }

    [Fact]
    public async Task DisposeReleasesDatabaseAndSidecarsForDeletion()
    {
        using var directory = new TestDirectory();
        var databasePath = Path.Combine(directory.Path, "gamelexicon.db");
        var factory = CreateFactory(databasePath);

        await using (var connection = await factory.OpenConnectionAsync())
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "CREATE TABLE deletion_test (id INTEGER PRIMARY KEY);";
            await command.ExecuteNonQueryAsync();
        }

        DeleteIfExists(databasePath + "-wal");
        DeleteIfExists(databasePath + "-shm");
        File.Delete(databasePath);

        Assert.False(File.Exists(databasePath));
        Assert.False(File.Exists(databasePath + "-wal"));
        Assert.False(File.Exists(databasePath + "-shm"));
    }

    [Fact]
    public void InvalidPathsProduceClearExceptions()
    {
        Assert.Throws<ArgumentException>(() => new SqliteConnectionFactory(new DatabaseOptions
        {
            DatabasePath = ""
        }));

        using var directory = new TestDirectory();
        Assert.Throws<ArgumentException>(() => new SqliteConnectionFactory(new DatabaseOptions
        {
            DatabasePath = directory.Path
        }));
    }

    internal static SqliteConnectionFactory CreateFactory(string databasePath) =>
        new(new DatabaseOptions
        {
            DatabasePath = databasePath,
            EnableWriteAheadLogging = true,
            BusyTimeoutMilliseconds = 5000
        });

    internal static async Task<T> ExecuteScalarAsync<T>(SqliteConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        var value = await command.ExecuteScalarAsync();
        return (T)Convert.ChangeType(value!, typeof(T));
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
