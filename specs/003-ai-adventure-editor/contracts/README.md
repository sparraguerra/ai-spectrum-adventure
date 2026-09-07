# Contracts: AI Adventure Editor

All authoring contracts are internal boundaries. The UI submits author intent; deterministic Application code validates and persists drafts/versions; the Authoring Proposal Agent emits only non-authoritative proposal data.

| Contract | Direction | Authority |
|---|---|---|
| `adventure-draft-request.schema.json` | UI -> Application | Author-requested mutable draft change. |
| `authoring-validation-result.schema.json` | Application -> UI | Deterministic validation result. |
| `authoring-proposal.schema.json` | Agent -> Application/UI | Pending, reviewable proposal only. |
| `publish-adventure-result.schema.json` | Application -> UI | Immutable version creation result. |
| `playtest-result.schema.json` | Application -> UI | Validated playtest session result. |

Unexpected fields are rejected. No proposal contract contains a publish command or direct persistence instruction.