using Microsoft.Data.Sqlite;

namespace GameLexicon.Infrastructure.Persistence.Migrations;

public interface IDatabaseMigration
{
    int Version { get; }

    Task ApplyAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        CancellationToken cancellationToken);
}
