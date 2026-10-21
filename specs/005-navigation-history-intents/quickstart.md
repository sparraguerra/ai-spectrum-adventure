# Quickstart: Navigation History and Vertical Movement

**Date**: 2026-10-21  
**Feature**: [Navigation History and Vertical Movement](./spec.md)

## Prerequisites

- .NET 10 SDK
- Repository dependencies restored by the normal `dotnet test` workflow

## Automated validation

From the repository root, run the existing tests for the affected layers:

```powershell
dotnet test tests\AI.SpectrumAdventure.Agents.Tests\AI.SpectrumAdventure.Agents.Tests.csproj
dotnet test tests\AI.SpectrumAdventure.Application.Tests\AI.SpectrumAdventure.Application.Tests.csproj
dotnet test tests\AI.SpectrumAdventure.Domain.Tests\AI.SpectrumAdventure.Domain.Tests.csproj
dotnet test tests\AI.SpectrumAdventure.IntegrationTests\AI.SpectrumAdventure.IntegrationTests.csproj
```

Expected outcome: all four test projects pass, including coverage that:

- bare `up` / `down`, `go up` / `go down`, and mixed-case forms produce the expected structured `Go` intent;
- `back` produces a targetless `Back` intent;
- successful ordinary travel pushes history and repeated `back` actions pop locations in reverse order;
- failed movement and `back` with empty history do not change the current location or history;
- vertical exits obey their authored conditions, and absent vertical exits do not start unknown-boundary exploration;
- a save/reload retains the back-navigation stack, and older snapshots remain readable.

## Manual adventure scenario

Start *The Tide Beneath the Harbor* and issue the following commands:

1. `go north` from Stone Pier to reach Salt Street.
2. `go east` to reach Salt Marsh.
3. `go north` to reach the Leaning Lighthouse.
4. Enter `Down`; the player should reach the Sluice Chamber.
5. Enter `Up`; the player should return to the Leaning Lighthouse.
6. Enter `back`; the player should return to the Sluice Chamber, the location most recently left by successful travel.

At a location without an authored `Up` or `Down` exit, request that direction and verify the player stays in place with no-exit feedback. At the start of a new game, enter `back` and verify the player stays at the starting location with no-previous-location feedback.
