# Contracts: Conversational Adventure MVP

These schemas define the structured boundaries between deterministic application code and AI agents, per constitution Principle III ("Structured Output Over Free-Form State Mutation") and Principle IV ("Rules Before Narrative"). The MVP is a single deployable application (no public HTTP API is required for external consumers), so these contracts are **internal architectural boundaries** — enforced as C# records in `AI.SpectrumAdventure.Contracts` and validated at runtime against the equivalent JSON Schema below whenever data crosses an agent boundary (agent input/output, and the player-facing action request/result exchanged between the Blazor UI and the Application layer).

| Contract | Direction | Produced by | Consumed by |
|---|---|---|---|
| `player-action-request.schema.json` | UI → Application | Blazor UI (player input) | `AdventureOrchestrator` |
| `game-event.schema.json` | Application → Domain | Rules Engine (proposed events) | `Game.Apply(...)` |
| `action-result.schema.json` | Application → UI | `AdventureOrchestrator` | Blazor UI |
| `narration-result.schema.json` | Agent → Application | Narrator Agent | `AdventureOrchestrator` (validated before use) |
| `npc-response.schema.json` | Agent → Application | NPC Agent | `AdventureOrchestrator` (validated before use) |
| `visual-scene-spec.schema.json` | Agent → Image Pipeline | Visual Art Director Agent | `ImagePipeline` (async, non-blocking) |

All schemas use `"additionalProperties": false` so unexpected/hallucinated fields from an LLM-backed agent response fail validation instead of silently entering the system (supports Principle VIII's testability and Principle I's "AI-generated content MUST NOT directly bypass domain validation").
