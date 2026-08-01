---
name: skill-maintenance
description: Perform the GameLexicon Skill Impact Review after repository-changing tasks and maintain project Skills, routing, catalog, and changelog when reusable workflow knowledge changes. Use for Skill or Agent maintenance; do not rewrite Skills for ordinary implementation details.
---

# Instructions

After every task that changes repository files:

1. List the Skills actually used.
2. Review Git status and diff.
3. Decide whether reusable workflow knowledge changed: steps, fixed commands, path sources, prerequisites, stop conditions, safety rules, acceptance criteria, Agent routing, or source documents.
4. If no reusable change occurred, do not modify Skills and report `Skill update required: No`.
5. If reusable change occurred, request a read-only `skill_curator` review, apply only the smallest justified update, and update `docs/SKILLS_CATALOG.md` and `docs/SKILL_CHANGELOG.md`.
6. Validate frontmatter, unique names, nonempty bodies, referenced paths, routing consistency, TOML where applicable, and `git diff --check`.
7. Do not copy task logs, full diffs, code details, or large product specifications into Skills.
8. Keep descriptions precise enough for routing and preserve all safety rules.
9. After Agent, AGENTS, or Skill changes, tell the user to restart or open a new Codex session.

Use `docs/ENVIRONMENT.md` as the single source for machine-specific paths and `docs/DECISIONS.md` for durable policy.
