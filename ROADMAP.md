# Project Athlon — Execution Roadmap

> **Living master plan** for build order (distinct from the VisionScope manuscript).  
> **Last updated:** 2026-07-28  
> **Manuscript:** [Project_ATHLON_VisionScope/INDEX.md](./Project_ATHLON_VisionScope/INDEX.md)

---

## Current status

| Milestone | Status |
|-----------|--------|
| **Spike_01 — Artifact Slice** | ✅ Complete (2026-07-15) — **frozen archive** |
| **Spike_02 — Agent chain via artifacts** | ✅ Complete (2026-07-24) — **frozen archive** |
| **Spike_03 — Console publish via artifacts** | 🔄 In progress — Phases 0–4 ✅; **next Phase 5** — [checklist](./src/Spike_03/ImplementationPlan.md#9-checklist-tracker) |
| **Promotion to `Athlon.*`** | ⬜ After Spike_03 — [PromotionPlan.md](./PromotionPlan.md) |
| **PoC Sprint 1 — API + Basic Portal** | ⬜ After promotion |

Spike checklists live under each spike; promotion progress lives in [PromotionPlan.md](./PromotionPlan.md).

---

## Decision log

### 2026-07-28 — Publisher is build-only (functional tests later)

**Decision:** Spike_03 **Publisher** materializes `Publish/{workflowId}/` and runs `dotnet build` only.  
**Functional run/output checks** are deferred to a future **Tester** agent (not Coder test data; keeps Publisher deterministic).  
Amends Spike_03 lock **L10**. Details: [ImplementationPlan §2 L10](./src/Spike_03/ImplementationPlan.md#2-pre-locked-decisions-2026-07-27).

### 2026-07-27 — Spike_03 before promotion

**Decision:** Insert **Spike_03** after Spike_02 and **before** promotion / PoC Sprint 1.

**Primary concern:** *Can an artifact chain end in buildable, runnable code on disk?*

**Order:**

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

## Spike_03 (in progress) — summary

| Item | Detail |
|------|--------|
| **Thesis** | Artifact chain ends in **buildable** console code under `Publish/` (functional run → future Tester) |
| **Chain** | BusinessRequirement → **Analyst** → **Planner** → **Coder** → **Publisher** (deterministic build) |
| **Host** | Minimal + real LLM; pause after each LLM agent |
| **Baseline** | Full copy of Spike_02 under [`src/Spike_03/`](./src/Spike_03/) |
| **Phase progress** | 0–4 ✅ · **next Phase 5** — [Checklist](./src/Spike_03/ImplementationPlan.md#9-checklist-tracker) |
| **Docs** | [README](./src/Spike_03/README.md) · [ImplementationPlan](./src/Spike_03/ImplementationPlan.md) · [AGENTS](./src/Spike_03/AGENTS.md) |

---

## After Spike_03 — promotion & Sprint 1

Plan: **[PromotionPlan.md](./PromotionPlan.md)** (deferred until Spike_03 succeeds).

```text
Athlon.Spike.* (from Spike_03 when proven — or Spike_02 if Spike_03 is spike-only)
        →  Athlon.Contracts / Artifacts / Workflow / Agents / Llm
Then: Athlon.Api + Basic Portal (Appendix D §D.6)
```

---

## Fresh session starter

```text
Read ROADMAP.md and src/Spike_03/AGENTS.md.
Spike_01 and Spike_02 are complete/frozen — do not modify them.
Spike_03 Phases 0–4 are done. Continue at Phase 5 per src/Spike_03/ImplementationPlan.md
(checklist §9). Publisher is build-only (L10); functional run checks deferred to Tester.
Promotion is deferred until Spike_03 succeeds. No portal/API yet.
```
