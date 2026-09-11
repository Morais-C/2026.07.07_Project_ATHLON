# Project Athlon — Execution Roadmap

> **Living master plan** for build order (distinct from the VisionScope manuscript).  
> **Last updated:** 2026-08-05  
> **Manuscript:** [Project_ATHLON_VisionScope/INDEX.md](./Project_ATHLON_VisionScope/INDEX.md)

---

## Current status

| Milestone | Status |
|-----------|--------|
| **Spike_01 — Artifact Slice** | ✅ Complete (2026-07-15) — **frozen archive** |
| **Spike_02 — Agent chain via artifacts** | ✅ Complete (2026-07-24) — **frozen archive** |
| **Spike_03 — Console publish via artifacts** | ✅ Complete (2026-07-29) — **frozen archive** |
| **Spike_04 — Change existing console via artifacts** | ✅ Complete (2026-07-31) — **frozen archive** — [checklist](./src/Spike_04/ImplementationPlan.md#9-checklist-tracker) |
| **Spike_05 — Archetype packs (`console-v1`)** | ✅ Complete (2026-08-04) — **frozen archive** — [checklist](./src/Spike_05/ImplementationPlan.md#9-checklist-tracker) |
| **Spike_06 — `rest-api-v1` archetype pack** | 🔄 In progress — Phase 0–3 ✅, next Phase 4 ([plan](./src/Spike_06/ImplementationPlan.md)) |
| **Promotion to `Athlon.*`** | ⬜ After Spike_06 — [PromotionPlan.md](./PromotionPlan.md) |
| **PoC Sprint 1 — API + Basic Portal** | ⬜ After promotion |

Spike checklists live under each spike; promotion progress lives in [PromotionPlan.md](./PromotionPlan.md) (blocked until Spike_06).

---

## Decision log

### 2026-07-31 — Archetype packs before promotion (Spike_05 / Spike_06)

**Decision:** Insert **Spike_05** (formal `console-v1` pack + loader) and **Spike_06** (`rest-api-v1` pack) **before** promotion and PoC Sprint 1.

**Primary concern:** *Can the proven Spike_04 engine run from versioned Athlon Solution Archetype packs, and can a second pack (`rest-api-v1`) reuse the same loader?*

**Order:**

```text
1. Spike_01–04 (done, frozen) — artifact chain through incremental console change
2. Spike_05 (next) — archetypes/console-v1/ pack + ArchetypePackLoader; regression = Spike_04 via pack
3. Spike_06 — archetypes/rest-api-v1/ pack; OpenAPI + build + contract-test proof
4. Promote engine + pack model → Athlon.*
5. PoC Sprint 1 — Athlon.Api + Basic Portal + CI
```

**Why before promotion:** Promotion should lift a **productized archetype model** (packs + loader), not spike-root hardcoded paths. `console-v1` behavior exists in Spike_04; the **pack asset** does not.

**Vision:** [Solution Archetype definition](./Project_ATHLON_VisionScope/Project_Athlon_Solution_Archetype_Definition.md) · [REST API positioning](./Project_ATHLON_VisionScope/Project_Athlon_REST_API_Competitive_Positioning.md)

**Locks (summary):** fork Spike_04 → Spike_05; pack at `archetypes/console-v1/`; same chain as Spike_04; 10 pack components required; fail fast.  
Full locks: [Spike_05 ImplementationPlan §2](./src/Spike_05/ImplementationPlan.md#2-pre-locked-decisions-2026-07-31).

### 2026-07-29 — Spike_04 before promotion (incremental change)

**Decision:** Insert **Spike_04** after Spike_03 and **before** promotion / PoC Sprint 1.

**Primary concern:** *Can agents change an existing console via artifacts (not chat), ending in a new buildable Publish tree?*

**Order:**

```text
1. Spike_01 (done, frozen) — single agent → artifact
2. Spike_02 (done, frozen) — BA → StructuredRequirement → Developer → Implementation
3. Spike_03 (done, frozen) — Analyst → Planner → Coder → Publisher → Publish/{workflowId}/
4. Spike_04 (done) — Fixture + ChangeRequest → … → PatchPackage → Applier → Publish/{workflowId}/
5. Spike_05 — console-v1 archetype pack + loader (superseded 2026-07-31 for steps after Spike_04)
6. Spike_06 — rest-api-v1 archetype pack
7. Promote proven code → Athlon.*
8. PoC Sprint 1 — Athlon.Api + Basic Portal + CI
```

**Locks (summary):** checked-in fixture; ChangeRequest `feature` \| `bugfix` (one shape); CodeContext = all fixture files + caps; PatchPackage = **unified diffs**; new Publish folder per run (branch metaphor); deterministic Applier (build-only); host **fail fast** (no Enter pauses).  
Full locks: [Spike_04 ImplementationPlan §2](./src/Spike_04/ImplementationPlan.md#2-pre-locked-decisions-2026-07-29).

### 2026-07-28 — Publisher is build-only (functional tests later)

**Decision:** Spike_03 **Publisher** materializes `Publish/{workflowId}/` and runs `dotnet build` only.  
**Functional run/output checks** are deferred to a future **Tester** agent (not Coder test data; keeps Publisher deterministic).  
Amends Spike_03 lock **L10**. Details: [ImplementationPlan §2 L10](./src/Spike_03/ImplementationPlan.md#2-pre-locked-decisions-2026-07-27).

### 2026-07-27 — Spike_03 before promotion

**Decision:** Insert **Spike_03** after Spike_02 and **before** promotion / PoC Sprint 1.

**Primary concern:** *Can an artifact chain end in buildable, runnable code on disk?*

**Order (historical — superseded 2026-07-29 for steps after Spike_03):**

```text
1. Spike_01 (done, frozen) — single agent → artifact
2. Spike_02 (done, frozen) — BA → StructuredRequirement → Developer → Implementation
3. Spike_03 (next) — Analyst → Planner → Coder → Publisher → Publish/{workflowId}/
4. Promote proven code → Athlon.*
5. PoC Sprint 1 — Athlon.Api + Basic Portal + CI
```

**Baseline:** Fork by copy from Spike_02 → `src/Spike_03/`. Do not modify Spike_01 or Spike_02.

**Roster lock:** LLM Analyst, Planner, Coder; deterministic Publisher (**build**; functional run later via Tester).  
**Bounds:** net9 console, read→process→print (0+ ReadLine OK), ≤3 source files; Analyst aborts ASAP if out of scope.  
Full locks: [Spike_03 ImplementationPlan §2](./src/Spike_03/ImplementationPlan.md#2-pre-locked-decisions-2026-07-27).

### 2026-07-17 — Spike_02 before promotion

**Decision:** Do **Spike_02 first**; **defer** promotion of spike code into `Athlon.*` and PoC Sprint 1 (API/portal) until Spike_02 succeeds.

**Primary concern:** *Can agents chain through artifacts?*

**Order (historical — superseded 2026-07-27 for steps after Spike_02):**

```text
1. Spike_01 (done, frozen) — single Developer Agent loop
2. Spike_02 (done) — BA → StructuredRequirement → Developer → Implementation
3. Promote proven code → Athlon.*
4. PoC Sprint 1 — Athlon.Api + Basic Portal + CI
```

### 2026-07-17 — Spike_02 pre-Phase-0 locks (from review)

| # | Decision |
|---|----------|
| L1 | **Fork by copy:** `src/Spike_02/` starts as a full copy of Spike_01 (separate solution). Spike_01 stays a working archive. Evolve Spike_02 in place (`Athlon.Spike.*` project names kept inside the Spike_02 folder). |
| L2 | **Split raw need from structured requirement:** do not overload Spike_01’s thin `{ text }` wrapper as the BA output. Chain is `RawNeed` → BA → **`StructuredRequirement`** (JSON schema) → Developer → `Implementation`. |
| L3 | **BA has same rigor as Developer:** schema + fence strip + **1 retry** on invalid JSON; no publish until valid. |
| L4 | **Mid-chain human gate (stub):** optional **Enter pause** after BA publishes (inspect artifact on disk) before Developer runs. *(Amended 2026-07-20: no CLI `--auto-approve` / dual y/N prompts.)* |
| L5 | **Thesis test required:** prove Developer’s prompt is composed only from `LoadAsync(structuredRequirementArtifactId)` — never from BA’s raw LLM completion string. |
| L6 | **Sequence:** BA → Developer with raw need text as input (thin workflow class optional; prefer readable `Program.cs`). |

**Minimum `StructuredRequirement` fields (pin in Spike_02 plan §8):**  
`title`, `actors`, `goal`, `acceptanceCriteriaDraft`, `constraints`, `priority`

**Out of scope until after Spike_02:** portal, API, SQL, LangGraph, RAG, MCP, agents beyond BA + Developer.

### 2026-07-20 — Spike_02 host: Minimal + real LLM

**Decision:** Training spike keeps a **slim console** — hardcoded sample need, `appsettings.json` + OpenRouter, short training comments. **Drop** Spike_01 CLI flags (`--text`, `--input`, `--load`, `--auto-approve`, `--help`). *(Config amended 2026-07-20: use native `appsettings.json` / `appsettings.Local.json` instead of `.env`.)*

**Why:** Host scaffolding was drowning the thesis (agents in sequence via artifacts). Thesis requirements (schema, retry, LoadAsync-only handoff, thesis test) stay mandatory.

See [src/Spike_02/ImplementationPlan.md](./src/Spike_02/ImplementationPlan.md) §2.

---

## Spike_01 (complete) — summary

- Path: Business requirement → workflow → Developer Agent → validated `ImplementationArtifact`
- Location: [`src/Spike_01/`](./src/Spike_01/) — **do not modify**
- Docs: [README](./src/Spike_01/README.md) · [ImplementationPlan](./src/Spike_01/ImplementationPlan.md)

---

## Spike_02 (complete) — summary

| Item | Detail |
|------|--------|
| **Thesis** | Agents collaborate by exchanging immutable artifacts, not conversation logs |
| **Chain** | Raw need → **BA Agent** → StructuredRequirement → (**Enter pause**) → **Developer Agent** → Implementation |
| **Host** | Minimal + real LLM (no CLI flags) |
| **Baseline** | Full copy of Spike_01 under [`src/Spike_02/`](./src/Spike_02/) |
| **Phase progress** | [Checklist](./src/Spike_02/ImplementationPlan.md#12-checklist-tracker) — all phases ✅ |
| **Docs** | [README](./src/Spike_02/README.md) · [ImplementationPlan](./src/Spike_02/ImplementationPlan.md) · [AGENTS](./src/Spike_02/AGENTS.md) |

Success: Developer Agent’s only input is a BA-produced artifact loaded by id — not pasted chat / not BA completion text.

---

## Spike_03 (complete) — summary

| Item | Detail |
|------|--------|
| **Thesis** | Artifact chain ends in **buildable** console code under `Publish/` (functional run → future Tester) |
| **Chain** | BusinessRequirement → **Analyst** → **Planner** → **Coder** → **Publisher** (deterministic build) |
| **Host** | Minimal + real LLM; pause after each LLM agent |
| **Baseline** | Full copy of Spike_02 under [`src/Spike_03/`](./src/Spike_03/) |
| **Phase progress** | All phases ✅ — [Checklist](./src/Spike_03/ImplementationPlan.md#9-checklist-tracker) |
| **Docs** | [README](./src/Spike_03/README.md) · [ImplementationPlan](./src/Spike_03/ImplementationPlan.md) · [AGENTS](./src/Spike_03/AGENTS.md) |

Success: demo published `Publish/{workflowId}/` with `buildSucceeded: true`; functional run optional / Tester later.

---

## Spike_04 (complete) — summary

| Item | Detail |
|------|--------|
| **Thesis** | Agents change an **existing** console via artifacts → new buildable `Publish/{workflowId}/` |
| **Chain** | Fixture + ChangeRequest → **Analyst** → CodeContext → **Planner** → **Coder** → **Applier** (apply + build) |
| **Host** | Minimal + real LLM; **fail fast** (no Enter pauses) |
| **Baseline** | Full copy of Spike_03 under [`src/Spike_04/`](./src/Spike_04/) + checked-in fixture |
| **Phase progress** | All phases ✅ — [Checklist](./src/Spike_04/ImplementationPlan.md#9-checklist-tracker) |
| **Docs** | [README](./src/Spike_04/README.md) · [ImplementationPlan](./src/Spike_04/ImplementationPlan.md) · [AGENTS](./src/Spike_04/AGENTS.md) |

Success: demo applies ChangeRequest onto fixture; Applier writes `Publish/{workflowId}/` with `buildSucceeded: true`.

**Note:** Spike_04 proves `console-v1` **behavior**; formal **archetype pack** is Spike_05.

---

## Spike_05 (complete) — summary

| Item | Detail |
|------|--------|
| **Thesis** | Same Spike_04 chain runs from **`archetypes/console-v1/`** pack loaded by id |
| **Chain** | Unchanged: ChangeRequest → Analyst → CodeContext → Planner → Coder → Applier |
| **Deliverable** | `ArchetypePackLoader` + all [10 pack components](./Project_ATHLON_VisionScope/Project_Athlon_Solution_Archetype_Definition.md#archetype-pack-minimum-contents) |
| **Baseline** | Fork Spike_04 → [`src/Spike_05/`](./src/Spike_05/); fixture `echo-v1` |
| **Phase progress** | All phases ✅ — [Checklist](./src/Spike_05/ImplementationPlan.md#9-checklist-tracker) |
| **Docs** | [README](./src/Spike_05/README.md) · [ImplementationPlan](./src/Spike_05/ImplementationPlan.md) · [AGENTS](./src/Spike_05/AGENTS.md) |

Success: `dotnet test` green via pack paths (79); live demo apply + build OK from `console-v1` pack.

**Note:** `console-v1` is **proved (pack)**. Next commercial archetype is Spike_06 `rest-api-v1`.

---

## Spike_06 (in progress) — summary

| Item | Detail |
|------|--------|
| **Thesis** | Second archetype pack **`rest-api-v1`** reuses Spike_05 loader |
| **Proof** | apply + `dotnet build` + OpenAPI consistency + contract tests |
| **Fixture** | `mini-erp-v1` — near-empty mini-ERP Minimal API / net9; checked-in OpenAPI |
| **Host** | rest-api-only (`Athlon:ArchetypeId` = `rest-api-v1`) |
| **Vision** | [REST API competitive positioning](./Project_ATHLON_VisionScope/Project_Athlon_REST_API_Competitive_Positioning.md) |
| **Baseline** | Fork Spike_05 → [`src/Spike_06/`](./src/Spike_06/) |
| **Phase progress** | [Checklist](./src/Spike_06/ImplementationPlan.md#9-checklist-tracker) |
| **Docs** | [README](./src/Spike_06/README.md) · [ImplementationPlan](./src/Spike_06/ImplementationPlan.md) · [AGENTS](./src/Spike_06/AGENTS.md) |

Success: governed ChangeRequest on REST API fixture with artifact chain + deterministic proof gates.

**Locks (2026-08-05):** OpenAPI checked-in SoT (may be patched by ChangeRequest); no auth / in-memory only / no EF; demos = add product / add customer. Full locks: [ImplementationPlan §2](./src/Spike_06/ImplementationPlan.md#2-pre-locked-decisions-2026-08-05).

---

## After Spike_06 — promotion & Sprint 1

Plan: **[PromotionPlan.md](./PromotionPlan.md)** (blocked until Spike_06 — promote engine **and** pack model).

```text
Athlon.Spike.* (from Spike_06 when proven)
        →  Athlon.Contracts / Artifacts / Workflow / Agents / Llm / ArchetypePacks
Then: Athlon.Api + Basic Portal (Appendix D §D.6)
```

---

## Fresh session starter

```text
Read ROADMAP.md and src/Spike_06/AGENTS.md.
Spike_01–05 are frozen. Continue Spike_06 per src/Spike_06/ImplementationPlan.md.
Goal: rest-api-v1 pack + mini-erp-v1; reuse ArchetypePackLoader; proof = build + OpenAPI + contract tests.
No promotion. No portal/API. One phase at a time; update docs on phase end.
```
