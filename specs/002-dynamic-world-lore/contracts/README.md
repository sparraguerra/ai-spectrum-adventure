# Contracts: Dynamic World, Lore & Procedural Adventures

These internal contracts separate player/UI requests, deterministic application workflows, and AI enrichment. They extend Feature 001's structured contract rule: all agent-originated output is validated, and only deterministic code creates authoritative world events.

| Contract | Direction | Authority |
|---|---|---|
| `world-generation-request.schema.json` | Application -> deterministic generator | Candidate request only; no AI input. |
| `world-exploration-result.schema.json` | Application -> UI | Persisted factual travel/discovery result. |
| `lore-discovery-result.schema.json` | Application -> UI | Player-safe discovery result. |
| `npc-context.schema.json` | Application -> NPC Agent | Read-only, scoped knowledge projection. |
| `puzzle-evaluation-result.schema.json` | Application -> UI/Narrator | Deterministic evaluation result. |
| `world-enrichment-proposal.schema.json` | World Enrichment Agent -> Application | Non-authoritative presentation proposal. |

All contracts reject unexpected fields. Existing Feature 001 player-action, action-result, narration, NPC-response, game-event, and visual-scene contracts remain in effect.