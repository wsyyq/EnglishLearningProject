# Decisions

## ADR-001: Primary agent is the only default writer

- Status: Accepted
- Context: all agents share one worktree.
- Decision: the primary agent owns repository writes, validation, status updates, and completion decisions.
- Reason: prevent conflicting edits and preserve user changes.
- Consequence: specialists return read-only evidence and proposals.

## ADR-002: Specialists are read-only by default

- Status: Accepted
- Context: specialist value is investigation and review.
- Decision: all four project specialist TOMLs use `sandbox_mode = "read-only"`.
- Reason: parallel analysis should not create concurrent writes.
- Consequence: the primary agent applies approved recommendations.

## ADR-003: Shared state lives in repository documents

- Status: Accepted
- Context: chat context is not a durable project database.
- Decision: status, environment, decisions, routing, handoff, catalog, and Skill history are documented under `docs/`.
- Reason: future sessions need inspectable evidence.
- Consequence: keep documents current and concise; do not paste terminal logs.

## ADR-004: Skills contain reusable workflows only

- Status: Accepted
- Context: code and task facts change more often than workflow policy.
- Decision: Skills contain triggers, procedures, safety boundaries, and source routing—not implementation logs or duplicated specifications.
- Reason: reduce drift and context cost.
- Consequence: ordinary code changes usually do not modify Skills.

## ADR-005: Skill changes require impact review

- Status: Accepted
- Context: automatic Skill rewrites can weaken safety or create noise.
- Decision: every modifying task performs Skill Impact Review; only evidence-backed reusable changes update Skills, catalog, and changelog.
- Reason: keep maintenance intentional and auditable.
- Consequence: `skill_curator` reviews proposed reusable changes read-only.

## ADR-006: Agent/Skill changes require a new session

- Status: Accepted
- Context: AGENTS and discovery metadata are generally loaded at session start.
- Decision: after changing AGENTS, Agent TOML, or Skills, tell the user to restart or open a new Codex session.
- Reason: current-session hot reload is not guaranteed.
- Consequence: do not claim new routing is fully active until a new session verifies it.
