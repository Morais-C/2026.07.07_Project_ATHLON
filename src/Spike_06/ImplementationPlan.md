# Spike_06 — Implementation Plan

> **Formal `rest-api-v1` archetype pack** — reuse Spike_05 loader → ChangeRequest on mini-ERP REST fixture → apply + build + OpenAPI + contract-test proof  
> **Host:** Minimal console + real OpenRouter LLM; **fail fast** (no Enter pauses)  
> **Baseline:** Fork Spike_05 → `Spike_06.sln`; **rest-api-only** (no `console-v1`)  
> **Master plan:** [ROADMAP.md](../../ROADMAP.md)  
> **Archetype spec:** [Project_Athlon_Solution_Archetype_Definition.md](../../Project_ATHLON_VisionScope/Project_Athlon_Solution_Archetype_Definition.md)  
> **Positioning:** [REST API competitive positioning](../../Project_ATHLON_VisionScope/Project_Athlon_REST_API_Competitive_Positioning.md)

---

## 1. Objective

Prove the first **commercial** Athlon Solution Archetype pack:

**Can the Spike_05 `ArchetypePackLoader` + change chain run a governed ChangeRequest on a bounded REST API baseline (`mini-erp-v1`), with deterministic proof beyond build?**

**Training goal:** Host defaults to `rest-api-v1`; pack resolves bounds/prompts/schemas/demos; Applier records apply + build + OpenAPI consistency + contract tests.

---

## 2. Pre-locked decisions (2026-08-05)

| # | Decision |
|---|----------|
| L1 | **Sequence:** Spike_06 **before** promotion / portal / API. Spike_01–05 frozen. |
| L2 | **Fork by copy:** `src/Spike_06/` from Spike_05. Do not modify Spike_01–05. |
| L3 | **Archetype id:** `rest-api-v1` only (host default). **No** `console-v1` pack in Spike_06. |
| L4 | **Fixture id:** `mini-erp-v1` — near-empty mini-ERP ASP.NET **Minimal API / net9** (`GET /health` + checked-in OpenAPI + contract tests). |
| L5 | **Pack location:** `archetypes/rest-api-v1/` under Spike_06 root. |
| L6 | **Ten components:** Pack MUST satisfy [formal checklist §10 components](../../Project_ATHLON_VisionScope/Project_Athlon_Solution_Archetype_Definition.md#archetype-pack-minimum-contents). |
| L7 | **Chain:** Same agents/artifact types as Spike_05 (ChangeRequest → Analyst → CodeContext → Planner → Coder → Applier). Extend Applier proof only. |
| L8 | **OpenAPI SoT:** Checked-in `openapi.yaml` (or `.json`) is the baseline contract. ChangeRequests **may patch** OpenAPI when adding/changing endpoints; consistency is evaluated on the **post-apply** publish tree. |
| L9 | **Proof pipeline (mandatory):** (1) apply OK → (2) `dotnet build` → (3) OpenAPI valid YAML/JSON **and** declared operations covered by contract tests → (4) `dotnet test` on fixture contract-test project. |
| L10 | **Contract tests:** Checked-in `WebApplicationFactory` + xUnit project in the fixture; Applier gate runs `dotnet test`. |
| L11 | **Spike bounds:** **No auth**; **in-memory store only**; **no EF/SQL**; **no extra NuGet** beyond fixture baseline needs. |
| L12 | **Demos:** Author concrete demos (add product / add customer style) in Phase 0/1 under `demos/change-requests.json`. |
| L13 | **Out of scope:** Promotion, portal/API, git PR output, client archetypes, Tester LLM agent. |
| L14 | **Host:** `Athlon:ArchetypeId` = `rest-api-v1`; fail fast if pack missing/invalid. |

---

## 3. Scope boundaries

### In scope

| Area | Deliverable |
|------|-------------|
| Pack | `archetypes/rest-api-v1/` with 10 components |
| Fixture | `fixtures/mini-erp-v1/` Minimal API + OpenAPI + contract tests |
| Loader reuse | Same `ArchetypePackLoader` (extend only if proof/manifest fields require it) |
| Applier | Gates for OpenAPI consistency + contract tests |
| Tests | Pack loader + thesis + REST fixture Applier/E2E adaptations |
| Docs | README, AGENTS, this plan; ROADMAP + VisionScope updates |

### Out of scope

| Area | Deferred |
|------|----------|
| `console-v1` in this spike | Remains proved in frozen Spike_05 |
| Promotion / `Athlon.*` | After Spike_06 |
| Portal / API | PoC Sprint 1 |
| Auth / EF / SQL | Later archetype version or future spike |

---

## 4. Architecture

```text
appsettings.json  →  archetypeId: "rest-api-v1"
        ↓
  ArchetypePackLoader  →  archetypes/rest-api-v1/archetype.json
        ↓
  ChangeRequest → Analyst → CodeContext → Planner → Coder → Applier
        ↓
  fixtures/mini-erp-v1/  →  Publish/{workflowId}/
        ↓
  proof: apply → build → openapi consistency → contract tests
```

**Pack folder:**

```text
archetypes/rest-api-v1/
  archetype.json
  bounds.md
  code-context.md
  change-request.md
  schemas/
  prompts/
  patch-apply.md
  proof/pipeline.json
  demos/change-requests.json
  README.md
```

**Fixture folder:**

```text
fixtures/mini-erp-v1/
  MiniErp/                 # Minimal API
  openapi.yaml             # checked-in contract (SoT)
  MiniErp.ContractTests/   # WebApplicationFactory + xUnit
  README.md
```

---

## 5. Phased delivery

### Phase 0 — Fork + rest-api skeleton + mini-erp fixture

**Goal:** `Spike_06.sln` builds; `rest-api-v1` pack skeleton + `mini-erp-v1` fixture on disk; Spike_01–05 untouched.

| Task | Output |
|------|--------|
| Copy Spike_05 → Spike_06; rename sln | `Spike_06.sln` |
| Remove `console-v1` + `echo-v1` | rest-api-only |
| Add `archetypes/rest-api-v1/` skeleton (10 slots) | Manifest points at `mini-erp-v1` |
| Add near-empty `fixtures/mini-erp-v1/` | Health + OpenAPI + contract tests |
| Host default `rest-api-v1` | `appsettings.json` |
| Author this plan + README + AGENTS | Docs present |
| Draft demo catalog placeholders | `demos/change-requests.json` (fill product/customer demos) |

**Exit:** `dotnet build Spike_06.sln`; fixture `dotnet build` + `dotnet test` green on baseline; pack loads by id.

---

### Phase 1 — Pack content for REST (prompts, bounds polish, demos)

**Goal:** REST-oriented prompts/bounds; demo ChangeRequests for add product / add customer.

| Task | Output |
|------|--------|
| Rewrite Analyst/Planner/Coder prompts for Minimal API + OpenAPI | Components 5–6 ready |
| Finalize bounds / change-request / code-context docs | Components 2–4 |
| Author demos (feature: add product; feature/bugfix pair as needed) | Component 10 |
| Pack README checklist complete | Manual review |

**Exit:** All 10 components present and REST-accurate.

---

### Phase 2 — OpenAPI consistency + contract-test gates in Applier

**Goal:** Applier implements L9 gates; manifest fields recorded.

| Task | Output |
|------|--------|
| Validate publish OpenAPI is valid YAML/JSON | `openapiConsistencySucceeded` |
| Check declared operations covered by contract tests | Same gate or sibling check |
| Run `dotnet test` on contract-test project | `contractTestsSucceeded` |
| Unit tests with hand-written patches on `mini-erp-v1` | Green / fail-fast cases |

**Exit:** Empty ChangeRequest (no-op / health-only) path: apply+build+openapi+tests green on baseline copy.

---

### Phase 3 — Wire host + tests to `rest-api-v1`

**Goal:** Host + test harness use pack/fixture ids; Spike_05 console-specific tests adapted or replaced.

| Task | Output |
|------|--------|
| `Program.cs` demo from pack catalog | Active demo selectable |
| `SpikeTestPaths` → `rest-api-v1` / `mini-erp-v1` | Tests resolve pack |
| Replace echo-oriented Applier/E2E fixtures | REST patches |
| Fail fast on bad `archetypeId` | Non-zero exit |

**Exit:** `dotnet test` green for adapted suite; host loads `rest-api-v1`.

---

### Phase 4 — Live demo + thesis

**Goal:** Live LLM ChangeRequest (add product or add customer) through full chain; thesis holds.

| Task | Output |
|------|--------|
| Live demo documented in README | apply + all proof gates OK |
| Thesis: LoadAsync-only handoff; pack path resolution | Tests green |
| Update ROADMAP + VisionScope archetype table | `rest-api-v1` = proved (pack) |

**Exit:** README success criteria checked; promotion unblocked.

---

## 6. Testing strategy

| Layer | Approach |
|-------|----------|
| Fixture baseline | `dotnet build` + `dotnet test` on `mini-erp-v1` without Athlon |
| Pack loader | Load `rest-api-v1`; missing/invalid fail fast |
| Applier gates | Hand-written PatchPackage → publish → build/openapi/tests |
| Thesis | Artifact id handoff; pack-resolved prompt paths |
| Live LLM | Optional smoke; one in-bounds resource add |

---

## 7. Risks

| Risk | Mitigation |
|------|------------|
| OpenAPI↔test coverage check is underspecified | ✅ Algorithm pinned in Phase 2 (see §7.1) |
| Contract tests slow / flaky under Applier | Timeout applied; tests rebuild from patched source |
| Prompt reuse from console misguides agents | Phase 1 rewrite complete |
| Caps too tight for multi-file resource adds | Phase 0 caps start at 16 files / 64k chars; tune if needed |

### 7.1 Operation coverage algorithm (Phase 2)

The `OpenApiValidator.CheckOperationCoverage` algorithm determines if each declared OpenAPI operation is covered by contract tests:

1. **Parse OpenAPI**: Extract all operations (path + method + optional operationId) from `paths` section
2. **Load test content**: Read all `.cs` files from the contract test directory
3. **For each operation**, check coverage via heuristics:
   - `operationId` reference in test code (case-insensitive)
   - Exact path string in quotes (e.g., `"/health"`)
   - Normalized path pattern (parameters replaced with regex)
   - Path prefix without parameters
4. **Result**: All operations must have at least one coverage signal; missing coverage → gate fails

This is a heuristic suitable for the spike; production may use explicit test-to-operation mapping.

---

## 8. After Spike_06

| Milestone | Intent |
|-----------|--------|
| **Promotion** | Lift engine + `ArchetypePackLoader` + packs → `Athlon.*` ([PromotionPlan.md](../../PromotionPlan.md)) |
| **PoC Sprint 1** | Athlon.Api + Basic Portal + CI |

---

## 9. Checklist tracker

> **Single source of truth for Spike_06 phase progress.**

| Phase | Status |
|-------|--------|
| 0 — Fork + rest-api skeleton + mini-erp fixture | ✅ Complete (2026-08-05) |
| 1 — Pack content (prompts, demos) | ✅ Complete (2026-09-11) |
| 2 — Applier OpenAPI + contract-test gates | ✅ Complete (2026-09-11) |
| 3 — Host + tests wired to rest-api-v1 | ⬜ Not started |
| 4 — Live demo + thesis + docs | ⬜ Not started |

---

## 10. References

| Document | Relevance |
|----------|-----------|
| [ROADMAP.md](../../ROADMAP.md) | Spike_06 before promotion |
| [Spike_05 ImplementationPlan](../Spike_05/ImplementationPlan.md) | Loader + pack model (frozen) |
| [Solution Archetype definition](../../Project_ATHLON_VisionScope/Project_Athlon_Solution_Archetype_Definition.md) | 10 pack components |
| [REST API positioning](../../Project_ATHLON_VisionScope/Project_Athlon_REST_API_Competitive_Positioning.md) | Commercial SKU framing |
