# Data Model: AI Adventure Editor

**Feature**: 003-ai-adventure-editor | **Date**: 2026-09-02

| Entity | Key fields | Rules |
|---|---|---|
| `AdventureDraft` | id, adventure identifier, title, definition JSON, revision, status, current version id | Mutable only through validated author actions; identifier is unique. |
| `AdventureVersion` | id, draft id, sequence, definition JSON, published at | Immutable; unique `(draftId, sequence)`. |
| `AuthoringProposal` | id, draft id, request summary, patch JSON, status, validation report | Pending until author accepts/rejects; never applies itself. |
| `AuthoringValidationResult` | draft/proposal id, issues, evaluation revision | Each blocking issue identifies element and reason. |
| `PlaytestSession` | id, draft id, draft revision, game id, started at | Bound to a validated snapshot; does not modify source content. |
| `AuthoringAuditEntry` | id, draft id, action, timestamp, source | Records acceptance, rejection, publication, restoration, and playtest actions. |

```text
AdventureDraft 1 --- * AuthoringProposal
AdventureDraft 1 --- * AdventureVersion
AdventureDraft 1 --- * AuthoringAuditEntry
AdventureDraft 1 --- * PlaytestSession --- 1 Game
AdventureVersion 1 --- * Game (immutable source version identity)
```

Validation reuses runtime JSON validation and adds identifier uniqueness, reachable start, meaningful playable content, protected-lore consistency, and location/NPC/item/puzzle/lore reference checks.

## Adventure Definition JSON

The mutable `AdventureDraft.DefinitionJson` and immutable `AdventureVersion.DefinitionJson` use the same top-level shape. Draft identity, title, and starting location are also stored as relational columns; create, save, proposal acceptance, and restore synchronize the JSON keys `id`, `title`, and `startingLocationId` with those columns.

- `locations[]`: `id`, `name`, `description`, `type`, `environment`, `exits[]`, `objectIds[]`, and `npcIds[]`. `type` uses the Domain `LocationType` vocabulary. `environment` contains `terrain`, `climate`, `lighting`, and optional `features`.
- `exits[]`: `direction`, `to`, and optional `requiredFlagKey` / `mustBeSet` condition.
- `items[]`: `id`, `name`, `description`, string-valued `states`, and optional reveal condition.
- `npcs[]`: `id`, `name`, `personality`, `knowledgeBoundary`, `goals`, `relationships`, and optional `locationId`. If `locationId` and a location's `npcIds` are both supplied, they must agree.
- `lore[]`: `id`, `classification`, `category`, `scope`, `contentKey`, `content`, `isProtected`, and `subjectReferences`. `PlayerDiscoverableKnowledge` and `RegionalFact` entries can be discovered when references match gameplay triggers permitted by their scope.
- `puzzles[]`: `id`, `state`, `prerequisites[]`, `solutions[]` with explicit `conditions[]`, declared `outcomes[]`, and `chainLinks[]`. The singular legacy `puzzle` property remains a runtime fallback for existing adventures.

The initial World is bootstrapped from the materialized Game: location type/environment and authored lore entries are copied into `WorldLocation` / `World.LoreEntries`. `GameSnapshot` also retains location type/environment and NPC goals/relationships for restart compatibility. Puzzle resolution returns only declared consequence IDs from the normal orchestration path after a successful deterministic evaluation.