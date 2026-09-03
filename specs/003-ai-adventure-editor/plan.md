# Implementation Plan: AI Adventure Editor

**Branch**: `003-ai-adventure-editor` | **Date**: 2026-09-02 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/003-ai-adventure-editor/spec.md`

## Summary

Add an in-application Adventure Editor to the existing modular monolith. The editor owns mutable `AdventureDraft` records and immutable `AdventureVersion` snapshots. It validates all accepted author changes deterministically before saving and validates complete drafts before publication. AI returns structured `AuthoringProposal` records only; the author explicitly accepts a proposal, after which deterministic validation may apply it to the draft. Playtests create games from validated draft snapshots through the existing game engine. A maintained author-facing manual lives at `docs/adventure-authoring-guide.md`.

## Technical Context

**Language/Version**: C# 13 / .NET 10.

**Primary Dependencies**: Existing ASP.NET Core Blazor Interactive Server, EF Core/Npgsql, Microsoft Agent Framework, Azure Identity, Blob Storage, and OpenTelemetry.

**Storage**: PostgreSQL stores drafts, immutable version snapshots, authoring proposals, validation reports, and playtest metadata. Existing game snapshots retain the exact published version identity used to start them. Blob Storage remains only for optional visual previews/assets.

**Testing**: xUnit and FluentAssertions; `WebApplicationFactory` integration tests; mocked Agent Framework outputs. No live model calls in CI.

**Target Platform**: One stateless Linux Container App with external PostgreSQL and Blob Storage.

**Project Type**: Existing server-rendered Blazor modular monolith. No microservices or independently deployed editor/agent services.

**Performance Goals**: Draft save and validation feedback within 2 seconds at p95 for a small adventure. AI proposals and visual previews are optional and never block saving or manual editing.

**Constraints**: Only deterministic code saves accepted draft changes, creates versions, or starts playtests. Published versions are immutable. AI output is structured and reviewable. Domain stays independent of EF Core, Azure, Agent Framework, and UI.

**Scale/Scope**: One author role, bounded sessions, and optimistic concurrency. Collaboration, marketplaces, and autonomous AI publication are excluded.

## Constitution Check

| Principle | Status | Response |
|---|---|---|
| I, III, IV | PASS | Draft mutation, validation, publication, and playtest creation are deterministic use cases; proposals are never state mutations. |
| II, XX | PASS | Add one Authoring Proposal Agent with a scoped proposal-only responsibility. |
| VI, VII, VIII | PASS | Authors retain acceptance control; draft/version/proposal memory is separate; validators and contracts are testable. |
| V, XXI, XXII | PASS | Retro previews reuse optional visual infrastructure and cannot block authoring or become authoritative. |
| IX, X | PASS | Record proposal, validation, acceptance, publication, and playtest metadata without raw prompts by default. |
| XI-XIX | PASS | PostgreSQL concurrency and existing Container App/IaC patterns remain sufficient. |

## Architecture and Workflows

- `AdventureDraft` is the mutable authoring aggregate: identifier, accepted definition JSON, revision, status, and validation state.
- `AdventureVersion` is immutable: draft identity, sequence, published timestamp, and a complete validated definition snapshot. Every player Game stores its `AdventureVersionId` source identity; draft playtests store their immutable draft-snapshot identity.
- `AuthoringProposal` is non-authoritative: request, structured patch, review status (`Pending`, `Accepted`, `Rejected`, `Invalid`), and validation result.
- `AuthoringValidator` reuses Feature 002 definition validation and adds reachability, protected lore, reference, and meaningful-content checks.
- `PlaytestSession` identifies a Game created from a validated draft snapshot and cannot modify draft/version content.

**Save draft**: author change -> deterministic validation -> optimistic-concurrency save -> validation result.

**AI proposal**: author request -> scoped Authoring Proposal Agent -> pending structured proposal -> author edits/accepts/rejects -> validation -> accepted patch applied to draft.

**Publish**: explicit author request -> full validator -> transaction creates immutable `AdventureVersion` -> catalog exposes the latest published version for new games. Existing games resolve their persisted `AdventureVersionId`, never a later version.

**Playtest**: valid draft -> immutable draft snapshot with source identity -> existing `AdventureWorldFactory` and game creation path.

**Command target resolution**: authored item names use their stable definition IDs as a fallback vocabulary, so commands such as `Take Screwdriver` resolve to `screwdriver` without requiring a hard-coded entry for every adventure.

**Restore**: version selection -> copy immutable JSON to a new draft revision; never mutate the selected version.

## Project Structure

```text
src/
├── AI.SpectrumAdventure.Domain/Authoring/
├── AI.SpectrumAdventure.Application/Authoring/
├── AI.SpectrumAdventure.Application/Abstractions/
├── AI.SpectrumAdventure.Contracts/
├── AI.SpectrumAdventure.Agents/Authoring/
├── AI.SpectrumAdventure.Infrastructure/Persistence/
└── AI.SpectrumAdventure.Web/Components/Authoring/

tests/
├── AI.SpectrumAdventure.Domain.Tests/
├── AI.SpectrumAdventure.Application.Tests/
├── AI.SpectrumAdventure.Agents.Tests/
└── AI.SpectrumAdventure.IntegrationTests/

docs/adventure-authoring-guide.md
```

**Structure Decision**: Extend existing projects by feature namespace. The catalog remains player-facing published content; authoring repositories own draft/version lifecycle. No external API or service extraction is needed.

## Incremental Delivery

1. Add authoring aggregates, contracts, records, optimistic concurrency, and deterministic validation.
2. Deliver manual draft CRUD and structured world/NPC/lore/puzzle editing.
3. Add immutable publication, version history/restoration, and catalog resolution by version.
4. Add draft playtests through the existing game engine.
5. Add reviewable AI proposals after manual authoring is reliable.
6. Add optional retro previews, telemetry, the authoring manual, and end-to-end regression coverage.
7. Maintain command-resolution regression coverage when authored content introduces new item names; simple names normalize to stable IDs and must not become `AmbiguousIntent`.

## Post-Design Re-check

The design keeps authoritative ownership deterministic: AI only proposes a patch and cannot save accepted content, publish a version, or launch a playtest without explicit author action and validation. Version snapshots isolate active games from later edits. PostgreSQL tokens and transactions protect concurrent changes across Container App replicas.

## Complexity Tracking

No entries.
