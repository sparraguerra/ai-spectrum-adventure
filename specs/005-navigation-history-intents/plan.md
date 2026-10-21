# Implementation Plan: Navigation History and Vertical Movement

**Branch**: `005-navigation-history-intents` | **Date**: 2026-10-21 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/005-navigation-history-intents/spec.md`

## Summary

Recognize `back` as a deterministic gameplay action that returns to the last location reached by successful travel, and recognize `Up` and `Down` in both bare and prefixed movement commands. Keep state transitions in the existing rules-and-events pipeline. Store the previous-location stack with the game snapshot, record backtracking as a distinguishable game event, and preserve normal exit validation for vertical travel. A missing vertical exit must produce the ordinary no-exit result rather than trigger unknown-boundary exploration.

## Technical Context

**Language/Version**: C# on .NET 10 (`net10.0`)

**Primary Dependencies**: Existing .NET application, domain, contracts, and agent projects; Microsoft Agent Framework is not required for this deterministic feature. No new package is needed.

**Storage**: Existing EF-backed game repository serializes `GameSnapshot`, including the event history. Extend the snapshot with an optional previous-location list; no relational schema change is expected.

**Testing**: Existing xUnit test projects with FluentAssertions. Add focused interpreter, application/orchestration, domain snapshot, and persistence coverage.

**Target Platform**: Existing ASP.NET Core web adventure, with the current Azure Container Apps deployment unchanged.

**Project Type**: Modular monolith with separate Agents, Application, Contracts, Domain, Infrastructure, and Web projects.

**Performance Goals**: Keep both commands on the deterministic gameplay path without an extra model call. A `back` action reads and updates the in-memory location stack in constant time.

**Constraints**:

- Game state changes MUST remain deterministic and pass through rules validation and domain events.
- Existing adventure exits and their conditions remain authoritative for `Up`/`Down` travel.
- Failed movement MUST NOT change the previous-location stack.
- Existing snapshots without the new history field MUST remain loadable.
- No Azure resource, API response, or third-party package changes are required.

**Scale/Scope**: Add one player action (`back`), enable the two existing vertical directions in the intent parser, and apply the behavior to all games using the shared movement pipeline. Do not add a forward-history command or new adventure content.

## Constitution Check

| Gate | Status | Design evidence |
|---|---|---|
| Deterministic game state first (I) | PASS | The player history is authoritative domain state; rules validate transitions and events apply them. |
| Structured output and rules before narrative (III–IV) | PASS | Intent parsing yields a structured action; `RulesEngine` decides feasibility before narration or mutation. |
| Player agency (VI) | PASS | Both bare directions and natural-language movement commands are supported, alongside `back`. |
| Layered memory and persistent state (VII, XVI–XVII) | PASS | Location history belongs to the game/player snapshot and is independent of a web replica's lifetime. |
| Testable behavior (VIII) | PASS | Parser, rules, event application, snapshot compatibility, and end-to-end movement have deterministic test seams. |
| Modular monolith / no premature services (XIII) | PASS | Changes stay in the existing project boundaries; no new service or infrastructure is introduced. |
| Responsiveness and observability (IX, XIV, XXII) | PASS | Commands avoid model calls and continue through existing action telemetry and response behavior. |

No constitution violations or unresolved design gates were identified.

## Project Structure

### Documentation (this feature)

```text
specs/005-navigation-history-intents/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
└── contracts/
    └── navigation-intents.md
```

### Source Code (repository root)

```text
src/
├── AI.SpectrumAdventure.Agents/Intent/
│   ├── CommandPatternInterpreter.cs
│   └── WorldVocabulary.cs
├── AI.SpectrumAdventure.Application/
│   ├── Rules/                 # Backtracking validation and failure reason
│   └── Orchestration/         # Intent dispatch, narrative, vertical fallback
├── AI.SpectrumAdventure.Contracts/
│   └── ParsedIntent.cs        # Add the Back action
├── AI.SpectrumAdventure.Domain/
│   ├── Events/                # Backtracking event and snapshot mapping
│   ├── Games/                 # Snapshot persistence
│   └── Players/               # Previous-location history
└── AI.SpectrumAdventure.Infrastructure/
    └── Persistence/           # Existing GameSnapshot serialization boundary

tests/
├── AI.SpectrumAdventure.Agents.Tests/
├── AI.SpectrumAdventure.Application.Tests/
├── AI.SpectrumAdventure.Domain.Tests/
└── AI.SpectrumAdventure.IntegrationTests/
```

**Structure Decision**: Use the existing modular-monolith boundaries. Intent recognition belongs in Agents; structured actions and validation belong in Contracts/Application; authoritative history, movement events, and snapshot state belong in Domain; the existing Infrastructure serializer remains the persistence boundary. Tests extend the corresponding existing test projects.

## Phase 0: Research

The design decisions and alternatives are recorded in [research.md](./research.md). The existing code confirms that:

- `WorldVocabulary` and `CommandPatternInterpreter` centralize case-insensitive deterministic direction recognition, but currently list only cardinal directions.
- `ConnectionDirection` and authored adventure exits already include `Up` and `Down`.
- `AdventureOrchestrator` routes structured intents through `RulesEngine` and applies validated events.
- `Game` stores an append-only movement event history; `GameSnapshot` serializes both player state and that event history.
- `ProcessActionAsync` currently attempts unknown-boundary exploration for any `Go` direction with no authored exit. Vertical directions need to preserve the specified no-exit behavior instead.

No external research or package selection is required.

## Phase 1: Design & Contracts

### Data Model

See [data-model.md](./data-model.md). The design adds a previous-location stack to the authoritative Player state and its persisted snapshot, plus a distinct backtracking event so applying `back` pops the stack rather than recording a new ordinary forward move.

### User-Facing Command Contract

See [contracts/navigation-intents.md](./contracts/navigation-intents.md). This internal contract documents the recognized command forms, resulting structured intents, and observable success/failure behavior. The existing web response shape remains unchanged.

### Validation Guide

See [quickstart.md](./quickstart.md) for targeted test commands and end-to-end scenarios covering parser behavior, movement, repeated backtracking, blocked exits, and persistence.

### Persistence and Compatibility

- New game snapshots serialize the ordered previous-location stack.
- Snapshots without that optional field remain valid. Their stack is reconstructed from the existing chronological movement events; legacy movement events are treated as ordinary successful travel.
- Backtracking is recorded as a distinct movement event in the existing event-history format, so reloads preserve the distinction between forward travel and popping history.
- No database migration or infrastructure change is planned.

### Unknown-Boundary Behavior

Vertical directions that have no current authored exit return the standard `NoSuchExit` result and do not invoke unknown-boundary exploration. A vertical exit that exists still uses the existing movement-condition validation; a blocked exit reports `ExitConditionNotMet` without changing history.

## Constitution Check (Post-Design)

All pre-design gates remain PASS. The snapshot change is an additive optional field, and the event change is additive to the existing event mapping. The deterministic rules pipeline remains authoritative; no AI agent, cloud resource, or new service is introduced. No complexity exception is needed.

## Complexity Tracking

No constitution violations or additional architectural components are proposed.
