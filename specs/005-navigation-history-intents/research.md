# Research: Navigation History and Vertical Movement

**Date**: 2026-10-21  
**Feature**: [Navigation History and Vertical Movement](./spec.md)

## Decision 1: Recognize commands in the deterministic intent path

**Decision**: Extend the shared deterministic direction vocabulary and classic command patterns to recognize `Up` and `Down` (bare and after the existing movement verbs). Recognize `back` as its own structured action with no target.

**Rationale**: The interpreter tries exact command patterns before its keyword fallback. The existing vocabulary contains only cardinal directions even though the domain direction enum and adventure exits already contain vertical directions. A deterministic match avoids an unnecessary model call and makes capitalization and output reproducible.

**Alternatives considered**:

- Rely on the natural-language classifier to infer vertical directions: rejected because exact commands such as `Down` are currently unrecognized and the fallback only detects direction words when paired with a movement keyword.
- Treat `back` as `Go` with a special target string: rejected because `back` is a different operation from selecting a direction and currently `Go` requires a target.

## Decision 2: Keep the previous-location stack in authoritative game state

**Decision**: Store previous locations on the Player as an ordered stack and include that stack in `PlayerSnapshot`. Applying an ordinary successful movement pushes the origin. Applying a validated backtracking event pops the most recent location and moves the player there. A failed action emits no event and leaves the stack unchanged.

**Rationale**: The stack represents authoritative navigation state needed for repeated `back` actions, so it must survive save/reload. A dedicated backtracking event distinguishes popping history from ordinary movement in the existing append-only event log.

**Alternatives considered**:

- Recompute the stack from movement events for every `back`: rejected because it makes each back action scan the full event history and complicates distinguishing normal movement from a return.
- Store only a previous-location pointer: rejected because the specification requires repeated `back` requests to return through multiple locations.

## Decision 3: Keep vertical movement on the existing exit rules

**Decision**: Use the existing `Go` action and movement validation for `Up` and `Down`. If no vertical exit exists, return normal no-exit feedback and do not invoke dynamic unknown-boundary exploration.

**Rationale**: Adventure exits already use the same direction names and `ConnectionDirection` supports both values. Reusing the current rules preserves exit conditions and state-transition behavior. The feature specification explicitly excludes unknown-boundary exploration as a substitute for a missing vertical exit.

**Alternatives considered**:

- Add a separate vertical movement workflow: rejected because vertical exits already fit the shared movement model.
- Explore or generate a vertical connection whenever the exit is missing: rejected because it conflicts with the specified no-exit behavior.

## Decision 4: Preserve older game snapshots

**Decision**: Make the snapshot history field optional for deserialization. When loading a snapshot without it, reconstruct history from the persisted sequence of legacy `PlayerMovedEvent` records, which represent ordinary successful movements.

**Rationale**: Existing saves store the movement event history and should remain usable after this additive feature. No database schema change is needed because the infrastructure persists the serialized game snapshot.

**Alternatives considered**:

- Initialize all old saves with an empty history: rejected because it would unnecessarily discard usable route history already present in the event log.
- Require a database migration: rejected because the change is to the serialized snapshot contract, not a relational column or table.
