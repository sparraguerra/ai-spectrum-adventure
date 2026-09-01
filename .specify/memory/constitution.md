# AI Spectrum Adventure Constitution

**Version:** 1.1.0  
**Status:** Active  
**Platform:** .NET + Microsoft Agent Framework + Azure Container Apps

---

# Project Principles

## I. Deterministic Game State First

The game world MUST have a single authoritative source of truth.

Large Language Models and AI agents MAY propose actions, events, narrative consequences, and state transitions, but they MUST NOT be the authoritative owners of game state.

All persistent game state changes MUST be validated and applied by deterministic application code.

The game state MUST be capable of representing, at minimum:

- Player state
- Player inventory
- Locations
- Items
- NPCs
- Relationships
- Quests
- Puzzle state
- World flags
- Historical events

Narrative output MUST be derived from the current authoritative state whenever factual consistency is required.

AI-generated content MUST NOT directly bypass domain validation.

**Rationale:** AI-generated narratives can hallucinate or contradict previous events. The game engine must preserve consistency independently from the language model.

---

## II. Agent Responsibilities Must Be Explicit

Every AI agent MUST have a clearly defined responsibility.

Agents MUST NOT duplicate responsibilities without a documented reason.

The initial architecture SHOULD separate the following concerns:

- **Game Director / Narrator** — narrative interpretation and storytelling
- **NPC Agent** — character behavior and dialogue
- **Puzzle Agent** — puzzle reasoning and hint generation
- **Visual Art Director** — transformation of narrative scenes into visual specifications
- **Lore Keeper** — long-term narrative consistency
- **World Builder** — generation of world structures and background content

The orchestration layer MUST control which agents are invoked and when.

Agents SHOULD communicate through structured contracts rather than unrestricted natural-language conversations whenever practical.

Agents MUST NOT be deployed as independent infrastructure components unless there is a clear operational, scalability, isolation, or ownership requirement.

**Rationale:** Explicit responsibilities reduce agent conflicts, improve testability, and control cost and latency.

---

## III. Structured Output Over Free-Form State Mutation

Whenever an agent proposes changes to the game, the output MUST use structured data.

Agent outputs SHOULD distinguish between:

- Narrative text
- Proposed events
- Proposed state changes
- Visual requirements
- NPC reactions
- Validation requirements

Example conceptual structure:

```json
{
  "narration": "...",
  "proposedEvents": [],
  "visualScene": {},
  "npcReactions": []
}
```

The application MUST validate structured output before applying changes to the authoritative game state.

Free-form LLM text MUST NOT be parsed as the only mechanism for modifying game state.

**Rationale:** Structured contracts make the system reliable, testable, observable, and resistant to hallucinated state.

---

## IV. Rules Before Narrative

Game rules MUST take precedence over narrative generation.

The system MUST determine whether an action is possible according to the current game state and rules before permanently narrating its consequences.

The architecture SHOULD follow this flow:

```text
Player Intent
    ↓
Intent Interpretation
    ↓
Rules Validation
    ↓
State Transition
    ↓
Narrative Generation
```

The Narrator MAY creatively describe an outcome but MUST NOT invalidate established rules.

Examples:

- The player cannot use an item they do not possess.
- A dead NPC cannot participate unless the game explicitly supports resurrection.
- A locked door cannot become open without a valid state transition.
- Knowledge not acquired by the player MUST NOT be revealed without an intentional narrative mechanism.

**Rationale:** The project is a game, not an unrestricted storytelling chatbot.

---

## V. Retro 8-Bit Identity Is a Core Requirement

The project MUST preserve a recognisable retro 8-bit identity.

The primary visual inspiration SHOULD come from classic ZX Spectrum-era adventures and games.

Visual generation MUST support a constrained retro style including, where applicable:

- Limited colour palettes
- Low resolution
- Visible pixels
- Dithering
- Retro composition
- ZX Spectrum-inspired aesthetics
- Optional attribute-clash simulation

Generated visual assets SHOULD pass through a visual style validation or post-processing stage when required to preserve the intended aesthetic.

The project SHOULD avoid generic modern AI-art aesthetics when a retro equivalent can be produced.

The user experience SHOULD evoke classic 8-bit adventures while providing modern natural-language interaction.

**Rationale:** The retro identity is a fundamental product characteristic, not merely a cosmetic theme.

---

## VI. The Player Must Always Have Agency

The system MUST prioritise meaningful player agency.

The player MUST be able to express actions in natural language.

The game SHOULD support both:

- Classic adventure-style commands
- Natural-language actions

Examples:

```text
EXAMINE DOOR
OPEN CHEST
TAKE KEY
GO NORTH
```

And:

```text
I carefully inspect the door for traps.
```

The system MUST NOT force the player into a single predetermined narrative path unless explicitly designed as part of a specific scenario.

Multiple valid solutions SHOULD be supported when they are compatible with the game state and rules.

Creative player actions SHOULD be evaluated rather than rejected simply because they do not match predefined commands.

**Rationale:** The value of an AI-driven adventure is the ability to react creatively to player actions while maintaining coherent rules.

---

## VII. Memory Must Be Layered

The project MUST distinguish between different kinds of memory.

The architecture SHOULD maintain:

### 1. Authoritative Memory

- Deterministic game state
- Persistent facts
- Inventory
- NPC state
- Quest state

### 2. Narrative Memory

- Recent events
- Conversation context
- Important story moments

### 3. Lore Memory

- World history
- Factions
- Geography
- Mythology
- Established facts

Memory retrieval MUST be scoped to the agent and task that require it.

The system SHOULD avoid sending the complete history of the game to every model invocation.

Memory systems MUST NOT replace the authoritative game state.

**Rationale:** Layered memory improves consistency, reduces token usage, and allows long-running adventures.

---

## VIII. Testable AI Behavior

AI functionality MUST be testable.

The project MUST support deterministic testing of:

- Rules
- State transitions
- Inventory changes
- Quest progression
- Puzzle validation
- Agent contracts

AI-generated behavior SHOULD be evaluated using representative test scenarios.

Critical narrative scenarios SHOULD have regression tests covering:

- Contradictions
- Impossible actions
- Inventory inconsistencies
- NPC knowledge boundaries
- Puzzle exploits
- Repeated player actions

LLM providers MUST be abstracted sufficiently to allow testing with mocked or simulated responses.

**Rationale:** Agentic applications require systematic evaluation rather than relying solely on manual prompt testing.

---

## IX. Observability Is a First-Class Feature

Every significant agent execution MUST be observable.

The system SHOULD record relevant execution information, including:

- Agent invoked
- Workflow invoked
- Input context metadata
- Tool calls
- Structured output
- Validation result
- State changes
- Execution duration
- Errors
- Model usage where available

Sensitive information MUST NOT be unnecessarily persisted in logs or telemetry.

The architecture SHOULD make it possible to reconstruct why a particular game event occurred.

Distributed tracing MUST be supported when the application evolves into multiple services.

OpenTelemetry SHOULD be the standard abstraction for telemetry.

Azure Application Insights SHOULD be used as the primary production observability destination when deployed to Azure.

**Rationale:** Multi-agent systems are difficult to debug without visibility into orchestration and decision-making.

---

## X. Cost and Latency Must Be Managed

The game SHOULD minimise unnecessary model calls.

The system MUST NOT invoke multiple agents when deterministic code can resolve the operation.

Potential optimisation strategies include:

- Caching generated images
- Reusing location descriptions
- Summarising narrative history
- Invoking NPC agents only when relevant
- Using smaller models for classification tasks
- Using structured workflows instead of unrestricted agent conversations

Visual generation SHOULD occur only when:

- The player enters a materially new scene.
- The visual state of the location changes significantly.
- A narrative event explicitly requires a new image.

Long-running operations SHOULD NOT unnecessarily block the player's main interaction loop.

**Rationale:** Interactive gameplay requires acceptable responsiveness and sustainable operational cost.

---

# XI. Cloud-Native Architecture First

The production deployment target for the project MUST be Microsoft Azure.

The primary application hosting platform MUST be **Azure Container Apps**.

The application MUST be designed as a containerized cloud-native application.

Every deployable application component MUST be capable of running inside a container.

The architecture SHOULD follow twelve-factor application principles where practical.

Configuration MUST be externalized from application binaries.

The application MUST NOT depend on local filesystem persistence for production game state.

Persistent state MUST be stored in external managed services.

**Rationale:** Azure Container Apps provides managed container hosting, scaling, revisions, networking, and cloud-native capabilities without requiring the operational complexity of managing Kubernetes directly.

---

# XII. Azure Container Apps as the Primary Hosting Platform

Azure Container Apps MUST be the default production hosting environment.

The initial MVP SHOULD favour a simple architecture consisting of a small number of Container Apps.

The project MUST NOT prematurely deploy every agent as an independent Container App.

The initial architecture SHOULD prioritize:

- Low operational complexity
- Fast deployment
- Easy debugging
- Cost efficiency
- Clear observability

The preferred initial deployment model is:

```text
Azure Container Apps Environment
│
├── Adventure Web / API
│   │
│   ├── Blazor UI
│   ├── ASP.NET Core API
│   ├── Agent Orchestration
│   ├── Microsoft Agent Framework
│   └── Deterministic Rules Engine
│
└── Supporting Azure Services
```

Independent Container Apps SHOULD only be introduced when there is a measurable benefit such as:

- Independent scaling
- Long-running background processing
- Resource isolation
- Different deployment cadence
- Different compute requirements
- Independent ownership boundaries

**Rationale:** The project should evolve from a modular monolith toward distributed services only when justified.

---

# XIII. Progressive Architecture Evolution

The project MUST avoid premature microservices.

The initial implementation SHOULD be a modular monolith with clear internal boundaries.

The application SHOULD be designed so that components can later be extracted into independent services.

The expected evolution is:

### Phase 1 — Modular Monolith

```text
Adventure Container App
│
├── UI
├── API
├── Agent Orchestrator
├── Agents
└── Game Engine
```

### Phase 2 — Asynchronous Workers

```text
Adventure API
      │
      ▼
Events
      │
 ┌────┴─────┐
 ▼          ▼
Visual     Music
Worker     Worker
```

### Phase 3 — Distributed World Services

```text
Adventure Platform
│
├── Adventure API
├── Visual Worker
├── Music Worker
├── World Generation Worker
└── Background Processing
```

The extraction of a component into an independent Container App MUST have a documented reason.

**Rationale:** Architectural complexity should follow real product needs.

---

# XIV. Asynchronous Processing and Events

Operations that may take significant time SHOULD be processed asynchronously when doing so improves player experience.

Examples include:

- Image generation
- Image post-processing
- Music generation
- World generation
- Background summarization
- Non-critical lore processing

The player's primary interaction loop SHOULD prioritize fast narrative responses.

The preferred conceptual flow is:

```text
Player Action
      ↓
Rules Engine
      ↓
Game State Update
      ↓
Narrative Response
      ↓
Player Receives Result
```

Secondary operations MAY continue asynchronously:

```text
Game Event
    │
    ├── Generate Image
    ├── Generate Music
    └── Update Lore Summary
```

Event-driven communication SHOULD be considered when components are deployed independently.

Dapr MAY be introduced for:

- Pub/Sub messaging
- Service invocation
- State abstractions
- Workflow orchestration

Dapr MUST NOT be introduced solely because it is available.

Its adoption MUST solve a concrete architectural requirement.

**Rationale:** The game must remain responsive while allowing rich multimedia generation.

---

# XV. Azure Identity and Secret Management

Production workloads SHOULD use Managed Identity wherever supported.

Applications SHOULD authenticate to Azure services without embedding long-lived credentials in application configuration.

Managed Identity SHOULD be preferred for access to:

- Azure AI services
- Azure Storage
- Azure Key Vault
- Other Azure resources

Secrets MUST NOT be committed to source control.

Secrets MUST NOT be embedded in container images.

Azure Key Vault SHOULD be used for secrets that cannot be replaced by Managed Identity.

Development environments MAY use local configuration mechanisms appropriate for secure developer workflows.

**Rationale:** Cloud-native security should minimize credential management and secret exposure.

---

# XVI. Persistence and Cloud Storage

Persistent game state MUST be independent from the Container App lifecycle.

The application MUST assume that container instances are ephemeral.

The following categories of data SHOULD be externally persisted:

### Game Data

- Save games
- Game state
- Players
- NPC state
- Quests
- Events

### Generated Assets

- Generated images
- Retro-processed images
- Maps
- Audio assets

The recommended Azure architecture is:

```text
Structured Game Data
        ↓
Managed Database

Generated Assets
        ↓
Azure Blob Storage
```

The exact database technology MAY evolve, but domain models MUST remain independent from infrastructure implementations.

**Rationale:** Container replicas may restart, scale, or be replaced at any time.

---

# XVII. Scalability and Stateless Compute

Containerized application components SHOULD be stateless whenever practical.

Session state and game state MUST NOT depend on a specific Container App replica.

The system MUST support multiple replicas without causing inconsistent game state.

Concurrency-sensitive state transitions MUST be protected using appropriate persistence and concurrency mechanisms.

Scaling rules SHOULD be based on real workload characteristics.

Scale-to-zero MAY be used for non-interactive workloads where cold-start latency is acceptable.

Interactive game components SHOULD consider minimum replica configuration when low latency is required.

**Rationale:** Cloud-native applications must tolerate scaling and replica replacement.

---

# XVIII. Infrastructure as Code

Azure infrastructure MUST be reproducible.

Production infrastructure SHOULD be defined using Infrastructure as Code.

The preferred implementation MAY use:

- Bicep
- Azure Developer CLI
- Terraform

Infrastructure definitions MUST support, where appropriate:

- Resource Groups
- Azure Container Apps Environment
- Container Apps
- Managed Identity
- Networking
- Monitoring
- Storage
- Secrets and configuration references

Manual production infrastructure changes SHOULD be avoided.

**Rationale:** Reproducible infrastructure reduces configuration drift and deployment errors.

---

# XIX. Continuous Delivery

Application deployment MUST be automated.

The project SHOULD support automated pipelines for:

```text
Build
  ↓
Test
  ↓
Container Build
  ↓
Security Validation
  ↓
Deploy
  ↓
Smoke Test
```

Container images SHOULD be versioned.

Production deployments SHOULD support safe rollout mechanisms.

Azure Container Apps revisions SHOULD be used where appropriate to support:

- Rollbacks
- Revision history
- Controlled releases
- Traffic management

A deployment MUST NOT be considered complete until basic application health verification succeeds.

**Rationale:** Agentic applications and cloud infrastructure require reliable, repeatable deployment processes.

---

# XX. Microsoft Agent Framework and Cloud Boundaries

Microsoft Agent Framework MUST be used for agentic concerns where it provides a meaningful abstraction.

These concerns MAY include:

- Agent implementation
- Agent orchestration
- Workflows
- Conversations
- Tools
- Multi-agent coordination

Traditional .NET services MUST be preferred for:

- Game rules
- State validation
- Persistence
- Security
- Authorization
- Deterministic calculations

The application MUST NOT convert deterministic business logic into LLM prompts unnecessarily.

Agent Framework components MUST remain replaceable from the perspective of the core game domain.

**Rationale:** AI agents should enhance the game engine rather than replace reliable software engineering.

---

# XXI. Visual Generation Pipeline

Visual generation MUST be treated as a separate pipeline from authoritative game state.

The recommended flow is:

```text
Game State
    ↓
Narrative Context
    ↓
Visual Art Director
    ↓
Visual Scene Specification
    ↓
Image Generation
    ↓
Retro / Spectrum Processing
    ↓
Azure Blob Storage
    ↓
Player UI
```

The generated image MUST NOT become the authoritative representation of the game state.

The image SHOULD be considered a visual interpretation of the authoritative state.

The system SHOULD support regeneration of assets if:

- The visual generation model changes.
- The retro processing algorithm improves.
- An asset becomes corrupted or unavailable.

Image generation SHOULD support asynchronous execution.

**Rationale:** Separating visual generation from game state enables consistency, regeneration, and independent evolution.

---

# XXII. User Experience and Responsiveness

The player interaction loop MUST prioritize responsiveness.

The system SHOULD provide narrative feedback without waiting unnecessarily for:

- Image generation
- Music generation
- Background world generation

The UI SHOULD support progressive updates.

Examples include:

```text
Narrative Response
       ↓
Image Loading
       ↓
Image Appears
```

The player SHOULD be able to continue interacting where this does not create conflicting state transitions.

Long-running background operations MUST expose failure handling where relevant.

**Rationale:** AI-generated multimedia should enhance gameplay rather than make interaction feel slow.

---

# Technology Principles

## Primary Platform

The project MUST target modern .NET.

The initial implementation SHOULD use:

- C#
- .NET 10 or the current supported .NET version
- ASP.NET Core
- Microsoft Agent Framework
- Azure Container Apps
- Structured JSON contracts
- OpenTelemetry

Traditional .NET services SHOULD be preferred for deterministic business logic.

---

## User Interface

The initial user interface SHOULD prioritize:

- Fast iteration
- Web deployment
- Retro visual styling

Blazor SHOULD be considered the default frontend technology unless a future specification identifies a stronger requirement.

The UI MUST support:

- Narrative output
- Player input
- Generated images
- Game status
- Inventory
- Optional map
- Save/load functionality

The interface SHOULD be capable of receiving asynchronous updates from background operations.

---

# Development Workflow

The project MUST use Spec-Driven Development for significant features.

Each substantial feature SHOULD follow:

```text
Constitution
    ↓
Specify
    ↓
Clarify
    ↓
Plan
    ↓
Tasks
    ↓
Analyze
    ↓
Implement
```

Specifications MUST describe:

- User value
- Functional requirements
- Acceptance criteria
- Constraints
- Edge cases

Plans MUST describe:

- Architecture
- Agent responsibilities
- State contracts
- Persistence impact
- Azure infrastructure impact
- Scalability considerations
- Observability requirements
- Testing strategy

Implementation MUST NOT bypass unresolved architectural constraints defined by the Constitution.

---

# Definition of Done

A feature is considered complete only when:

- Functional requirements are implemented.
- Acceptance criteria pass.
- Game state changes are deterministic and validated.
- Agent contracts are defined where applicable.
- Automated tests exist for deterministic behavior.
- Relevant AI behavior has evaluation scenarios.
- Observability has been considered.
- Performance and model-call impact have been evaluated.
- The retro 8-bit identity has been preserved where applicable.
- Azure deployment impact has been considered.
- Infrastructure changes are reproducible.
- Documentation has been updated when architecture or contracts change.

For independently deployed components, Definition of Done MUST additionally consider:

- Health checks
- Scaling behavior
- Configuration
- Identity and permissions
- Telemetry
- Failure handling

---

# Governance

This Constitution defines the highest-level technical and product principles of the AI Spectrum Adventure project.

All specifications, plans, tasks, infrastructure definitions, and implementations MUST comply with these principles.

When a proposed implementation conflicts with the Constitution, one of the following MUST occur:

1. The implementation is changed to comply.
2. The specification is revised.
3. The Constitution is explicitly amended.

Constitution amendments MUST document:

- The reason for the change.
- The architectural or product impact.
- Any migration requirements.

The project SHOULD prefer:

- Simplicity over premature complexity.
- Deterministic correctness over uncontrolled AI behavior.
- Player agency over predetermined scripts.
- Modular architecture over premature microservices.
- Managed cloud services over unnecessary infrastructure management.
- Observable systems over opaque automation.

---

# Project Vision

AI Spectrum Adventure is a cloud-native, agentic conversational adventure inspired by the creativity and limitations of classic 8-bit games.

The project combines:

- Modern AI agents
- Deterministic game systems
- Natural-language interaction
- Procedural storytelling
- Retro visual aesthetics
- Cloud-native scalability

The objective is not merely to create a chatbot that tells stories.

The objective is to create a **persistent, coherent, playable world** in which artificial intelligence expands player possibilities while deterministic systems preserve the rules of the game.

**The AI creates possibilities.**

**The game engine preserves reality.**

**The player creates the adventure.**