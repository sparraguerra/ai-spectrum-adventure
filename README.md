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
dotnet run --project src/AI.SpectrumAdventure.Web
```

### Local database

`Game` state persists as a JSON snapshot in a single `games` table (see `AI.SpectrumAdventure.Infrastructure/Persistence`), targeting PostgreSQL in production. For local development:

```powershell
docker run --name adventure-postgres -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=adventure -p 5432:5432 -d postgres:16
dotnet user-secrets set "ConnectionStrings:AdventureDb" "Host=localhost;Database=adventure;Username=postgres;Password=postgres" --project src/AI.SpectrumAdventure.Web
```

EF Core creates the `games` table automatically via `dbContext.Database.EnsureCreated()`/migrations once a design-time migration is added (`dotnet ef migrations add InitialCreate --project src/AI.SpectrumAdventure.Infrastructure --startup-project src/AI.SpectrumAdventure.Web`); the automated test suite exercises the same repository logic against EF Core's InMemory provider, so no database is required to run `dotnet test`.

For full local setup (secrets, Azure AI configuration) and manual validation scenarios, see [specs/001-conversational-adventure-mvp/quickstart.md](specs/001-conversational-adventure-mvp/quickstart.md).

### Dynamic world validation

Feature 002 stores generated worlds, lore, NPC placement, puzzle consequences, and presentation metadata in the same `AdventureDb` connection used by the existing application. Apply the included migrations before manual testing:

```powershell
dotnet ef database update --project src/AI.SpectrumAdventure.Infrastructure --startup-project src/AI.SpectrumAdventure.Web
dotnet test
```

The test suite uses deterministic generators and test doubles; it does not require a live Azure AI endpoint. See [specs/002-dynamic-world-lore/quickstart.md](specs/002-dynamic-world-lore/quickstart.md) for Feature 002 scenarios.

## Container development

The application is packaged as a stateless .NET 10 container. Runtime configuration comes from environment variables; no connection string, Azure endpoint, or storage credential is included in the image.

```powershell
docker compose up --build
Invoke-WebRequest http://localhost:8080/health
docker compose down --volumes
```

The compose environment starts PostgreSQL and Azurite for local use. Supply real Azure settings only through your shell or a local ignored environment file:

```powershell
$env:AZURE_AI_ENDPOINT = "https://<resource>.openai.azure.com"
$env:POSTGRES_PASSWORD = "<local-password>"
docker compose up --build
```

Configuration follows ASP.NET Core environment-variable binding: `ConnectionStrings__AdventureDb`, `AzureAI__Endpoint`, `AzureAI__ChatDeployment`, `AzureAI__ImageDeployment`, `AzureStorage__ConnectionString`, and `ApplicationInsights__ConnectionString`.
