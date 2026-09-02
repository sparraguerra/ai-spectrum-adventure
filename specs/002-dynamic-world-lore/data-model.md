# Data Model: Dynamic World, Lore & Procedural Adventures

**Feature**: 002-dynamic-world-lore | **Date**: 2026-09-02

Authoritative entities remain pure Domain types. EF Core records, mappings, transactions, indexes, and migrations belong only to Infrastructure.

## Aggregate roots

### `World`

| Field | Purpose |
|---|---|
| `WorldId` | Persistent world identity. |
| `WorldSeed` | Immutable seed used to derive deterministic generation keys. |
| `GenerationVersion` | Immutable generation rule version for this world. |
| `Regions` | Owned regions and their authoritative locations. |
| `Connections` | Owned directional graph edges. |
| `LoreEntries` | Canonical structured lore and allowed perspectives. |
| `Npcs` | Canonical NPC state and placement. |
| `Puzzles` | Reusable puzzle definitions and authoritative states. |
| `Events` | Append-only world event history. |
| `GenerationMetadata` | Immutable provenance for generated structures. |
| `ConcurrencyToken` | Persistence-boundary optimistic-concurrency token. |

**Invariants**: seed/version never change; a location identity is unique; a direction from a location has at most one authoritative active connection; a generation key has at most one authoritative result; events are append-only; agent output cannot call mutation methods.

### `Game`

Add `WorldId` and `PlayerKnowledge`; retain current location, inventory, personal history, and bounded NPC conversation context. `Game` references world entities by identifiers and never contains a second authoritative world graph.

## Owned World entities

| Entity | Key fields | Rules |
|---|---|---|
| `Region` | `RegionId`, type, terrain profile, parent/adjacent relationships, expansion policy | Region types and terrain transitions must be allowed by generation rules. |
| `Location` | `LocationId`, region, type, structural name key, environmental properties, state, scene version | Persistent identity and structural properties cannot be changed by enrichment. |
| `Connection` | source, direction, destination, visibility, availability, optional condition | Destination exists; source-direction is unique; changes require a world event. |
| `GeneratedContentMetadata` | generation key, seed/version, source boundary, reason, generated at | Immutable after a successful commit; supports deduplication and diagnostics. |
| `WorldEvent` | event id, sequence, cause, occurred at, kind, payload | Only validated events change world state; sequence is monotonically increasing. |
| `NpcState` | `NpcId`, traits, goals, allowed lore references, current location, relationships, state version | One canonical location at a time; movement requires validated event. |
| `Puzzle` | `PuzzleId`, state, prerequisite references, clues, solutions, outcomes, next-puzzle references | State transitions are evaluated deterministically; solutions are explicit. |
| `LoreEntry` | `LoreId`, category, truth classification, scope, subject references, content key, discoverability | Immutable/historical facts cannot be silently contradicted; rumours are labelled as uncertain perspective. |

## Player knowledge

`PlayerKnowledge` is owned by `Game` and contains discovered location IDs, connection IDs, region IDs, lore IDs, clue IDs, learned NPC information IDs, and discovery timestamps. It is additive by default; a discovery event is idempotent and duplicate discovery returns known context rather than a new reward.

## State transitions

| Transition | Validation | Result |
|---|---|---|
| Unknown boundary -> generated connection | deterministic candidate, graph/lore/NPC/event constraints, unique key | persist location/connection/metadata then permit movement/discovery |
| Location undiscovered -> discovered | successful arrival or supported discovery | record player knowledge and emit discovery event |
| Lore undiscovered -> discovered | relevant trigger and player access | record lore ID in player knowledge |
| NPC relocation | rule/event permits move, target location exists | append event and update canonical NPC location |
| Puzzle unknown -> discovered -> investigating/blocked -> solved | deterministic preconditions and explicit solution conditions | persist state and outcomes; unlock dependent elements |
| Scene version increment | material visual state change only | request optional post-commit visual enrichment |

## Relationships

```text
World 1 --- * Region 1 --- * Location
World 1 --- * Connection --- 1 Location (destination)
World 1 --- * LoreEntry
World 1 --- * NpcState
World 1 --- * Puzzle
World 1 --- * WorldEvent
Game * --- 1 World
Game 1 --- 1 PlayerKnowledge --- * World entity references
```

## Persistence boundaries

`WorldRecord` is the concurrency boundary for world-level changes, with child records or a serialized world snapshot as selected during implementation. The required database constraint is unique `(WorldId, GenerationKey)` for generated content. A single transaction writes world changes and any associated game/player-knowledge changes. Blob assets remain non-authoritative references keyed by world, location, and scene version.