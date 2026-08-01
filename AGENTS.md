# GameLexicon Codex Instructions

## Repository root

The repository root is:

`D:\UGit\EnglishLearningProject`

The existing Godot project is currently located at:

`english-learning-project/`

Do not move or rename the existing Godot project unless a task explicitly
requires it.

Do not create project files inside the Godot editor installation directory.

## Required reading

Before planning or modifying this repository, read:

- `docs/PRODUCT_SPEC.md`
- `docs/IMPLEMENTATION_STATUS.md`, if it exists

Treat `docs/PRODUCT_SPEC.md` as the product, architecture, milestone, and
acceptance-criteria source of truth.

## Technical baseline

- Godot version: Godot 4.7.1 .NET
- Primary language: C#
- MVP platform: Windows 10/11 x64
- Godot project directory: `english-learning-project/`
- Architecture:
  - Godot desktop application
  - Windows CaptureBridge
  - Local Tesseract OCR
  - SQLite
- The application must remain offline-first.
- Do not inject code into games.
- Do not access game process memory.
- Do not implement anti-cheat bypasses.

## Repository rules

- Work on only one task at a time.
- Do not attempt to implement the entire product in one run.
- Before editing, inspect the repository and current Git status.
- Preserve existing user files and uncommitted changes.
- Do not modify files outside this repository.
- Do not move `english-learning-project/` without explicit permission.
- Do not put SQL directly in Godot UI scripts.
- Do not put Win32 calls directly in Godot UI scripts.
- Do not put OCR process execution directly in Godot UI scripts.
- Keep Domain independent of Godot, SQLite, Windows, and OCR implementations.
- Add or update tests for each implemented behavior.
- Do not add unrequested production dependencies.
- Do not commit databases, screenshots, logs, API keys, local configuration,
  build output, or Godot cache files.

## Task workflow

Before implementation:

1. Read the required documentation.
2. Inspect relevant existing files.
3. Check Git status.
4. State the exact task scope.
5. List files expected to change.

During implementation:

1. Stay within the current task.
2. Keep architectural boundaries.
3. Add error handling.
4. Add relevant tests.

Before completion:

1. Build the solution.
2. Validate the Godot project when relevant.
3. Run relevant tests.
4. Review the Git diff.
5. Update `docs/IMPLEMENTATION_STATUS.md`.

## Completion report

The final response for each task must include:

- Task ID and task name
- Files created
- Files modified
- Commands executed
- Build result
- Test result
- Manual verification performed
- Known limitations
- Suggested next task

A task is not complete when the build or relevant tests fail.

## Default coordination role

The primary Codex agent is the project coordinator.

The primary agent owns:

- task classification
- subagent delegation
- final repository writes
- build and test execution
- Git diff review
- `docs/IMPLEMENTATION_STATUS.md` updates
- final completion decisions

Professional agents are read-only by default. The primary agent remains the only default writer in the shared workspace.

## Mandatory task routing

At the beginning of every repository task:

1. Apply the `project-routing` Skill.
2. Read `docs/IMPLEMENTATION_STATUS.md`.
3. Read `docs/ENVIRONMENT.md`.
4. Classify the task as Git, Godot, milestone/product, Skill/Agent, or cross-domain.
5. Invoke the matching Skill.
6. Delegate read-heavy specialist analysis when it improves correctness.
7. Keep the primary agent as the only default writer.

Route Git and UGit work to `ugit_manager`, Godot/C#/scene work to `godot_specialist`, milestone/product work to `milestone_architect`, and Skill/Agent maintenance to `skill_curator`. For independent cross-domain analysis, specialists may work in parallel read-only; never allow parallel writes to the same worktree.

## Mandatory post-change review

After every task that changes repository files:

1. Run the relevant build, tests, and validation.
2. Review Git status and diff.
3. Update applicable project status and `docs/AGENT_HANDOFF.md`.
4. Apply the `skill-maintenance` Skill.
5. Report Skills used.
6. Report whether a Skill update was required.
7. If reusable workflow changed, update the affected Skill, `docs/SKILLS_CATALOG.md`, and `docs/SKILL_CHANGELOG.md`.
8. If AGENTS, Agent configs, or Skills changed, tell the user to restart or start a new Codex session.

Do not modify a Skill merely to record an ordinary code change. Do not mechanically rewrite all Skills, turn Skills into code/task logs, or weaken safety rules. Modify Agent TOML only when responsibilities change and `project-routing` only when routing changes. For uncertain Skill changes, record a proposal instead of deleting an existing rule.

## Agent and Skill report

Final reports for repository tasks must include:

- Primary domain
- Primary agent
- Supporting agents
- Skills used
- Skill impact review
- Skills updated
- Documentation updated
- Reload/restart requirement

Project Agent and Skill configuration is generally discovered at session start; do not claim current-session changes are immediately fully active.
