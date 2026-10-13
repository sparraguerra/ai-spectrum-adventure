# Azure Deployment Plan

> **Status:** Deployed

Generated: 2026-10-13

---

## 1. Project Overview

**Goal:** Build and deploy the current AI Spectrum Adventure app to the existing Azure Container App, and make generated scene images anonymously readable without allowing container listing.

**Path:** Modernize Existing

**Target:** `spectrum-adventure-web` in `rg-ai-spectrum-adventure`

## 2. Requirements

| Attribute | Value |
|-----------|-------|
| Classification | Existing MVP/demo deployment |
| Scale | Small; retain current 1-2 replica limits |
| Budget | Cost-optimized; use existing resources |
| Subscription | MVP subscription (`1c19e41d-1da6-4a71-ac80-bc9a7fbfb294`), confirmed by user |
| Location | `francecentral`, confirmed from the existing app and by user |

## 3. Components Detected

| Component | Type | Technology | Path |
|-----------|------|------------|------|
| Adventure web app | Web application | ASP.NET Core .NET 10 / Blazor | `src/AI.SpectrumAdventure.Web` |
| Container image | Runtime package | Docker / Azure Container Registry | `Dockerfile` |
| Infrastructure | Existing resources | Azure Container Apps, PostgreSQL Flexible Server, Blob Storage, AI Foundry, Application Insights | `infra/` |

## 4. Recipe Selection

**Selected:** Bicep (existing IaC) plus a targeted Azure CLI image update.

**Rationale:** The repository already defines these resources with Bicep and a GitHub Actions workflow. The target app already exists. Re-running the full resource-group deployment would require the unavailable PostgreSQL admin secret and risks changing unrelated resources, so the live rollout will update only the existing storage container ACL, app environment variables, and app image. Bicep and the workflow will be kept consistent for subsequent deployments.

## 5. Architecture

**Stack:** Container App with existing managed dependencies

### Service Mapping

| Component | Azure Service | SKU / configuration |
|-----------|---------------|---------------------|
| Web app | Azure Container Apps | Existing environment, 0.5 CPU / 1 GiB, 1-2 replicas |
| Container image | Azure Container Registry | Existing Basic registry `spectrumadventureblvdr6cch7bk2` |
| Adventure data | Azure Database for PostgreSQL Flexible Server | Existing `adventure` database; preserve connection secret |
| Scene image blobs | Azure Blob Storage | Existing `visual-assets`; anonymous Blob read only, no container listing |
| AI inference | Azure AI Foundry / Azure OpenAI | Existing managed identity and endpoint settings |
| Telemetry | Application Insights / Log Analytics | Preserve existing configuration |

### Supporting Services

| Service | Purpose |
|---------|---------|
| User-assigned managed identity | Access Storage and AI without app secrets |
| Container Apps ingress | Preserve current HTTPS public endpoint and target port 8080 |

### Environment Variables

Add and persist these app settings:

| Variable | Value / source | Reason |
|----------|---------------|--------|
| `AzureStorage__AllowPublicBlobAccess` | `true` | Allows the app to apply Blob-only public access to `visual-assets` |
| `AzureStorage__PublicBlobEndpoint` | `https://stspectrumadventureblvdr.blob.core.windows.net/` | Rewrites returned image URLs to the browser-reachable storage endpoint |

Do not deploy the Compose value `http://localhost:10000` to Azure. Preserve all existing secret references and managed identity settings. The current live container ACL is `container`; narrow it to `blob`.

## 6. Provisioning Limit Checklist

No new Azure resources are planned. The existing Container App receives a new revision; the existing storage account/container settings are updated in place.

| Resource Type | Number to Deploy | Total After Deployment | Limit/Quota | Notes |
|---------------|------------------|------------------------|-------------|-------|
| Microsoft.App/containerApps | 0 | 1 | N/A; existing resource | New revision only; no new app |
| Microsoft.Storage/storageAccounts | 0 | 1 | N/A; existing resource | Public access is already enabled at account scope |
| Microsoft.Storage/storageAccounts/blobServices/containers | 0 | 1 | N/A; existing child resource | Change ACL from `container` to `blob` |
| Microsoft.ContainerRegistry/registries | 0 | 1 | N/A; existing resource | Push a uniquely tagged image only |

**Status:** No additional Azure resource quota is consumed.

## 7. Execution Checklist

### Phase 1: Planning
- [x] Analyze existing Bicep and GitHub Actions deployment flow
- [x] Confirm subscription and location with user
- [x] Confirm existing app, ACR, storage account, and current container ACL
- [x] Select existing Bicep + targeted image update recipe
- [x] User approved this completed plan

### Phase 2: Execution
- [x] Update Bicep and GitHub Actions to persist both app settings and Blob-only access
- [x] Build/test locally and compile Bicep
- [x] Build and push immutable image `spectrum-adventure:deploy-20261013-2009695fd56c-1` to the existing ACR
- [x] Set the existing container ACL to Blob read only
- [x] Update only the existing Container App image and two non-secret environment variables
- [x] Verify the new revision, app health, and an anonymously readable image URL

### Phase 3: Validation
- [x] Core CLI/auth and `az bicep build --file infra/main.bicep`
- [x] Root template `az deployment group validate` with a validation-only PostgreSQL password marker; root template is preview-only
- [x] Root template what-if reviewed; it predicted unrelated creates/modifications/deletes and will not be applied
- [x] Target template `infra/scene-assets.bicep` compiles, validates, and its what-if changes only the existing `visual-assets` container ACL
- [x] Docker build and automated tests
- [x] Static Bicep managed-identity role review
- [x] Azure Policy assignments for the target scope
- [x] Record validation proof below

### Phase 4: Deployment
- [x] Build and push the immutable image to the existing ACR
- [x] Update the existing storage container ACL to Blob read only
- [x] Update only the existing app image and two non-secret environment variables
- [x] Verify the deployed HTTPS endpoint and anonymous image read

## 8. Validation Proof

| Check | Command Run | Result | Timestamp |
|-------|-------------|--------|-----------|
| Existing local Docker Compose image flow | `docker compose up --build`; browser `naturalWidth` check | Pass; image loaded at 1536x1024 | 2026-10-13 |
| Azure target and config read | `az containerapp show`; `az storage account list`; `az storage container show` | Pass; target exists, account allows public access, container ACL is `container` | 2026-10-13 |
| .NET tests | `runTests` | Pass; 252 passed | 2026-10-13 |
| .NET web build | `dotnet build src/AI.SpectrumAdventure.Web/AI.SpectrumAdventure.Web.csproj --no-restore` | Pass | 2026-10-13 |
| Local container build | `docker build --tag ai-spectrum-adventure:azure-preflight .` | Pass | 2026-10-13 |
| Bicep compilation | `az bicep build --file infra/main.bicep` | Pass; existing lint warnings remain | 2026-10-13 |
| Azure Policy assignments | Azure Policy MCP `policy_assignment_list` at target resource group | Pass; inherited Security Center assignment is audit-only | 2026-10-13 |
| Root template validation/what-if | `validate-deployment.ps1 -Scope group ...` | Validation passed. What-if found 9 creates, 57 modifications and 34 deletions, so the root template is explicitly not applied. | 2026-10-13 |
| Targeted ACL Bicep | `az bicep build`, `az deployment group validate`, and `az deployment group what-if` for `infra/scene-assets.bicep` | Pass; only `visual-assets` is deployed, other resource IDs are ignored | 2026-10-13 |
| ACR image build | `az acr build --registry spectrumadventureblvdr6cch7bk2 --image spectrum-adventure:deploy-20261013-2009695fd56c-1 --file Dockerfile .` | Succeeded; digest `sha256:cacd27d6524bbb1b0711bb744e2f7f517d07063f4eac31f294592e0306f90fa9` | 2026-10-13 |
| Storage ACL deployment | `az deployment group create --template-file infra/scene-assets.bicep ...` | Succeeded; `visual-assets` is `blob` | 2026-10-13 |
| Container App revision | `az containerapp update ... --image ... --set-env-vars ...` | Succeeded; revision `spectrum-adventure-web--0000012`, 1 replica, Healthy | 2026-10-13 |
| Health endpoint | HTTPS `GET /health` | HTTP 200 | 2026-10-13 |
| Anonymous scene blob read | HTTPS `HEAD` to generated blob URL | HTTP 200; 2,015,399 bytes | 2026-10-13 |
| Browser scene render | Browser `naturalWidth` check | Pass; image loaded at 1536x1024 from the Azure Blob endpoint | 2026-10-13 |

**Validated by:** azure-validate workflow
**Validation timestamp:** 2026-10-13

## 9. Files to Change

| File | Purpose | Status |
|------|---------|--------|
| `.azure/deployment-plan.md` | Source of truth for deployment workflow | Deployed |
| `infra/main.bicep` | Parameterize public scene blobs and derive the production blob endpoint | Updated |
| `infra/main.json` | Generated ARM template kept in sync with `main.bicep` | Updated |
| `infra/containerapp.bicep` | Add the two app environment variables | Updated |
| `infra/scene-assets.bicep` | Isolated, existing-container ACL update | Deployed |
| `infra/scene-assets.json` | Generated ARM template for the scoped ACL update | Generated |
| `.github/workflows/deploy.yml` | Preserve the approved storage setting on future deployments | Updated |
| `specs/001-conversational-adventure-mvp/deployment-notes.md` | Document the production endpoint and public-read scope | Updated |

## Role Assignment Verification

- Status: Verified (static Bicep review)
- Identity: existing user-assigned identity `id-spectrum-adventure-blvdr6cch7bk2`
- Roles confirmed: `Storage Blob Data Contributor` scoped to the storage account; Azure AI roles scoped to the AI account
- Issues: none for the operations in this plan

## 10. Next Steps

> Current: Complete

1. App is deployed and healthy at `https://spectrum-adventure-web.salmonbay-84be1462.francecentral.azurecontainerapps.io`.
2. The adventure scene image was fetched anonymously from the configured Blob endpoint and rendered in the browser.