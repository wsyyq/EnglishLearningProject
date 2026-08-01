# Agent Handoff

## Current task

- Task ID: `M1-T04`
- Name: Persistence interfaces and query contracts
- Status: Done
- Primary domain: Application / Persistence Contracts
- Primary agent: primary coordinator
- Supporting agents: none

## Repository contracts

- Added `IVocabularyRepository` with exactly the four product-specification methods: active normalized-headword lookup, details, paged search, and save.
- Added `ISentenceExampleRepository` for example reads/saves, link saves/removal, and an explicitly atomic `SetPrimaryAsync` operation.
- Added `ITagRepository` for normalized-name lookup, idempotent get-or-create, entry tag reads, and atomic replacement through `SetForEntryAsync`.
- Every asynchronous persistence method ends with a required `CancellationToken`.
- Public Repository APIs expose only Domain types, Application query types, BCL types, Task, CancellationToken, and read-only collections.
- No permanent-delete method was added; lifecycle extension remains deferred until M1-T09/M1-T13 requirements are implemented.

## Query contracts

- Added immutable `PagedResult<T>` with defensive item copies, long-safe page counts, and navigation flags.
- Added immutable `VocabularySearchQuery` supporting search text, game title, tag IDs, optional EntryType, archive filter, sort order, page number, and page size.
- Defaults are ActiveOnly, UpdatedAtDescending, page 1, size 50. Page size is limited to 1–200.
- Search and game text are preserved exactly; the query rejects blank filters but performs no trimming, case conversion, Form KC, or normalization.
- Tag IDs reject empty or duplicate values and are defensively copied.
- No Review/M6 status filter exists.

## Read models

- Added `VocabularyEntrySummary`, `VocabularyEntryDetails`, `SentenceExampleDetails`, and `TagSummary`.
- Read models copy scalar state rather than retain mutable Domain entity references.
- Collection inputs are defensively copied; duplicate tag/example IDs are rejected.
- Details sort examples by SortOrder and Id, allow zero primary examples, and reject multiple primary examples.
- Sentence details require matching Domain example/link IDs and copy manual-example nullable Capture state and UTF-16 target information.

## Validation

- Baseline: clean worktree, M1-T03 commit `decfb68cdf7990c84047d350a25f98606ec2a054` present, main branch, eight projects, expected TFMs, no Godot process, build 0 warnings/errors, tests 145/145.
- Application and Application.Tests builds passed with 0 warnings and 0 errors.
- Application tests: 61/61 passed (60 added cases), 0 failed, 0 skipped.
- Eight-project root solution build passed with 0 warnings and 0 errors.
- Root tests: 205/205 passed, 0 failed, 0 skipped.
- Reflection tests verify the four vocabulary methods, Task-based signatures, final CancellationToken parameters, and absence of SQLite, Godot, Infrastructure, System.Data, and IQueryable public types.
- No restore or package change was required; NuGet Audit remains enabled.

## Files changed

- Added three interfaces under `src/GameLexicon.Application/Abstractions/Persistence/`.
- Added eight query/read-model files under `src/GameLexicon.Application/Entries/Queries/`.
- Added four Application test files covering public contracts, paging, search queries, and read models.
- Updated `docs/IMPLEMENTATION_STATUS.md` and this handoff.
- `docs/DECISIONS.md` and `docs/ENVIRONMENT.md` were not changed because no new durable policy or machine environment fact was introduced.

## Scope exclusions

- No Domain, Infrastructure, migration, database, Godot, project-reference, or package change.
- No Migration002, SQL, Repository implementation, UseCase, dependency registration, UI, or Review/M6 query behavior.
- M1-T05 was not executed.

## Skills used and impact

- Used: `project-routing`, `milestone-workflow`, `skill-maintenance`.
- Skill update required: No. M1-T04 adds ordinary Application persistence and query contracts; reusable routing, workflow steps, safety rules, and acceptance procedures did not change.

## Manual verification

- Non-GUI review completed on 2026-08-01 and passed; GUI verification was not applicable and Godot was not launched.
- The user confirmed interface scope, active lookup semantics, CancellationToken coverage, public API boundaries, atomic primary/tag semantics, immutable paging/query/read models, defensive copies, validation behavior, no-normalization behavior, and scope exclusions.
- The user confirmed Application tests passed 61/61, root tests passed 205/205, and the Git modification scope contains only M1-T04 work.

## Next allowed action

- Review and commit the completed M1-T04 changes through UGit when approved.
- `M1-T05: Migration002 manual examples and query support` remains Not Started and must not be executed automatically.
