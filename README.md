# AI Spectrum Adventure

An AI-powered conversational adventure inspired by classic 8-bit text adventures and ZX Spectrum games. The MVP scenario, **The Forgotten Tower**, is specified in [specs/001-conversational-adventure-mvp/spec.md](specs/001-conversational-adventure-mvp/spec.md).

## Solution layout

```text
AISpectrumAdventure.slnx

src/
├── AI.SpectrumAdventure.Domain/          # Pure C# authoritative game model (no external dependencies)
├── AI.SpectrumAdventure.Contracts/       # Shared DTOs/contracts (ParsedIntent, ActionResult, agent outputs)
├── AI.SpectrumAdventure.Application/     # Use cases, RulesEngine, AdventureOrchestrator
├── AI.SpectrumAdventure.Infrastructure/  # EF Core persistence, Blob Storage, telemetry wiring
├── AI.SpectrumAdventure.Agents/          # Microsoft Agent Framework agents (Narrator, NPC, Visual Art Director)
└── AI.SpectrumAdventure.Web/             # Blazor Server host (composition root)

tests/
├── AI.SpectrumAdventure.Domain.Tests/
├── AI.SpectrumAdventure.Application.Tests/
├── AI.SpectrumAdventure.Agents.Tests/
└── AI.SpectrumAdventure.IntegrationTests/
```

See [specs/001-conversational-adventure-mvp/plan.md](specs/001-conversational-adventure-mvp/plan.md) for the full architecture rationale and [specs/001-conversational-adventure-mvp/tasks.md](specs/001-conversational-adventure-mvp/tasks.md) for the implementation task breakdown.

## Running locally

```powershell
dotnet restore
dotnet build
dotnet test
dotnet run --project src/AI.SpectrumAdventure.AppHost
```

The AppHost starts the web application, PostgreSQL 16, and the Azurite Blob Storage emulator. Use the dashboard endpoint printed by Aspire to open the adventure and inspect the managed resources; `<web-endpoint>/health` and `<web-endpoint>/alive` are available in Development.

PostgreSQL, Azurite blobs, and the local Data Protection key ring persist across normal AppHost restarts. To deliberately reset only that local state, stop the AppHost and run:

```powershell
pwsh ./scripts/reset-local-state.ps1
```

The script requires you to type `RESET`; it does not target Azure resources, production infrastructure, or existing `postgres-data` and `azurite-data` folders.

AI-backed narration and image generation are optional for local topology validation. Configure Azure AI values with user secrets on the web project; never add them to tracked configuration. See [specs/004-aspire-local-orchestration/quickstart.md](specs/004-aspire-local-orchestration/quickstart.md) for the complete Aspire workflow.

For the complete author workflow, including the editor example, validation, publication, version restore, AI proposal boundaries, and playtesting, see [docs/adventures/adventure-authoring-guide.md](docs/adventures/adventure-authoring-guide.md).

### Feature 003 validation and resilience

Authoring operations emit OpenTelemetry spans and metrics through the existing `AI.SpectrumAdventure.Application` source. Telemetry records operation names, outcomes, and durations, but not prompts, draft definitions, or image bytes. Retro previews are optional: a failed visual agent or image generator leaves the factual scene summary available and never blocks saving, validation, publication, or playtesting.

Run the Feature 003 checks from the repository root:

```powershell
dotnet test
az bicep build --file infra/main.bicep
```

The authoring integration suite also checks optimistic-concurrency conflicts, publish races (at most one version for a draft revision), preview fallback, and p95 save-plus-validation latency for a small draft. If Azure CLI is unavailable locally, run the `dotnet test` command and record the Bicep check as skipped; CI or an Azure CLI-enabled environment must run the Bicep validation.

### Dynamic world validation

Feature 002 stores generated worlds, lore, NPC placement, puzzle consequences, and presentation metadata in the same `AdventureDb` connection used by the existing application. Apply the included migrations before manual testing:

```powershell
dotnet ef database update --project src/AI.SpectrumAdventure.Infrastructure --startup-project src/AI.SpectrumAdventure.Web
dotnet test
```

The test suite uses deterministic generators and test doubles; it does not require a live Azure AI endpoint. See [specs/002-dynamic-world-lore/quickstart.md](specs/002-dynamic-world-lore/quickstart.md) for Feature 002 scenarios.

For local dependencies, start `AI.SpectrumAdventure.AppHost` using the Aspire workflow above.

