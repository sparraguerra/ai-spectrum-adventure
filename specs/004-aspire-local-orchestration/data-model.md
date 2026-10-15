# Data Model: Aspire Local Orchestration

This feature does not add gameplay or production persistence entities. Its model describes the developer-local runtime topology.

## Local Application Topology

| Field | Description | Validation |
|-------|-------------|------------|
| Entry point | The AppHost launched by the developer | Must be the documented local startup path. |
| Web resource | The existing `AI.SpectrumAdventure.Web` project | Must reference the database and blob resources and expose a browser-reachable endpoint. |
| Diagnostic view | Aspire dashboard associated with the AppHost | Must show all managed resources and their diagnostics. |

## Local Resource

| Field | Description | Validation |
|-------|-------------|------------|
| Logical name | Stable name used by AppHost references | Must be unique within the AppHost. |
| Resource type | Web project, PostgreSQL database, blob-storage emulator, or diagnostic service | Must correspond to a required local dependency. |
| Readiness state | Starting, ready, failed, or stopped | Web readiness depends on database and blob-store readiness. |
| Connection contract | Named configuration supplied to the web project | Must not require a manually authored localhost endpoint. |
| Persisted state target | Named volume or ignored local key directory, when applicable | Must survive normal restarts and be removed only by the explicit reset workflow. |

## Developer-Supplied Configuration

| Field | Description | Validation |
|-------|-------------|------------|
| Azure AI endpoint and deployments | Existing `AzureAI` configuration values | May come from user secrets or environment variables; never from tracked configuration. |
| Azure AI credential | API key or developer identity used by the existing application | Must not be emitted in logs, dashboard output, or documentation examples. |
| Optional Application Insights connection | Existing production telemetry destination | Remains optional locally and is not required for AppHost startup. |

## Local Persisted State

| Category | Contents | Normal restart | Explicit reset |
|----------|----------|----------------|----------------|
| PostgreSQL | Games, adventures, worlds, and visual-asset metadata | Retained | Removed by reset script |
| Azurite | Generated visual blobs | Retained | Removed by reset script |
| Data Protection | Local key-ring files | Retained | Removed by reset script |

## State Transitions

```text
Stopped -> Starting -> Ready
                    -> Failed
Ready -> Stopped
Ready -> Resetting -> Stopped
```

- The web resource reaches `Ready` only after its required database and blob-store references are usable.
- A failed dependency leaves its resource in `Failed` and makes the failure visible in the dashboard; it must not trigger a local-state reset.
- `Resetting` is entered only through the documented reset script after developer confirmation.