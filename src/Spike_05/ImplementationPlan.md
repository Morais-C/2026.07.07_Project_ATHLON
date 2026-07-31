# Spike_05 — Implementation Plan

> **Formal `console-v1` archetype pack** — load pack by id → same Spike_04 change chain → same build proof  
> **Host:** Minimal console + real OpenRouter LLM; **fail fast** (no Enter pauses)  
> **Baseline:** Fork Spike_04 → `Spike_05.sln`; extract `archetypes/console-v1/`  
> **Master plan:** [ROADMAP.md](../../ROADMAP.md)  
> **Archetype spec:** [Project_Athlon_Solution_Archetype_Definition.md](../../Project_ATHLON_VisionScope/Project_Athlon_Solution_Archetype_Definition.md)

---

## 1. Objective

Prove Athlon’s **product cornerstone** before the first commercial archetype (`rest-api-v1`):

**Can the proven Spike_04 console pipeline run entirely from a versioned `console-v1` archetype pack?**

**Secondary:** Establish pack loader, manifest schema, and folder layout that Spike_06 (`rest-api-v1`) will reuse.

**Training goal:** Host selects `archetypeId`; all bounds, prompts, schemas, proof gates, and demo ChangeRequests resolve from the pack — not from spike-root hardcoding.

---

## 2. Pre-locked decisions (2026-07-31)

| # | Decision |
|---|----------|
| L1 | **Sequence:** Spike_05 **before** Spike_06 (`rest-api-v1`) and **before** promotion. Spike_01–04 frozen. |
| L2 | **Fork by copy:** `src/Spike_05/` from Spike_04. Do not modify Spike_01–04. |
| L3 | **Archetype id:** `console-v1`. Default fixture id: `echo-v1` (unchanged baseline). |
| L4 | **Pack location:** `archetypes/console-v1/` under Spike_05 root (sibling to `src` projects). |
| L5 | **Manifest:** `archetypes/console-v1/archetype.json` — identity, paths, caps, fixture ref, proof pipeline. |
| L6 | **Ten components:** Pack MUST satisfy [formal checklist §10 components](../../Project_ATHLON_VisionScope/Project_Athlon_Solution_Archetype_Definition.md#archetype-pack-minimum-contents). |
| L7 | **Chain unchanged:** Same agents and artifact types as Spike_04; only **resolution paths** move into pack. |
| L8 | **Proof pipeline:** Gate 1 = apply OK; Gate 2 = `dotnet build` (document in `proof/pipeline.json`). |
| L9 | **Regression bar:** Spike_04 E2E + thesis tests must pass when wired through pack loader. |
| L10 | **Out of scope:** `rest-api-v1`, promotion, portal, git PR output, Tester functional run. |
| L11 | **Host:** `archetypeId` from config (`appsettings.json`) or constant; fail fast if pack missing/invalid. |
| L12 | **Demos:** Sample feature + bugfix ChangeRequests in `archetypes/console-v1/demos/` (host may still pick one active demo in Phase 4). |

---

## 3. Scope boundaries

### In scope

| Area | Deliverable |
|------|-------------|
| Pack layout | `archetypes/console-v1/` with manifest + prompts + schemas + bounds + proof + demos |
| Loader | `ArchetypePack` / `ArchetypePackLoader` resolves paths and caps by id |
| Host | Wires agents + Applier from loaded pack |
| Tests | Pack loader tests; regression of Spike_04 E2E/thesis via pack |
| Docs | README, AGENTS, this plan; ROADMAP + VisionScope updates |

### Out of scope

| Area | Deferred |
|------|----------|
| Second archetype (`rest-api-v1`) | Spike_06 |
| Multi-archetype runtime switch in one run | Optional stretch only |
| Promotion / `Athlon.*` | After Spike_06 |
| Portal / API | PoC Sprint 1 |

---

## 4. Architecture

```text
appsettings.json  →  archetypeId: "console-v1"
        ↓
  ArchetypePackLoader  →  archetypes/console-v1/archetype.json
        ↓
  (same Spike_04 chain, pack-resolved paths)
        ↓
  fixtures/echo-v1/  →  Publish/{workflowId}/
```

**Pack folder (target layout):**

```text
archetypes/console-v1/
  archetype.json           # component 1 — identity, paths, caps
  bounds.md                # component 2
  code-context.md          # component 3 (or embedded in manifest)
  change-request.md        # component 4 — profile notes
  schemas/                 # component 5
  prompts/                 # component 6 — analyst, planner, coder
  patch-apply.md           # component 7 — conventions pointer
  proof/pipeline.json      # component 8
  fixtures/ → ../../fixtures/echo-v1  OR manifest points to fixtures/echo-v1
  demos/change-requests.json   # component 10
  README.md                # pack human summary
```

Pin fixture path in Phase 0: keep `fixtures/echo-v1/` at spike root (Spike_04 layout) unless migration is trivial.

---

## 5. Phased delivery

### Phase 0 — Fork + pack skeleton

**Goal:** Spike_05 builds; empty/skeleton `console-v1` pack; Spike_01–04 untouched.

| Task | Output |
|------|--------|
| Copy Spike_04 → Spike_05 | `Spike_05.sln` builds |
| Add `archetypes/console-v1/archetype.json` skeleton | Valid manifest schema (draft) |
| Document pack layout in README | Present |

**Exit:** `dotnet build`; pack folder exists with placeholder manifest.

---

### Phase 1 — Migrate pack contents from Spike_04

**Goal:** Move prompts/schemas/bounds/demos into pack; document all 10 components.

| Task | Output |
|------|--------|
| Copy `prompts/*`, `schemas/*` into pack | Components 5–6 |
| Extract bounds from analyst prompt + Spike_04 L6 into `bounds.md` | Component 2 |
| CodeContext caps in manifest | Component 3 |
| `demos/change-requests.json` (uppercase + trim bugfix) | Component 10 |
| `proof/pipeline.json` | Component 8 |
| Pack README | Component docs complete |

**Exit:** All 10 components present per formal definition (manual review checklist).

---

### Phase 2 — ArchetypePack loader

**Goal:** Load and validate manifest; expose paths to host/agents.

| Task | Output |
|------|--------|
| `ArchetypePack` record + loader in Contracts or Workflow | Parse `archetype.json` |
| Validation: required paths exist, ids match | Fail fast on bad pack |
| Unit tests | Invalid/missing pack throws |

**Exit:** Tests load `console-v1` pack from disk in test harness.

---

### Phase 3 — Wire host + agents to pack

**Goal:** No hardcoded spike-root `prompts/` or `schemas/` in runtime path.

| Task | Output |
|------|--------|
| `Program.cs` reads `archetypeId` | From appsettings |
| Agents constructed with pack-resolved paths | Analyst, Planner, Coder |
| CodeContextBuilder uses pack caps | From manifest |
| Applier unchanged semantics | fixture id from pack |

**Exit:** Manual smoke: same demo as Spike_04 via pack paths.

---

### Phase 4 — Regression + fail fast

**Goal:** Spike_04 parity; invalid archetype id fails cleanly.

| Task | Output |
|------|--------|
| Port/adapt E2E + Applier tests | Green via pack |
| Wrong/missing `archetypeId` | Non-zero exit |
| Remove duplicate spike-root prompts/schemas **or** keep as deprecated with loader-only path | Pin in Phase 3/4 |

**Exit:** `dotnet test` green; host fail fast on bad pack.

---

### Phase 5 — Thesis + pack tests

**Goal:** Prove prompts still LoadAsync-only; pack id drives resolution.

| Task | Output |
|------|--------|
| Thesis tests unchanged in intent | Handoff by artifact id |
| Test: agent prompt paths come from pack manifest | Not from hardcoded constants |
| Test: switching manifest path changes prompt file used | Optional |

**Exit:** `dotnet test` green including new pack tests.

---

### Phase 6 — Demo & documentation

**Goal:** Repeatable demo; docs updated for Spike_06 handoff.

| Task | Output |
|------|--------|
| `dotnet test` + live demo via pack | Document in README |
| Update ROADMAP, VisionScope archetype table | `console-v1` = **proved (pack)** |
| Fresh-session starter for Spike_06 | In ROADMAP + AGENTS |

**Exit:** README success criteria all checked.

---

## 6. Testing strategy

| Layer | Approach |
|-------|----------|
| Manifest parse | Valid/invalid `archetype.json` |
| Loader | Missing files, wrong id |
| Regression | Spike_04 Applier + E2E tests through pack loader |
| Thesis | LoadAsync-only; pack path in agent construction |
| Live LLM | Optional smoke; same as Spike_04 |

---

## 7. Risks

| Risk | Mitigation |
|------|------------|
| Large refactor breaks Spike_04 parity | Phase 4 regression gate before Phase 5 |
| Duplicate prompts at spike root vs pack | Delete or clearly deprecate spike-root copies in Phase 3 |
| Manifest schema churn | Version field in `archetype.json`; Spike_06 extends, not rewrites |

---

## 8. After Spike_05

| Milestone | Intent |
|-----------|--------|
| **Spike_06** | Add `archetypes/rest-api-v1/` using same loader; OpenAPI + contract tests |
| **Promotion** | Lift engine + `ArchetypePackLoader` + packs → `Athlon.*` |
| **PoC Sprint 1** | Portal/API after promotion |

---

## 9. Checklist tracker

> **Single source of truth for Spike_05 phase progress.**

| Phase | Status |
|-------|--------|
| 0 — Fork + pack skeleton | ✅ Complete (2026-07-31) |
| 1 — Migrate pack contents (`console-v1`) | ⬜ Not started ← **next** |
| 2 — ArchetypePack loader | ⬜ Not started |
| 3 — Wire host + agents | ⬜ Not started |
| 4 — Regression + fail fast | ⬜ Not started |
| 5 — Thesis + pack tests | ⬜ Not started |
| 6 — Demo & docs | ⬜ Not started |

---

## 10. References

| Document | Relevance |
|----------|-----------|
| [ROADMAP.md](../../ROADMAP.md) | Spike_05/06 before promotion |
| [Spike_04 ImplementationPlan](../Spike_04/ImplementationPlan.md) | Engine reference (frozen) |
| [Solution Archetype definition](../../Project_ATHLON_VisionScope/Project_Athlon_Solution_Archetype_Definition.md) | 10 pack components |
| [REST API positioning](../../Project_ATHLON_VisionScope/Project_Athlon_REST_API_Competitive_Positioning.md) | Spike_06 / commercial SKU |
