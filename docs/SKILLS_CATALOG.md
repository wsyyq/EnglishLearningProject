# Project Skills Catalog

## Skills

### project-routing

- Path: `.agents/skills/project-routing/SKILL.md`
- Trigger: start of every GameLexicon repository task; task classification and delegation.
- Do not trigger: unrelated work outside this repository.
- Sources: `AGENTS.md`, `docs/AGENT_SYSTEM.md`, `docs/IMPLEMENTATION_STATUS.md`.
- Agent: primary coordinator; routes to all specialists.
- Maintain when: Agent ownership, routing categories, or single-writer policy changes.
- Last review task: `META-T01`.

### ugit-workflow

- Path: `.agents/skills/ugit-workflow/SKILL.md`
- Trigger: Git/UGit status, ignores, diffs, commits, remotes, push, proxy, authentication, permissions, and recovery.
- Do not trigger: application or Godot implementation without a source-control issue.
- Sources: `AGENTS.md`, `docs/ENVIRONMENT.md`, `docs/DECISIONS.md`.
- Agent: `ugit_manager`.
- Maintain when: reusable Git safety or diagnostic workflow changes.
- Last review task: `META-T01`.

### godot-workflow

- Path: `.agents/skills/godot-workflow/SKILL.md`
- Trigger: Godot 4.7.1 .NET, C#, GodotSharp, project/scene review, builds, GUI, and headless validation.
- Do not trigger: pure Domain logic or Git-only work.
- Sources: `docs/PRODUCT_SPEC.md`, `docs/ENVIRONMENT.md`, `docs/DECISIONS.md`, active milestone instruction.
- Agent: `godot_specialist`.
- Maintain when: reusable Godot validation, path-source, or project protection rules change.
- Last review task: `META-T01`.

### milestone-workflow

- Path: `.agents/skills/milestone-workflow/SKILL.md`
- Trigger: product planning, one-task milestone instructions, scope, stop conditions, and acceptance review.
- Do not trigger: task implementation or automatic milestone advancement.
- Sources: `docs/PRODUCT_SPEC.md`, `docs/IMPLEMENTATION_STATUS.md`, `docs/MT_INSTRUCTION/`.
- Agent: `milestone_architect`.
- Maintain when: reusable task template or acceptance process changes.
- Last review task: `META-T01`.

### skill-maintenance

- Path: `.agents/skills/skill-maintenance/SKILL.md`
- Trigger: post-change Skill Impact Review and Agent/Skill/routing maintenance.
- Do not trigger: rewriting Skills for ordinary implementation details.
- Sources: `AGENTS.md`, `docs/AGENT_SYSTEM.md`, this catalog, `docs/SKILL_CHANGELOG.md`.
- Agent: `skill_curator`.
- Maintain when: Skill impact criteria, validation, or refresh rules change.
- Last review task: `META-T01`.

## Source impact map

| Source change | Skills to review |
|---|---|
| Git rules, `.gitignore`, remote or push workflow | `ugit-workflow` |
| Godot paths, versions, build, scene, or validation rules | `godot-workflow` |
| PRODUCT_SPEC, task template, milestone acceptance | `milestone-workflow` |
| Agent config, AGENTS, routing categories | `project-routing`, `skill-maintenance` |
| Skill maintenance or refresh behavior | `skill-maintenance` |

“Review” does not mean “modify.” Update a Skill only when reusable workflow knowledge changes.
