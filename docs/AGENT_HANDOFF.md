# Agent Handoff

## Current task

- Task ID: `M1-T09`
- Name: SQLite queries and lifecycle
- Status: Done
- Primary domain: Infrastructure / Persistence / Vocabulary Queries
- Primary agent: primary coordinator
- Supporting agents: none; read-only semantic review was completed by the primary coordinator under the current no-delegation constraint

## Repository implementation

- Added `SqliteVocabularyRepository.Queries.cs`; the merged `public sealed partial` type now implements the complete `IVocabularyRepository`.
- Preserved the M1-T08 constructor, connection factory, and `SaveAsync` without changing write logic.
- `FindByNormalizedHeadwordAsync` performs exact active-only lookup on caller-provided normalized text without trimming, case conversion, or normalization.
- `GetDetailsAsync` reads the complete entry, examples, and tags in one connection and one read transaction; active and archived entries are supported.
- `SearchAsync` reads Count, the base page, Primary examples, and Tags in one connection and one read transaction. The page query is not joined to association tables.

## Search and aggregation semantics

- SearchText is an escaped literal substring LIKE over headword, normalized headword, part of speech, phonetic, English definition, Chinese translation, and notes.
- SearchText does not search sentence text, game title, or tag name; it is not trimmed or normalized.
- GameTitle is an exact `COLLATE NOCASE` EXISTS filter over linked examples.
- TagIds use ALL semantics through parameterized EXISTS predicates; all non-null filters combine with AND.
- Archive and EntryType filters map directly to persisted values.
- UpdatedAt, Headword, and CreatedAt sorts all append Id ASC for stable pagination; Offset is computed as checked long.
- Summary Primary fields are null for zero Primary rows, populated for exactly one, and fail on multiple Primary rows. Tags are complete and stably sorted.

## Validation

- Added 33 Infrastructure query test cases; Infrastructure tests pass 127/127.
- Root solution tests pass 299/299: Domain 111, Application 61, Infrastructure 127.
- Infrastructure and root builds pass with 0 warnings and 0 errors.
- Tests use real temporary databases migrated through Version 1 and 2 and verify DB/WAL/SHM deletion.
- Covered interface shape, exact Find behavior, full/corrupt mapping, active/archived Details, manual/Capture/OCR examples, tags, zero/multiple Primary, all SearchText fields, LIKE escaping, GameTitle, Tag ALL, combined filters, archive/type filters, all sorting modes, pagination, cancellation, indexes, and resource release.
- Static audit found no `SELECT *`, query stub, unsupported placeholder, user-controlled SQL structure, normalization call, permanent delete, or logging of query values.
- Migration001 hash remains `1fd5546081fe87c479ebd21d52e26f7d1dfaa636`; Migration002 hash remains `d8ce250e24442ece38c231e3ae8286a4d0def4c5`.
- Godot was not launched and no Godot process remains.

## Files changed

- Added `src/GameLexicon.Infrastructure/Persistence/Repositories/SqliteVocabularyRepository.Queries.cs`.
- Added `tests/GameLexicon.Infrastructure.Tests/Persistence/Repositories/SqliteVocabularyRepositoryQueryTests.cs`.
- Minimally updated `SqliteVocabularyRepositoryWriteTests.cs` to remove the now-obsolete M1-T08-stage assertion that the type had no query interface; write behavior tests and production SaveAsync remain unchanged.
- Updated `docs/IMPLEMENTATION_STATUS.md`, this handoff, and `docs/DECISIONS.md` (ADR-008).
- `docs/ENVIRONMENT.md` was not changed because no environment fact changed.

## Scope exclusions

- No Domain, Application contract/query model, migration, existing sentence/tag Repository, vocabulary write logic, Godot, project reference, target framework, package, index, FTS, permanent delete, UseCase, UI, or M1-T10 implementation.

## Skills used and impact

- Used: `project-routing`, `milestone-workflow`, `ugit-workflow`, `skill-maintenance`.
- Skill update required: No. M1-T09 applies bounded repository-query techniques without changing reusable routing, workflow, or safety guidance.

## Decision review

- Added ADR-008 because SearchText field scope, exact GameTitle matching, and TagIds ALL semantics are durable contracts for later UseCases and UI.

## Manual verification

- GUI verification is not applicable and Godot was not launched.
- Non-GUI review completed and passed on 2026-08-02.
- The review confirmed the complete interface and four real methods, unchanged M1-T08 SaveAsync logic, exact active Find semantics, consistent Details/Search snapshots, complete example/tag aggregation, literal LIKE escaping, GameTitle and Tag filtering, stable sorting and pagination, Primary corruption handling, parameterization, cancellation, resource disposal, ADR-008 alignment, migration integrity, test results, and scope exclusions.

## Next allowed action

- Review and commit the completed M1-T09 changes with UGit.
- `M1-T10: Manual vocabulary creation UseCase` remains Not Started and must not be executed automatically.
