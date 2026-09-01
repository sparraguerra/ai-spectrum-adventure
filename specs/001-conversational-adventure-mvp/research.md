# Research: Conversational Adventure MVP

**Feature**: 001-conversational-adventure-mvp | **Date**: 2026-08-31

This document resolves the technical unknowns implied by the feature's technical context and constitution before Phase 1 design. Each decision follows the constitution's constraints (deterministic state ownership, modular monolith, no premature Dapr/microservices, Azure Container Apps as the production target).

## Decision 1: Relational persistence technology

- **Decision**: Use Entity Framework Core 10 with the Npgsql provider targeting Azure Database for PostgreSQL Flexible Server in production, and a local PostgreSQL container (or SQLite for the fastest inner-loop unit tests that don't need EF Core provider parity) for local development.
- **Rationale**: PostgreSQL is a fully managed Azure relational service compatible with Managed Identity-based authentication via Azure AD, has first-class EF Core support, and avoids licensing/compute overhead of SQL Server for a single-app MVP. Using EF Core keeps the Domain layer persistence-ignorant (constitution Principle XVI); only `AI.SpectrumAdventure.Infrastructure` references EF Core/Npgsql.
- **Alternatives considered**: Azure SQL Database (viable, slightly higher cost/complexity for MVP scope, no clear advantage here); Cosmos DB (rejected — relational/consistent-transaction model fits the turn-based, strongly-consistent GameState better than a document/NoSQL model); pure in-memory storage (rejected — violates constitution Principle XVI/XVII, which require externalized, replica-independent persistent game state).

## Decision 2: Blazor hosting model

- **Decision**: Blazor with Interactive Server render mode for the MVP.
- **Rationale**: Server-side rendering keeps game-processing logic co-located with the authoritative state and avoids shipping domain/agent-invocation code to the browser; it gives the fastest path to a responsive, low-latency conversational UI with SignalR-based real-time updates (useful for progressive image loading, constitution Principle XXII). WebAssembly would require exposing a public API and duplicating client/server concerns, which is unnecessary complexity for a single-scenario MVP.
- **Alternatives considered**: Blazor WebAssembly (rejected for MVP — adds a public API surface and client-side state synchronization complexity not justified yet); Blazor Auto (rejected — its main benefit, WASM prefetch/interactivity fallback, is not needed until multi-client scenarios exist).

## Decision 3: Intent interpretation approach

- **Decision**: A two-stage `IntentInterpreterAgent`: (1) a fast deterministic pattern/keyword matcher recognizes classic-command shapes (`LOOK`, `EXAMINE <target>`, `GO <direction>`, `TAKE <item>`, `OPEN <target>`, `TALK TO <npc>`, `USE <item> [ON <target>]`, plus common synonyms) with zero LLM cost; (2) any input that doesn't match a known pattern is sent to an LLM-backed classifier (via Microsoft Agent Framework) that returns a structured `ParsedIntent` (action, target, parameters, confidence) constrained by a JSON schema/response format.
- **Rationale**: Satisfies constitution Principle X (minimize unnecessary model calls) by resolving the majority of classic-command traffic deterministically, while still fully supporting unrestricted natural language per spec FR-005/FR-004. A single shared `ParsedIntent` contract (see `contracts/`) keeps both paths uniform for the Rules Engine.
- **Alternatives considered**: LLM-only intent classification for every input (rejected — unnecessary cost/latency for simple commands, violates Principle X); pure regex/keyword parser with no NLP fallback (rejected — fails spec FR-005's requirement to support unrestricted natural language and creative actions).

## Decision 4: Structured output enforcement with Microsoft Agent Framework

- **Decision**: Each agent (Intent Interpreter, Narrator, NPC, Visual Art Director) is implemented as a Microsoft Agent Framework agent configured with a strongly-typed structured output/response schema (JSON-schema-constrained response format where the underlying model supports it), deserialized directly into the corresponding Contracts DTO (`ParsedIntent`, `NarrationResult`, `NpcResponse`, `VisualSceneSpec`). A validation step in the Application layer rejects/retries once on schema-invalid output and falls back to a safe default (e.g., "I'm not sure what you mean" narration) rather than throwing to the player.
- **Rationale**: Directly implements constitution Principle III (structured output over free-form parsing) and Principle VIII (testable AI behavior — schemas enable deterministic golden-response tests with mocked chat clients).
- **Alternatives considered**: Free-form text parsing with regex/heuristics on LLM output (rejected — fragile, violates Principle III); requiring the LLM to call tools that directly write to the database (rejected — would make the agent an authoritative state owner, violating Principle I).

## Decision 5: Visual generation and retro processing

- **Decision**: The Visual Art Director agent produces a structured `VisualSceneSpec` (location, key visible objects, mood, style tag) which an `ImagePipeline` sends to an image generation model (Azure OpenAI/Azure AI Foundry image deployment), followed by a deterministic retro post-processing step (palette quantization to a fixed ZX-Spectrum-inspired palette, resolution downscale, optional dithering) implemented in plain .NET (no AI) before storing the final asset.
- **Rationale**: Matches constitution Principle V (retro identity enforced by a style validation/post-processing stage) and Principle XXI (visual pipeline separated from authoritative state; image is an interpretation, not a source of truth). Deterministic post-processing guarantees consistent 8-bit aesthetics regardless of the underlying image model's raw output style.
- **Alternatives considered**: Relying solely on prompt engineering for retro style (rejected — inconsistent results across generations, no deterministic guarantee); using a dedicated pixel-art generation model only (rejected — availability/quality varies; a deterministic post-process is a safer, testable guarantee).

## Decision 6: Testing framework and AI mocking strategy

- **Decision**: xUnit + FluentAssertions across all test projects. Agent-layer tests use Microsoft Agent Framework's testable chat-client abstractions (a fake/mock `IChatClient` or agent implementation returning canned structured responses) — no live network/LLM calls in unit or CI test runs. Integration tests use `WebApplicationFactory` with the same mocked agents plus a real (test-container or SQLite) database.
- **Rationale**: Satisfies constitution Principle VIII (LLM providers MUST be abstracted sufficiently to allow testing with mocked/simulated responses) while keeping CI fast and deterministic.
- **Alternatives considered**: MSTest/NUnit (equally valid; xUnit chosen for consistency with the wider .NET ecosystem and Agent Framework samples); live LLM calls in CI (rejected — nondeterministic, slow, costly, and not testable per Principle VIII).

## Decision 7: Concurrency and consistency for GameState updates

- **Decision**: EF Core optimistic concurrency using a `RowVersion`/`xmin`-style concurrency token on the `Game` aggregate root. Each player action is processed as a single use case that loads the game, validates via the Rules Engine, applies events, and saves within one transaction; a concurrency conflict (e.g., a second tab sending an action for the same `GameId`) is retried once and otherwise surfaces a friendly "someone else is already acting" narrative response rather than corrupting state.
- **Rationale**: Directly implements constitution Principle XVII (concurrency-sensitive state transitions MUST be protected) while keeping the app stateless (no server-side session affinity) for future horizontal scaling.
- **Alternatives considered**: Pessimistic row locking (rejected — unnecessary complexity/latency for a single-player MVP's low contention); in-memory locks per `GameId` (rejected — would break with multiple replicas, violating Principle XVII's replica-independence requirement).

## Decision 8: Observability

- **Decision**: OpenTelemetry tracing + metrics instrumented at: (a) the Application orchestrator (one span per player action, tagged with action type and outcome), (b) the Rules Engine (validation result and reason), (c) each agent invocation (agent name, duration, success/failure, structured-output-valid flag — no raw prompt/response content logged by default), and (d) the image pipeline (generation attempt, duration, success/failure, cache hit/miss). Exported via the OpenTelemetry Azure Monitor exporter to Application Insights in Azure; console/OTLP exporter for local development.
- **Rationale**: Implements constitution Principle IX (observability as first-class) and Principle IX's guidance to avoid persisting sensitive content unnecessarily — telemetry captures metadata, not full narrative text, by default.
- **Alternatives considered**: Framework-specific logging only (e.g., `ILogger` without tracing) — rejected as insufficient for reconstructing cross-component causality (Principle IX explicitly asks for this); a third-party APM SDK — rejected in favor of the constitution's explicit OpenTelemetry + Application Insights recommendation.

## Decision 9: Asynchronous image generation without Dapr

- **Decision**: An in-process background queue using `System.Threading.Channels.Channel<T>` plus a hosted `BackgroundService` (`ImageGenerationWorker`) inside the single `AI.SpectrumAdventure.Web` process. When a game event marks a scene as materially changed, the orchestrator enqueues an `ImageGenerationRequested` work item (containing the `VisualSceneSpec` and a scene-state cache key) and immediately returns the narrative response to the player. The worker processes the queue, calls the image pipeline, and updates a `VisualAsset` record (URL/blob reference + status) that the UI observes via a lightweight SignalR notification (Blazor Server already has a live circuit) or short client-side poll.
- **Rationale**: Satisfies constitution Principle XIV (asynchronous processing without introducing Dapr solely because it's available) and Principle X/XXII (non-blocking narrative response). A separate worker process/Dapr pub-sub is explicitly deferred until there is a concrete need for independent scaling (Principle XIII/XIV).
- **Alternatives considered**: Dapr pub/sub with a separate worker Container App (rejected for MVP — no concrete scaling/isolation requirement yet, and constitution explicitly says Dapr MUST NOT be introduced solely because it's available); synchronous image generation inline in the request (rejected — violates FR-025/Principle XXII, would block the gameplay loop).

## Decision 10: UI progressive image updates

- **Decision**: The Blazor Server component subscribes to a per-`GameId` update notification (in-process event via a scoped/singleton notifier service, since there's only one process) that re-renders the `SceneImage` component when the `ImageGenerationWorker` finishes; until then, the UI shows the previous image (or a simple placeholder on first visit) and the narrative text is already visible.
- **Rationale**: Implements constitution Principle XXII (progressive updates: narrative first, image appears when ready) using the simplest mechanism available in a single-process Blazor Server app — no SignalR groups/external pub-sub needed at MVP scale.
- **Alternatives considered**: Client polling every N seconds (viable fallback, slightly less elegant; kept as a documented fallback if in-process eventing proves awkward across DI scopes); WebSocket/SSE custom endpoint (rejected — redundant given Blazor Server's existing persistent connection).

## Decision 11: Identity and secrets

- **Decision**: `DefaultAzureCredential` for all Azure service access (Blob Storage, Azure AI/Foundry endpoint) in Azure Container Apps via a user-assigned Managed Identity; local development uses .NET user-secrets for connection strings/API keys and the Azurite storage emulator, never committing secrets to source control.
- **Rationale**: Directly implements constitution Principle XV.
- **Alternatives considered**: Storing API keys in App Settings/environment variables in production (rejected as primary approach — acceptable only as a fallback for services that don't yet support Managed Identity, to be documented if encountered during implementation).

## Summary of resolved unknowns

All "NEEDS CLARIFICATION" style technical unknowns implied by the Technical Context are resolved above. No open questions block Phase 1 design.
