---
name: project-routing
description: Route GameLexicon repository tasks to the primary agent, the appropriate read-only specialist, and the minimum project Skill set. Use at the start of every repository task; do not use for unrelated work outside this repository.
---

# Instructions

1. Read `docs/IMPLEMENTATION_STATUS.md` and `docs/ENVIRONMENT.md`.
2. Classify the request before modifying files.
3. Load the matching Skill and delegate read-heavy analysis when it improves correctness.
4. Keep the primary agent as the only default writer, validator, status updater, and completion decision-maker.

| Task type | Primary/specialist agent | Required Skill |
|---|---|---|
| Git, UGit, commit, push, remote, proxy, `.gitignore` | `ugit_manager` | `ugit-workflow` |
| Godot, C#, scenes, nodes, GodotSharp, headless, engine paths | `godot_specialist` | `godot-workflow` |
| Product design, game features, M0-TXX, acceptance, task decomposition | `milestone_architect` | `milestone-workflow` |
| Skill, Agent, AGENTS, routing, knowledge maintenance | `skill_curator` | `skill-maintenance` |
| Independent cross-domain analysis | Primary agent coordinates read-only specialists | All relevant Skills |

- Use one specialist for a single-domain task.
- Run specialists in parallel only when their analyses are independent.
- Never let multiple agents write the same workspace concurrently.
- Wait for specialist findings before deciding changes.
- Do not mechanically claim identities or files were read; report the actual routing result at completion.
