# Quickstart: Dynamic World, Lore & Procedural Adventures

**Feature**: 002-dynamic-world-lore | **Date**: 2026-09-02

Use this guide after implementation to validate Feature 002. See [data-model.md](./data-model.md) and [contracts/README.md](./contracts/README.md) for the boundaries under test.

## Prerequisites

- .NET 10 SDK and Docker.
- Local PostgreSQL and Azurite started through the repository's `docker-compose.yml`.
- Local user secrets configured for the existing Azure AI settings if enrichment/image behavior is being exercised. Automated tests must use mock agents and require no live AI service.

## Build and test

```powershell
dotnet restore
dotnet build
dotnet test
dotnet run --project src/AI.SpectrumAdventure.Web
```

To apply the Feature 002 persistence migration to an existing local AdventureDb before manual testing:

```powershell
dotnet ef database update --project src/AI.SpectrumAdventure.Infrastructure --startup-project src/AI.SpectrumAdventure.Web
```

## Validation scenarios

### 1. Expand an unknown boundary

Start a dynamic adventure and travel from a known location through an allowed unknown direction. Expect one persisted region/location/connection, a coherent factual description, and a discovered-map update. Repeat the request after leaving and returning: expect the same location identity and connection.

### 2. Reject invalid expansion

Attempt a direction not allowed by the source location or a candidate intentionally rejected by constraints. Expect a meaningful in-world response, no new authoritative location, and continued play in known locations.

### 3. Verify concurrent generation protection

Issue two simultaneous exploration requests for the same game/world boundary using the integration test harness. Expect exactly one persisted generation key and both requests to converge on the same authoritative location.

### 4. Discover lore and use the map

Discover at least five lore entries through conversations, objects, and environmental clues. Expect each entry and location to appear only after discovery, remain available later, and never be reported as a new discovery twice.

### 5. Validate NPC boundaries

Meet at least two NPCs. Ask about their local knowledge and then about distant/private facts. Expect stable identities and personalities, relevant local answers, and no unsupported knowledge disclosure. Trigger a validated relocation and verify the NPC has one coherent current location.

### 6. Solve a linked puzzle creatively

Discover a puzzle, its prerequisites, and a follow-on result. Attempt an invalid and a plausible natural-language solution. Expect invalid attempts to change nothing; a valid deterministic solution completes the puzzle and reveals its persistent consequence or follow-on discovery.

### 7. Continue through enrichment failure

Use an enrichment/image test double that fails. Expand a valid boundary and enter it. Expect the structural location and narrative fallback to remain playable, no corruption of world state, and observable enrichment failure telemetry.

## Expected automated coverage

Run Domain tests for generation determinism, topology/terrain/lore/NPC invariants, and puzzle conditions. Run Application tests for expansion, discovery, and action pipelines. Run Integration tests for PostgreSQL transactions, duplicate generation, and agent failure fallbacks. No test may require a live model to establish world correctness.

## Deployment configuration validation

Feature 002 reuses the existing `ConnectionStrings__AdventureDb` setting and the managed identity already passed to the single Azure Container App. It adds no production secrets and no Azure resources. The existing Application Insights connection string continues to export the application, agent, and infrastructure telemetry sources.

Validate the checked-in Bicep without deploying resources:

```powershell
az bicep build --file infra/main.bicep
```