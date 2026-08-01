# GameLexicon Agent System

## Operating model

The primary Codex agent is the project coordinator and only default writer. It classifies tasks, delegates read-heavy analysis, resolves conflicts, applies repository changes, runs validation, updates status, and decides completion.

Specialists are read-only by default:

- `ugit_manager`: Git/UGit state, ignores, remotes, push diagnostics, and safe recovery.
- `godot_specialist`: Godot 4.7.1 .NET, GodotSharp, C#, scenes, engine paths, and validation.
- `milestone_architect`: product scope, M0-TXX instructions, stop conditions, and acceptance reviews.
- `skill_curator`: reusable workflow impact, Skill drift, catalog, and changelog review.

Read-only specialists reduce conflicting edits in the shared worktree. Their findings return to the primary agent as evidence and recommendations; specialists do not mark tasks Done.

## Routing flow

1. Apply `project-routing` and read current status/environment.
2. Classify the primary domain.
3. Load the matching Skill and use one specialist for a single-domain task.
4. Use parallel specialists only for independent cross-domain analysis.
5. Wait for findings, then let the primary agent decide and write.
6. Validate, update status/handoff, and run `skill-maintenance` after modifications.

Use serial analysis when one result changes the next scope, when user changes overlap, or when a stop condition must be resolved first. Never allow concurrent writes to the same workspace.

## Conflict priority

1. User instruction and applicable repository safety rules.
2. `docs/PRODUCT_SPEC.md` and active milestone instruction.
3. Current evidence from repository and environment.
4. `docs/DECISIONS.md` and `docs/IMPLEMENTATION_STATUS.md`.
5. Specialist recommendations. The primary agent resolves disagreements and records durable decisions when needed.

## Loading and refresh behavior

Applicable `AGENTS.md` files are generally loaded when a session starts; rereading every identity file before every reply is not guaranteed. Skill bodies load when explicitly invoked or when their descriptions match. After changing AGENTS, Agent TOML, or Skills, restart or open a new Codex session before relying on discovery.

## Manual routing examples

Ask the primary agent explicitly, for example:

- “Use `ugit_manager` with `ugit-workflow` to diagnose this push failure read-only.”
- “Use `godot_specialist` with `godot-workflow` to review M0-T02 prerequisites.”
- “Use `milestone_architect` to draft one M0-T03 instruction without implementing it.”
- “Use `skill_curator` to perform a Skill Impact Review of this diff.”

## Troubleshooting

- Missing Agent/Skill: confirm paths in `docs/SKILLS_CATALOG.md`, validate TOML/frontmatter, then start a new session.
- Wrong routing: inspect descriptions and `project-routing`; change only the smallest ambiguous rule.
- Conflicting advice: preserve stop conditions and current user changes; the primary agent requests clarification when evidence cannot resolve it.
- Stale machine path: update `docs/ENVIRONMENT.md`, then review `godot-workflow` without duplicating the path there.
