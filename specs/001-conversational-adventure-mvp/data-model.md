# Data Model: Conversational Adventure MVP

**Feature**: 001-conversational-adventure-mvp | **Date**: 2026-08-31

All entities below live in `AI.SpectrumAdventure.Domain` and have zero dependency on EF Core, Agent Framework, or ASP.NET Core (persistence mapping/configuration lives separately in `AI.SpectrumAdventure.Infrastructure`). Types map to the feature spec's Key Entities section and the constitution's authoritative `GameState` requirements.

## Aggregate root: `Game`

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | Aggregate identity (`GameId`). |
| `CreatedAt` | `DateTimeOffset` | Set on start. |
| `UpdatedAt` | `DateTimeOffset` | Updated on every applied event. |
| `RowVersion` | `byte[]` / concurrency token | EF Core optimistic concurrency (Decision 7). |
| `Player` | `Player` (owned) | Current player state. |
| `Locations` | `IReadOnlyCollection<Location>` | The 4 MVP locations and their live state. |
| `Npcs` | `IReadOnlyCollection<Npc>` | MVP has exactly 1, modeled as a collection for extensibility. |
| `Puzzle` | `Puzzle` (owned) | The single primary puzzle (Forgotten Tower entrance). |
| `WorldFlags` | `IReadOnlyCollection<WorldFlag>` | Persistent facts (e.g., `sign-read`, `tower-unlocked`). |
| `EventHistory` | `IReadOnlyCollection<GameEvent>` | Append-only log of applied Game Events for this session (FR-003, FR-026). |

**Invariants**:
- A `Game` is always in exactly one `Location` (via `Player.CurrentLocationId`).
- `EventHistory` is append-only; existing entries are never mutated or removed (supports "previously established facts remain consistent" — FR-003).
- All mutations to `Game` happen through domain methods (e.g., `Game.Apply(GameEvent)`), never through public setters, so the Rules Engine/Application layer cannot bypass invariants (Principle I, IV).

## `Player`

| Field | Type | Notes |
|---|---|---|
| `CurrentLocationId` | `LocationId` | FK-like value object referencing a `Location`. |
| `Inventory` | `Inventory` (owned) | See below. |
| `KnownClues` | `IReadOnlySet<string>` (clue keys) | Tracks clues learned from the NPC (e.g., `tower-clue`), used by the Puzzle solution check (FR-019). |

## `Inventory`

| Field | Type | Notes |
|---|---|---|
| `Items` | `IReadOnlyCollection<ItemId>` | Items currently possessed (FR-011, FR-012). |

**Behavior**: `Add(ItemId)`, `Remove(ItemId)`, `Contains(ItemId)`. Only mutated via `ItemTaken`/`ItemUsed` (when consumed) Game Events applied to `Game`.

## `Location`

| Field | Type | Notes |
|---|---|---|
| `Id` | `LocationId` (`ForestEntrance`, `DarkForest`, `OldBridge`, `ForgottenTower`) | Fixed set for the MVP (spec world layout). |
| `Name` | `string` | Display name. |
| `BaseDescription` | `string` | Atmosphere/base narrative seed text passed to the Narrator, not the final rendered text (Narrator composes the final description — Principle IV: rules/state first, narrative derived after). |
| `Exits` | `IReadOnlyCollection<Exit>` | Connections to other locations, each with an optional `RequiredCondition`. |
| `ObjectIds` | `IReadOnlyCollection<ItemId>` | Objects/items currently present at this location (changes as items are taken/revealed). |
| `NpcIds` | `IReadOnlyCollection<NpcId>` | NPCs currently present (MVP: NPC only in `DarkForest`). |
| `Discovered` | `bool` | Set true the first time the player enters (drives `LocationDiscovered` event / first-visit visual generation). |

### `Exit` (value object)

| Field | Type | Notes |
|---|---|---|
| `Direction` | `string` (e.g., `North`, `West`, `East`) | Matches the spec's world-layout diagram. |
| `DestinationId` | `LocationId` | Target location. |
| `RequiredCondition` | `WorldFlagCondition?` | e.g., `ForgottenTower` entrance exit requires `tower-unlocked = true` (FR-007/FR-008/FR-018). Null = always open. |

## `GameItem` (Object/Item)

| Field | Type | Notes |
|---|---|---|
| `Id` | `ItemId` (e.g., `Sign`, `BridgeKey`, `Chest`) | Stable identifier. |
| `Name` | `string` | Display name. |
| `Description` | `string` | Base description seed for Narrator. |
| `State` | `ItemState` (flags: `Visible`, `Hidden`, `Collectible`, `Locked`, `Movable`, `Interactive`) | Supports FR-009; multiple flags may apply simultaneously (e.g., `Visible | Collectible`). |
| `LocationId` | `LocationId?` | Null once taken into inventory. |
| `RevealCondition` | `WorldFlagCondition?` | For `Hidden` items — becomes visible once a flag is set (FR edge case: hidden objects not revealed in descriptions until triggered). |

## `Npc`

| Field | Type | Notes |
|---|---|---|
| `Id` | `NpcId` | Stable identifier (MVP: one NPC in Dark Forest). |
| `Name` | `string` | Display name. |
| `PersonalityProfile` | `string` (structured seed text/traits) | Passed to the NPC Agent as authoritative personality context (Principle II). |
| `KnowledgeBoundary` | `IReadOnlySet<string>` (topic/clue keys) | Explicit whitelist of what the NPC is allowed to know/reveal (FR-017); anything not listed MUST NOT be revealed regardless of agent creativity. |
| `ConversationMemory` | `IReadOnlyCollection<ConversationTurn>` | Prior Q&A turns with this player, used for consistency (FR-016). |
| `RelationshipFlags` | `IReadOnlyCollection<WorldFlag>` | e.g., `npc-gave-clue`. |

### `ConversationTurn` (value object)

| Field | Type | Notes |
|---|---|---|
| `PlayerUtterance` | `string` | What the player asked/said. |
| `NpcReply` | `string` | What the NPC said (already validated against `KnowledgeBoundary`). |
| `Timestamp` | `DateTimeOffset` | Ordering within `ConversationMemory`. |

## `Puzzle`

| Field | Type | Notes |
|---|---|---|
| `Id` | `PuzzleId` (`ForgottenTowerEntrance`) | Single MVP puzzle. |
| `Solved` | `bool` | Persistent solved/unsolved state (FR-021). |
| `RequiredItemId` | `ItemId` (`BridgeKey`, discoverable at Old Bridge) | Resolved by Clarification Q1. |
| `RequiredClueKey` | `string` (`tower-clue`, learned from the NPC) | Resolved by Clarification Q1. |
| `SolutionAction` | `PuzzleSolutionCondition` (value object: "use `RequiredItemId` at `ForgottenTowerEntrance` while player knows `RequiredClueKey`") | Encapsulates FR-019/FR-020's dual-condition rule; evaluated only by the Rules Engine, never by an agent. |

**State transitions**: `Unsolved` → `Solved` exactly once, triggered only when the Rules Engine confirms both `Inventory.Contains(RequiredItemId)` and `Player.KnownClues.Contains(RequiredClueKey)` at the moment of a valid "use item at tower" action. Once `Solved`, the `ForgottenTower` entrance `Exit.RequiredCondition` evaluates to always-true (FR-021/US9 Acceptance #3).

## `WorldFlag` (value object / small entity)

*Maps to the spec's Key Entity "World Event/Flag": the persistent-fact half is `WorldFlag` below; the historical/audit half is the `GameEvent` log (next section).*

| Field | Type | Notes |
|---|---|---|
| `Key` | `string` (e.g., `sign-read`, `tower-unlocked`, `npc-gave-clue`) | Canonical vocabulary; Application/Domain code references flags by these string constants (centralized in a `WorldFlags` static class to avoid typos). |
| `SetAt` | `DateTimeOffset` | When the flag was set. |

## `GameEvent` (Domain events, append-only)

Base type `GameEvent { Guid Id, DateTimeOffset OccurredAt, GameEventKind Kind }` with the following concrete kinds (constitution's example list, mapped to spec needs):

| Event | Payload | Effect on `Game` |
|---|---|---|
| `PlayerMoved` | `FromLocationId`, `ToLocationId` | Updates `Player.CurrentLocationId`; may set `LocationDiscovered` alongside on first visit. |
| `ItemTaken` | `ItemId`, `LocationId` | Moves item from `Location.ObjectIds` to `Player.Inventory.Items`. |
| `ItemUsed` | `ItemId`, `TargetId?` | Applies item-specific effect (e.g., consumed, or triggers `PuzzleSolved`). |
| `ObjectStateChanged` | `ItemId`, `ItemState` (new state) | E.g., a chest becomes `Interactive`/unlocked, a hidden item becomes `Visible`. |
| `DoorOpened` | `LocationId`, `ExitDirection` | Clears a movement `RequiredCondition` (used conceptually; for the MVP, tower unlock is modeled via `PuzzleSolved` + `WorldFlag`). |
| `NpcRelationshipChanged` | `NpcId`, `WorldFlag` | Records a new fact learned/given (e.g., `npc-gave-clue`), appends to `ConversationMemory`/`RelationshipFlags`. |
| `PuzzleSolved` | `PuzzleId` | Sets `Puzzle.Solved = true`, sets `tower-unlocked` `WorldFlag`. |
| `LocationDiscovered` | `LocationId` | Sets `Location.Discovered = true` (drives first-visit visual generation trigger). |

**Rule**: Only the Application layer's `AdventureOrchestrator`, after Rules Engine validation, may call `Game.Apply(event)`. Agents never see or construct `GameEvent` instances directly — they receive read-only projections and return proposals that the orchestrator translates into events (Principle III/IV).

## Supporting value objects (not persisted independently)

- `ParsedIntent`: `Action` (enum: `Look`, `Examine`, `Go`, `Take`, `Open`, `Use`, `TalkTo`, `Unknown`, ...), `Target` (string, nullable), `Parameters` (dictionary), `Confidence` (double), `RawInput` (string). Produced by Intent Interpretation, consumed by the Rules Engine (never mutates state itself).
- `RulesValidationResult`: `Success` (bool), `Reason` (enum: `ItemNotInInventory`, `NoSuchExit`, `ExitConditionNotMet`, `TargetNotPresent`, `AlreadyInState`, ...), optional `ProposedEvents` (the Game Events to apply if valid).
- `WorldFlagCondition`: `RequiredFlagKey` (string), `MustBeSet` (bool) — used by `Exit.RequiredCondition` and `GameItem.RevealCondition`.
- `NarrationResult`, `NpcResponse`, `VisualSceneSpec`: agent output contracts — see `contracts/`.
- `VisualAsset`: `SceneStateKey` (string, cache key per Decision 5/9), `BlobUri` (string, nullable while pending), `Status` (`Pending`, `Ready`, `Failed`), `GeneratedAt` (nullable). Lives in `AI.SpectrumAdventure.Domain` as a read model reference; the actual blob content is managed by `Infrastructure`.

## Entity relationship summary

```text
Game (1) ──── (1) Player ──── (1) Inventory ──── (0..*) GameItem [by ItemId]
Game (1) ──── (4) Location ──── (0..*) GameItem [by ItemId, when present in the world]
Game (1) ──── (1) Npc (MVP: exactly one, located at DarkForest)
Game (1) ──── (1) Puzzle (ForgottenTowerEntrance)
Game (1) ──── (0..*) WorldFlag
Game (1) ──── (0..*) GameEvent (append-only history)
Location (1) ──── (0..*) Exit ──── (1) Location [DestinationId]
```

## Validation rules derived from Functional Requirements

- FR-008/Exit: an `Exit` with a non-null `RequiredCondition` MUST be rejected by the Rules Engine (not silently allowed) until the referenced `WorldFlag` is set.
- FR-010: any `ObjectStateChanged`/`ItemTaken` event MUST be applied before the next `Location`/`Item` read, guaranteeing subsequent descriptions reflect the new state (no caching of stale reads across a turn boundary).
- FR-014: `ItemUsed` validation MUST check `Inventory.Contains(ItemId)` before generating the event; the Rules Engine returns `ItemNotInInventory` otherwise, and the orchestrator does not touch `Game` state.
- FR-017: the NPC Agent's context passed by the orchestrator MUST only include `KnowledgeBoundary` entries the player has legitimately unlocked so far (e.g., no future clues), reinforcing that the agent's own creativity cannot leak information not modeled here.
- FR-019/FR-020: `Puzzle.SolutionAction` evaluation MUST be a pure Domain method (`Puzzle.CanSolve(Inventory, Player.KnownClues)`), independently unit-testable without any agent involvement.
