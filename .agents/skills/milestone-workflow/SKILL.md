---
name: milestone-workflow
description: Plan, generate, and review one bounded GameLexicon milestone task from PRODUCT_SPEC and current repository status. Use for M0-TXX instructions, product scope, prerequisites, stop conditions, and acceptance reviews; do not use to implement the task or advance automatically.
---

# Instructions

1. Read `docs/PRODUCT_SPEC.md` as product and acceptance source of truth.
2. Read `docs/IMPLEMENTATION_STATUS.md`, `docs/ENVIRONMENT.md`, `docs/DECISIONS.md`, current Git state, and applicable `docs/MT_INSTRUCTION/` files. Never infer current state from chat alone.
3. Define exactly one task with: Task ID, objective, required reading, fixed paths, prerequisites, ordered phases, mandatory stop conditions, allowed files, explicit exclusions, build commands, test commands, automated acceptance, manual acceptance, status update, and final report format.
4. Keep platform code outside Domain and SQL/OCR/Win32 execution outside UI.
5. Review completion against command evidence, diff scope, tests, manual validation, limitations, and documentation updates.
6. Leave the next task Not Started or Blocked as evidence requires. Never execute it automatically.

Keep milestone instructions specific while avoiding duplicated product prose. Update this Skill only when the reusable milestone process changes.
