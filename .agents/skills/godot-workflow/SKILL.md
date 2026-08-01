---
name: godot-workflow
description: Verify and maintain the existing GameLexicon Godot 4.7.1 .NET project, GodotSharp environment, C# integration, scenes, project.godot, Godot csproj, builds, editor runs, and headless validation. Use for Godot work; do not use for unrelated domain logic or Git-only tasks.
---

# Instructions

1. Read actual machine paths from `docs/ENVIRONMENT.md`; do not copy absolute paths into this Skill.
2. Read `AGENTS.md`, `docs/PRODUCT_SPEC.md`, `docs/IMPLEMENTATION_STATUS.md`, `docs/DECISIONS.md`, and the active milestone instruction.
3. Protect `english-learning-project/`: do not move, rename, duplicate, or edit it with two editor instances.
4. Verify, in order: .NET executable and console executable, exact 4.7.1/mono identity, GodotSharp contents, x64 architecture, installed SDKs, Git stop conditions, and a single `project.godot`.
5. Review `project.godot`, generated Godot `.csproj`, C# scripts, then `.tscn` resources and project references.
6. Preserve Godot-generated SDK and target framework. Do not hand-forge a Godot project file or force `net10.0` without compatibility evidence.
7. Validate with the active instruction's build/test commands, console headless editor command, and—when authorized—actual GUI run.
8. Treat GUI visibility as manual evidence; never claim it from headless output.

Never modify the Godot installation directory or Steam settings. Stop on edition/version/architecture mismatch, unsafe user changes, multiple projects, or target-framework incompatibility.

Update `docs/ENVIRONMENT.md` for machine paths/versions, `docs/DECISIONS.md` for durable architectural choices, and this Skill only for reusable workflow changes.
