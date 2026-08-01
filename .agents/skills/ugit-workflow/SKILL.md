---
name: ugit-workflow
description: Inspect and safely handle GameLexicon Git or UGit state, ignores, diffs, branches, remotes, commits, push failures, proxies, authentication, permissions, and recovery. Use for source-control tasks; do not use for application or Godot implementation.
---

# Instructions

1. Read `AGENTS.md`, `docs/IMPLEMENTATION_STATUS.md`, `docs/ENVIRONMENT.md`, and `docs/DECISIONS.md`.
2. Inspect `git status --short --branch`, repository root, branches, remotes, and relevant diffs before proposing mutations.
3. Preserve user changes. Distinguish tracked, untracked, ignored, staged, and generated files.
4. Review `.gitignore` with `git check-ignore -v` and avoid broad rules that hide source assets.
5. Before a checkpoint or commit, confirm scope, build/test evidence, staged paths, diff, and message. Never infer authorization to commit or push.
6. Diagnose push failures separately as network/DNS, proxy, authentication, permission, remote configuration, or repository-state failures.
7. Give safe commands, expected output, risk, and rollback.

Never run destructive history or cleanup operations—including `reset --hard`, `clean -fd`, force push, rebase, branch deletion, or remote deletion—without explicit authorization and exact target verification. Never store credentials, tokens, or unverified proxy endpoints.

Final Git report: root and branch; status; files changed/staged; commands; validation; remote action result; warnings and rollback.

Review `docs/ENVIRONMENT.md` when machine-specific Git facts change, `docs/DECISIONS.md` when policy changes, and this Skill only when the reusable workflow changes.
