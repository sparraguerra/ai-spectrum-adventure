# Data Model: Navigation History and Vertical Movement

**Date**: 2026-10-21  
**Feature**: [Navigation History and Vertical Movement](./spec.md)

## Player

The authoritative player participating in one game.

| Field | Type | Description |
|---|---|---|
| CurrentLocationId | Location ID | The location currently occupied by the player. |
| PreviousLocationIds | Ordered list of Location IDs | Stack of origins from successful ordinary movements that have not yet been consumed by `back`. The final entry is the next destination for `back`. |
| Inventory | Existing inventory | Unchanged by this feature. |

### Invariants

- A newly created game starts with an empty previous-location stack.
- An ordinary successful movement adds its origin to the top of the stack.
- A failed or blocked movement does not change the stack.
- A successful backtracking action selects and removes exactly the top entry, then sets it as the current location.
- A backtracking action with an empty stack fails without changing player state.
- The stack contains valid location IDs belonging to the same game world.

## Movement Intent

A structured player action produced by intent interpretation.

| Field | Type | Description |
|---|---|---|
| Action | Intent action | `Go` for direction movement or `Back` for returning through recent travel. |
| Target | Optional string | Canonical direction for `Go`; absent for `Back`. |
| Parameters | String map | Existing intent parameters; unchanged. |
| RawInput | String | Original player command. |

`Go` targets include `Up` and `Down` in addition to the existing cardinal directions. Direction matching remains case-insensitive.

## Movement Events

### Ordinary movement

The existing `PlayerMovedEvent` records an ordinary successful move from one location to another. Applying it pushes the event's origin onto the player's previous-location stack before/while updating the current location.

### Backtracking movement

A new, distinguishable backtracking event records a validated return from the current location to the top of the previous-location stack. Applying it pops exactly one entry and updates the current location. The event is included in the existing append-only game event history and serialized through the existing event snapshot mapper.

## Game Snapshot

The existing `PlayerSnapshot` gains an optional ordered list of previous location IDs.

- New snapshots serialize the complete stack.
- Deserialization accepts older snapshots where the field is missing.
- For an older snapshot without the field, the stack is reconstructed by reading its movement events in chronological order: ordinary move events push their origin; backtracking events pop one entry.
- Event history remains append-only; the navigation stack is the current player projection needed for efficient subsequent actions.

No new persistence aggregate, database table, or infrastructure resource is introduced.

## Adventure Exit

An existing directed connection from one location to a destination, optionally guarded by a condition. `Up` and `Down` use the same validation and discovery behavior as other authored directions. An absent vertical exit is a normal no-exit failure, not a request to generate an unknown boundary.

## State Transitions

| Request | Preconditions | State change | Result when precondition fails |
|---|---|---|---|
| `Go <direction>` with an available exit | Exit exists and any condition is satisfied | Push origin; move to destination; apply existing discovery events | No exit or unmet condition; no location-history change |
| `Back` | Previous-location stack is non-empty | Pop top location; move to it; record backtracking event | Stay in place and report no previous location |
| `Go Up` / `Go Down` | Matching authored vertical exit exists and is available | Same as ordinary movement | Stay in place with normal no-exit or blocked-exit feedback; do not explore unknown boundary |
