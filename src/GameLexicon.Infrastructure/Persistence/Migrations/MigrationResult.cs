namespace GameLexicon.Infrastructure.Persistence.Migrations;

public sealed record MigrationResult(
    int CurrentVersion,
    IReadOnlyList<int> AppliedVersions);
