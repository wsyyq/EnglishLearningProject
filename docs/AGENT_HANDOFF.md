# Agent Handoff

## Current task

- Task ID: `M1-T01`
- Status: Done
- Primary domain: Infrastructure / Persistence
- Primary agent: primary coordinator
- Supporting agents: `godot_specialist`, `milestone_architect`, `skill_curator`

## Scope implemented

- Infrastructure references `Microsoft.Data.Sqlite 8.0.29` and the audited native runtime bundle `SQLitePCLRaw.bundle_e_sqlite3 3.0.5`.
- Added `DatabaseOptions`, `SqliteConnectionFactory`, `IDatabaseMigration`, `MigrationRunner`, `MigrationResult`, and `Migration001_Initial`.
- Godot resolves `user://data/gamelexicon.db` and awaits migration completion before initializing M0-T03 navigation.
- No Repository, CRUD, text normalization, database UI, or M1-T02 behavior was implemented.

## Database behavior

- Logical path: `user://data/gamelexicon.db`.
- Each open returns a distinct non-pooled connection with Foreign Keys enabled and a 5000 ms Busy Timeout.
- WAL is enabled once per factory initialization and verified against a real file database.
- Runner validates positive, unique versions, executes pending migrations in ascending order, and rejects databases newer than the application.
- Every migration has its own transaction containing both schema changes and the `schema_migrations` insert; failure or cancellation rolls back and prevents later migrations.
- The migration interface includes an explicit `SqliteTransaction` parameter so every command is attached to the Runner-owned transaction.

## Schema version 1

- Runner creates `schema_migrations` with UTC ISO 8601 timestamps.
- Migration001 creates: captures, ocr_regions, ocr_tokens, sentence_examples, vocabulary_entries, entry_examples, tags, entry_tags, review_cards, review_logs, and app_settings.
- It creates the three required indexes: `ux_vocabulary_entries_normalized_active`, `ux_review_cards_entry_type`, and `ix_review_cards_due`.
- Foreign-key enforcement, `RESTRICT` behavior, and unique/partial-unique constraints match the instruction and are exercised by tests.
- `app_settings` is reserved only; M0-T04 JSON remains the active settings source and no synchronization was added.

## Validation

- Baseline: clean worktree, M0-T04 commit `65f846f` present, eight projects, expected TFMs, no Godot process, build passed, tests 21/21 passed.
- Infrastructure tests: 33/33 passed. Root total: 35/35 passed, 0 failed, 0 skipped.
- Godot and the eight-project root solution build with 0 errors. Existing `NU1900` can occur while the NuGet vulnerability source is temporarily unreachable; Audit was not disabled.
- An initial `NU1903` for the old SQLitePCLRaw 2.1.6 native dependency was resolved by pinning bundle 3.0.5, which supplies SQLite 3.53.4. Final NuGet Audit reports no known vulnerable packages.
- Godot 4.7.1 .NET headless editor build passed and the native SQLite Provider loaded in runtime.
- First headless launch created a 122,880-byte database and applied Migration 1.
- Second headless launch reused the database, did not reapply Migration 1, and reported schema current 1.
- Migration-applied event count is one; schema-current event count is two. Both launches initialized Dashboard/navigation and exited without a residual Godot process.
- Temporary test databases, WAL/SHM sidecars, and directories were deleted successfully.

## Files changed

- Added Infrastructure persistence and migration source files.
- Added Infrastructure persistence tests.
- Modified only the Infrastructure project package references and the Godot `AppServices`/`AppRoot` startup composition.
- Updated implementation status and this handoff.
- `docs/ENVIRONMENT.md` was not changed because package/schema facts are implementation facts, not machine environment changes.

## Manual verification

- Completed on 2026-08-01 and passed.
- Both launches initialized normally; Dashboard, all six routes, Settings, logging, and development-mode persistence passed without regression.
- No C#, resource, SQLite Provider, connection, migration, duplicate-table, or database-lock errors occurred.
- `user://data/gamelexicon.db` exists and is nonzero; it does not appear in Git status.
- Migration 1 was applied once, and the second launch reported schema current without reapplying it.
- Logs contained no full connection string, database content, learning text, API key, token, or password.
- The application closed normally and left no Godot process.

## Skills used and impact

- Used: `project-routing`, `milestone-workflow`, `godot-workflow`, `skill-maintenance`.
- Skill update required: No. The dependency security pin, migration transaction behavior, temporary-database cleanup, and two-run headless validation are M1-T01-specific implementation and acceptance facts; they do not change reusable routing or workflow guidance.

## Next allowed action

- Review and commit the completed M1-T01 changes through UGit when approved.
- `M1-T02` remains Not Started and must not be executed automatically.
