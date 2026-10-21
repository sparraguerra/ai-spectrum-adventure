# Navigation Intent Contract

**Date**: 2026-10-21  
**Feature**: [Navigation History and Vertical Movement](../spec.md)

This contract describes the player-visible command forms and their structured interpretation. It does not change the existing web/API response shape.

## Recognized commands

| Player input | Structured action | Target |
|---|---|---|
| `back` | `Back` | None |
| `up` | `Go` | `Up` |
| `down` | `Go` | `Down` |
| `go up` | `Go` | `Up` |
| `go down` | `Go` | `Down` |
| `walk up`, `travel up`, `head up` | `Go` | `Up` |
| `walk down`, `travel down`, `head down` | `Go` | `Down` |

The listed forms are case-insensitive. Existing cardinal movement commands remain supported. The command `back` is a separate action, not a direction or a location target.

## Successful behavior

- `Back` returns to the most recently departed location represented by the active history stack, consumes that entry, and returns the usual successful action result with the new current location.
- A successful `Go Up` or `Go Down` uses the current location's matching authored exit, applies its conditions, discovers the destination according to existing movement behavior, and returns the usual successful action result.

## Failure behavior

- `Back` with no stored previous location leaves the current location unchanged and returns a clear no-previous-location message.
- `Go Up` or `Go Down` without a matching exit leaves the current location unchanged and uses the normal no-exit feedback. It does not launch unknown-boundary exploration.
- A matching exit whose condition is unmet leaves the current location and history unchanged and uses the normal blocked-exit feedback.
- Failed movement does not create or consume a previous-location entry.

## Example adventure route

In *The Tide Beneath the Harbor*, from the Leaning Lighthouse, `Down` reaches the Sluice Chamber through its authored `Down` exit. From the Sluice Chamber, `Up` reaches the Leaning Lighthouse through its authored `Up` exit.
