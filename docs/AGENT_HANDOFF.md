# Agent Handoff

## Current task

- Task ID: `M1-T03`
- Name: Entry and sentence-example domain models
- Status: Done
- Primary domain: Domain / Entries
- Primary agent: primary coordinator
- Supporting agents: none

## Scope implemented

- Added `EntryType`, `VocabularyEntry`, `SentenceExample`, `EntryExampleLink`, and `Tag` under `GameLexicon.Domain/Entries`.
- Added an internal `EntryGuard` only to centralize identifier, required-text, and UTC validation.
- Added five Domain test files under `GameLexicon.Domain.Tests/Entries`.
- No Repository, DTO, query, SQL, migration, UseCase, Godot, UI, duplicate handling, sentence splitting, or target relocation was implemented.

## Domain invariants

- `EntryType` values are fixed: Word=0, Phrase=1, Expression=2, SentencePattern=3; undefined values are rejected.
- Entity and link identifiers reject `Guid.Empty`.
- Required text rejects null, empty, and whitespace-only values without echoing content in production exceptions.
- Persisted timestamps must have UTC offset zero. `VocabularyEntry.UpdatedAt` cannot precede `CreatedAt` or move backwards from its current value.
- Mutation methods validate every argument before changing state, so failed updates leave the object unchanged.
- Models accept already-normalized fields without duplicating M1-T02 normalization or modifying supplied text.

## Sentence-example rules

- `CaptureId` is nullable so a manual example may exist without a screenshot; `OcrRegionId` requires a nonempty `CaptureId`.
- `TargetStart` and `TargetLength` use .NET UTF-16 code-unit indices and `Substring` semantics.
- Targets must be nonempty, in bounds, and must not split a UTF-16 surrogate pair.
- Migration001 still has non-null `capture_id`; it was not modified. ADR-007 records that M1-T05 must resolve the mismatch with a new migration.
- A single-primary invariant across multiple `EntryExampleLink` instances is deferred to the Repository transaction boundary.

## Representative verification

- Valid VocabularyEntry creation: passed; empty ID/headword, undefined EntryType, invalid UTC/order, and backwards updates were rejected.
- Manual SentenceExample without Capture: passed; OCR without Capture and invalid source IDs were rejected.
- Start, middle, end, and multiword UTF-16 ranges passed; out-of-range and surrogate-splitting ranges were rejected; an emoji prefix correctly placed `"Get out"` at UTF-16 index 3.
- EntryExampleLink accepted SortOrder 0, rejected negative values, and allowed primary-state changes.
- Tag accepted valid names, rejected empty normalized names, and preserved caller-provided rename values without normalizing them.

## Validation

- Baseline: clean worktree, M1-T02 commit `4793f73b175c9d72df7706616679b907149e6c0b` present, main branch, eight projects, expected TFMs, no Godot process, build 0 warnings/errors, tests 75/75.
- Domain and Domain.Tests builds passed with 0 warnings and 0 errors.
- Domain tests: 111/111 passed (70 added cases), 0 failed, 0 skipped.
- Eight-project root solution build passed with 0 warnings and 0 errors.
- Root tests: 145/145 passed, 0 failed, 0 skipped.
- No restore or package change was required; NuGet Audit remains enabled.

## Files changed

- Added six Domain files under `src/GameLexicon.Domain/Entries/`.
- Added five Domain test files under `tests/GameLexicon.Domain.Tests/Entries/`.
- Updated `docs/IMPLEMENTATION_STATUS.md`, `docs/DECISIONS.md`, and this handoff.
- `docs/ENVIRONMENT.md` was not changed because no machine environment fact changed.

## Skills used and impact

- Used: `project-routing`, `milestone-workflow`, `skill-maintenance`.
- Skill update required: No. The Guid, UTC, UTF-16 range, and atomic-mutation rules are M1-T03 domain decisions already captured in task documentation and ADR-007; reusable routing and workflow guidance did not change.

## Manual verification

- Non-GUI review completed on 2026-08-01 and passed; GUI verification was not applicable and Godot was not launched.
- The user confirmed the five-model scope, fixed enum values, Guid/UTC/timestamp invariants, atomic mutations, nullable Capture/OCR source rule, UTF-16 and surrogate boundaries, exception privacy, and dependency boundaries.
- The user confirmed Domain tests passed 111/111, root tests passed 145/145, Migration001 was unchanged, ADR-007 is retained, and no Migration002, Repository, UseCase, Godot, or UI work was included.

## Next allowed action

- Review and commit the completed M1-T03 changes through UGit when approved.
- `M1-T04: Persistence interfaces and query contracts` remains Not Started and must not be executed automatically.
