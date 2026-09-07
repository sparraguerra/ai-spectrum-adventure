# Research: AI Adventure Editor

**Feature**: 003-ai-adventure-editor | **Date**: 2026-09-02

## Decision 1: Persist drafts separately from immutable versions

- **Decision**: Use mutable drafts and immutable version snapshots.
- **Rationale**: Authors can edit safely while existing games retain stable source content.
- **Alternatives considered**: Updating one catalog row in place (rejected: breaks version isolation); cloning live worlds (rejected: couples authoring to runtime state).

## Decision 2: Reuse validated adventure-definition JSON

- **Decision**: Store draft/version content as validated adventure-definition JSON and continue loading games with `AdventureWorldFactory`.
- **Rationale**: Feature 002 already supports multiple puzzles, alternatives, outcomes, and chains in this format.
- **Alternatives considered**: A second fully relational authoring representation (rejected: creates two sources of truth).

## Decision 3: AI creates pending structured proposals

- **Decision**: The Authoring Proposal Agent emits a schema-constrained patch/proposal and has no persistence dependency.
- **Rationale**: Author acceptance is explicit and auditable; the validator remains authoritative.
- **Alternatives considered**: Direct AI draft writes (rejected: violates the Constitution); prose suggestions only (rejected: cannot be safely validated/applied).

## Decision 4: Publish transaction creates the next immutable snapshot

- **Decision**: Full validation precedes a transaction allocating a unique version sequence and storing the exact definition snapshot.
- **Rationale**: Prevents partial publication and concurrent duplicate versions.
- **Alternatives considered**: Marking a mutable draft as published (rejected: no version isolation).

## Decision 5: Playtests reuse the player game engine

- **Decision**: A validated draft snapshot is passed into the existing factory/game creation workflow.
- **Rationale**: Author and player behavior cannot diverge.
- **Alternatives considered**: Separate editor simulation (rejected: duplicates rules).

## Decision 6: Manual workflows work without AI

- **Decision**: CRUD, validation, publish, restore, and playtest are fully usable without an available model or preview service.
- **Rationale**: AI is optional enrichment, not an authoring dependency.
- **Alternatives considered**: Requiring an AI-generated template (rejected: harms reliability and author control).

All technical decisions are resolved.