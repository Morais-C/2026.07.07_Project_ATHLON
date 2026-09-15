# Spike_07 — Implementation Plan

> **Sequential ChangeRequests on `rest-api-v1`** — CR₁ from pristine fixture → Publish₁ → CR₂ from Publish₁ → Publish₂  
> **Host:** Minimal console + real OpenRouter LLM; **fail fast** (no Enter pauses)  
> **Baseline:** Fork Spike_06 → `Spike_07.sln`; extend host for publish-path chaining  
> **Master plan:** [ROADMAP.md](../../ROADMAP.md)  
> **Archetype spec:** [Project_Athlon_Solution_Archetype_Definition.md](../../Project_ATHLON_VisionScope/Project_Athlon_Solution_Archetype_Definition.md)  
> **Prior spike:** [Spike_06 ImplementationPlan](../Spike_06/ImplementationPlan.md) (frozen)

---

## 1. Objective

Prove **sequential governed ChangeRequests** on the same `rest-api-v1` / `mini-erp-v1` archetype:

**Can CR₂ apply onto the Publish tree produced by CR₁ (not the pristine fixture), with the same four proof gates on every hop?**

**Training goal:** Host selects a demo sequence; hop 1 uses the fixture; hop 2+ use a prior `Publish/{workflowId}/` path as CodeContext/Applier baseline; each hop records apply + build + OpenAPI consistency + contract tests.

---

## 2. Pre-locked decisions (2026-09-15)

| # | Decision |
|---|----------|
| L1 | **Sequence:** Spike_07 **before** promotion / portal / API. Spike_01–06 frozen. |
| L2 | **Fork by copy:** `src/Spike_07/` from Spike_06. Do not modify Spike_01–06. |
| L3 | **Archetype + fixture:** Reuse `rest-api-v1` pack and `mini-erp-v1` fixture from Spike_06 (no new pack id). |
| L4 | **Two hops only:** CR₁ then CR₂. No third hop, no merge/rebase of Publish folders. |
| L5 | **Hop 1 baseline:** Pristine `fixtures/mini-erp-v1/` (same as Spike_06). |
| L6 | **Hop 2 baseline:** **Publish path** from CR₁ — `Publish/{workflowId}/` on disk. **Not** the pristine fixture. |
| L7 | **Baseline mechanism:** Config key `Athlon:BaselinePublishPath` (relative to spike root). When empty and `Athlon:BaselineSource` = `fixture`, use fixture. When set, CodeContext + Applier read that folder. **Do not** resolve baseline by artifact id lookup in this spike. |
| L8 | **Demo sequence (locked):** CR₁ = `add-product-resource`; CR₂ = `add-customer-resource` (ids from Spike_06 `demos/change-requests.json`). |
| L9 | **Chain config:** `Athlon:DemoSequence` = JSON array of demo ids in order, e.g. `["add-product-resource", "add-customer-resource"]`. Host runs hops sequentially; after each hop, records publish path for the next. |
| L10 | **Proof pipeline (each hop):** (1) apply OK → (2) `dotnet build` → (3) OpenAPI valid + operations covered by contract tests → (4) `dotnet test` on contract-test project. Same gates as Spike_06. |
| L11 | **Immutable Publish:** Each hop writes a **new** `Publish/{workflowId}/`. Never overwrite a prior Publish tree. |
| L12 | **Out of scope:** Promotion, portal/API, git PR output, auth, EF/SQL, new archetypes, Tester LLM agent. |

---

## 3. Scope boundaries

### In scope

| Area | Deliverable |
|------|-------------|
| Fork | `Spike_07.sln` copied from Spike_06 |
| Host/config | Demo sequence runner; baseline source switch (fixture vs publish path) |
| CodeContext / Applier | Accept publish-path baseline for hop 2 |
| Tests | Two-hop chain test — both hops’ proof gates asserted |
| Live demo | Product then customer on evolving tree |
| Docs | README, AGENTS, this plan; ROADMAP update on completion |

### Out of scope

| Area | Deferred |
|------|----------|
| New packs or fixtures | Spike_06 assets reused |
| Promotion / `Athlon.*` | After Spike_07 |
| Portal / API | PoC Sprint 1 |
| 3+ hop chains | Future spike if needed |
| Baseline by artifact id | Path-only for simplicity |

---

## 4. Architecture

```text
Hop 1 — CR₁ (add-product-resource)
──────────────────────────────────
  BaselineSource: fixture
  fixtures/mini-erp-v1/
        ↓
  ChangeRequest → Analyst → CodeContext → Planner → Coder → Applier
        ↓
  Publish/{workflowId₁}/     ← Publish₁ (products + health)
        ↓
  proof: apply → build → openapi → contract tests  ✅

Hop 2 — CR₂ (add-customer-resource)
──────────────────────────────────
  BaselineSource: publish
  BaselinePublishPath: Publish/{workflowId₁}/
        ↓
  ChangeRequest → Analyst → CodeContext → Planner → Coder → Applier
        ↓
  Publish/{workflowId₂}/     ← Publish₂ (products + customers + health)
        ↓
  proof: apply → build → openapi → contract tests  ✅
```

**Config sketch (appsettings):**

```json
{
  "Athlon": {
    "ArchetypeId": "rest-api-v1",
    "DemoSequence": ["add-product-resource", "add-customer-resource"],
    "BaselineSource": "fixture",
    "BaselinePublishPath": ""
  }
}
```

After hop 1 completes, host sets `BaselineSource` = `publish` and `BaselinePublishPath` = `Publish/{workflowId₁}/` before starting hop 2 (in-process or via recorded chain state file — implementer picks one; must be deterministic and testable).

---

## 5. Phased delivery

### Phase 0 — Fork Spike_06 → Spike_07

**Goal:** `Spike_07.sln` builds and tests green **unchanged** from Spike_06; docs already present.

| Task | Output |
|------|--------|
| Copy `src/Spike_06/` → `src/Spike_07/` | `Spike_07.sln`, renamed projects if needed |
| Rename solution / paths in docs | Spike_07 identity |
| Verify `dotnet build` + `dotnet test` | Same pass count as Spike_06 at fork time |
| Spike_01–06 untouched | Frozen archives |

**Exit:** Build + test green on fork; no behavioral changes yet.

---

### Phase 1 — Host/config: demo sequence + publish-path baseline

**Goal:** Host can run a configured demo sequence; hop 2 reads CR₁ Publish folder as baseline.

| Task | Output |
|------|--------|
| Add `Athlon:DemoSequence` config + parser | Ordered demo ids |
| Add `Athlon:BaselineSource` + `Athlon:BaselinePublishPath` | Fixture vs publish switch |
| Wire CodeContext builder to use publish path when configured | Hop 2 sees product endpoints from Publish₁ |
| Wire Applier to copy/apply onto publish path baseline | New Publish₂ folder |
| After hop 1, record `Publish/{workflowId}/` for hop 2 | Chain state (in-memory or small JSON sidecar under `artifacts/`) |
| Fail fast if publish path missing or invalid | Non-zero exit |

**Exit:** Manual or scripted two-hop run completes config wiring (tests may still be single-hop).

---

### Phase 2 — Tests for two-hop chain

**Goal:** Automated test proves both hops and all eight gate checks (four per hop).

| Task | Output |
|------|--------|
| Handwritten PatchPackages or fixture-based chain test | `TwoHopChainTests` (name flexible) |
| Hop 1: fixture baseline → Publish₁; assert 4 gates | Green |
| Hop 2: Publish₁ baseline → Publish₂; assert 4 gates | Green |
| Assert CR₂ tree contains **both** product and customer surfaces | Regression guard |
| `dotnet test Spike_07.sln` green | Full suite |

**Exit:** CI-style test covers sequential baseline without live LLM (deterministic patches acceptable).

---

### Phase 3 — Live demo + docs + ROADMAP

**Goal:** Live LLM chain: add product then add customer; Spike_07 marked complete.

| Task | Output |
|------|--------|
| Live demo with `DemoSequence` | Both hops OK under `Publish/` |
| Update README success criteria | All checked |
| Update ROADMAP + root AGENTS.md | Spike_07 complete; promotion next |
| Update this checklist | All phases ✅ |

**Exit:** README success criteria checked; promotion unblocked.

---

## 6. Testing strategy

| Layer | Approach |
|-------|----------|
| Fork regression | `dotnet build` + `dotnet test` unchanged after Phase 0 |
| Single-hop (inherited) | Spike_06 tests still green on fork |
| Two-hop chain | Deterministic patches: fixture → Publish₁ → Publish₂; 4 gates × 2 hops |
| Baseline guard | Test fails if hop 2 accidentally uses pristine fixture (e.g. no `/products` in baseline) |
| Live LLM | Optional smoke; run `DemoSequence` end-to-end once |
| Thesis | Artifact id handoff unchanged within each hop; baseline selection is host/config only |

---

## 7. Risks

| Risk | Mitigation |
|------|------------|
| CodeContext caps exclude files added in hop 1 | Raise caps slightly or ensure publish tree layout matches fixture layout |
| Publish path stale if workflow id not recorded | Write chain state after hop 1; test asserts path exists |
| OpenAPI coverage fails on combined surface | Hop 2 contract tests must cover new **and** existing operations |
| Live LLM drift on second hop | Phase 2 deterministic test is the proof; live demo is smoke only |

---

## 8. After Spike_07 → PromotionPlan

| Milestone | Intent |
|-----------|--------|
| **Promotion** | Lift engine + `ArchetypePackLoader` + packs → `Athlon.*` ([PromotionPlan.md](../../PromotionPlan.md)) |
| **PoC Sprint 1** | Athlon.Api + Basic Portal + CI |

Sequential baseline chaining is part of what promotion should preserve (immutable Publish trees as branch metaphor).

---

## 9. Checklist tracker

> **Single source of truth for Spike_07 phase progress.**

| Phase | Status |
|-------|--------|
| 0 — Fork Spike_06 → Spike_07 | ⬜ Not started |
| 1 — Host/config: demo sequence + publish-path baseline | ⬜ Not started |
| 2 — Two-hop chain tests | ⬜ Not started |
| 3 — Live demo + docs + ROADMAP | ⬜ Not started |

---

## 10. References

| Document | Relevance |
|----------|-----------|
| [ROADMAP.md](../../ROADMAP.md) | Spike_07 before promotion |
| [Spike_06 ImplementationPlan](../Spike_06/ImplementationPlan.md) | Single-hop REST proof (frozen) |
| [Spike_06 demos/change-requests.json](../Spike_06/archetypes/rest-api-v1/demos/change-requests.json) | Locked demo ids |
| [Solution Archetype definition](../../Project_ATHLON_VisionScope/Project_Athlon_Solution_Archetype_Definition.md) | Pack components (unchanged) |
