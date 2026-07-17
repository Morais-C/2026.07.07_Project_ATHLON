# Project Athlon — Execution Roadmap

> **Living master plan** for build order (distinct from the VisionScope manuscript).  
> **Last updated:** 2026-07-17  
> **Manuscript:** [Project_ATHLON_VisionScope/INDEX.md](./Project_ATHLON_VisionScope/INDEX.md)

---

## Current status

| Milestone | Status |
|-----------|--------|
| **Spike_01 — Artifact Slice** | ✅ Complete (2026-07-15) — **frozen archive** |
| **Spike_02 — Agent chain via artifacts** | ⬜ Phase 0 done — next Phase 1 (StructuredRequirement) |
| **Promotion to `Athlon.*`** | ⬜ After Spike_02 |
| **PoC Sprint 1 — API + Basic Portal** | ⬜ After promotion |

---

## Decision log

### 2026-07-17 — Spike_02 before promotion

**Decision:** Do **Spike_02 first**; **defer** promotion of spike code into `Athlon.*` and PoC Sprint 1 (API/portal) until Spike_02 succeeds.

**Primary concern:** *Can agents chain through artifacts?*

**Order:**

```text
1. Spike_01 (done, frozen) — single Developer Agent loop
2. Spike_02 (next) — BA → StructuredRequirement → Developer → Implementation
3. Promote proven code → Athlon.*
4. PoC Sprint 1 — Athlon.Api + Basic Portal + CI
```

### 2026-07-17 — Spike_02 pre-Phase-0 locks (from review)

| # | Decision |
|---|----------|
| L1 | **Fork by copy:** `src/Spike_02/` starts as a full copy of Spike_01 (separate solution). Spike_01 stays a working archive. Evolve Spike_02 in place (`Athlon.Spike.*` project names kept inside the Spike_02 folder). |
| L2 | **Split raw need from structured requirement:** do not overload Spike_01’s thin `{ text }` wrapper as the BA output. Chain is `RawNeed` → BA → **`StructuredRequirement`** (JSON schema) → Developer → `Implementation`. |
| L3 | **BA has same rigor as Developer:** schema + fence strip + **1 retry** on invalid JSON; no publish until valid. |
| L4 | **Mid-chain human gate (stub):** optional approve after BA publishes (inspect artifact on disk) before Developer runs; plus final gate before complete (same `--auto-approve` pattern). |
| L5 | **Thesis test required:** prove Developer’s prompt is composed only from `LoadAsync(structuredRequirementArtifactId)` — never from BA’s raw LLM completion string. |
| L6 | **Workflow rename:** `BusinessNeedToImplementationWorkflow` (input = raw need text, not a pre-baked BusinessRequirement). |

**Minimum `StructuredRequirement` fields (pin in plan §7):**  
`title`, `actors`, `goal`, `acceptanceCriteriaDraft`, `constraints`, `priority`

**Out of scope until after Spike_02:** portal, API, SQL, LangGraph, RAG, MCP, agents beyond BA + Developer.

---

## Spike_01 (complete) — summary

- Path: Business requirement → workflow → Developer Agent → validated `ImplementationArtifact`
- Location: [`src/Spike_01/`](./src/Spike_01/) — **do not modify for Spike_02 work**
- Docs: [README](./src/Spike_01/README.md) · [ImplementationPlan](./src/Spike_01/ImplementationPlan.md)

---

## Spike_02 (next) — summary

| Item | Detail |
|------|--------|
| **Thesis** | Agents collaborate by exchanging immutable artifacts, not conversation logs |
| **Chain** | Raw need → **BA Agent** → StructuredRequirement → (**gate**) → **Developer Agent** → Implementation |
| **Baseline** | Full copy of Spike_01 under [`src/Spike_02/`](./src/Spike_02/) |
| **Docs** | [README](./src/Spike_02/README.md) · [ImplementationPlan](./src/Spike_02/ImplementationPlan.md) · [AGENTS](./src/Spike_02/AGENTS.md) |

Success: Developer Agent’s only input is a BA-produced artifact loaded by id — not pasted chat / not BA completion text.

---

## After Spike_02 — promotion & Sprint 1

```text
Athlon.Spike.* (from Spike_02)  →  Athlon.Contracts / Artifacts / Workflow / Agents / Llm
Then: Athlon.Api + Basic Portal (Appendix D §D.6)
```

---

## Fresh session starter

```text
Implement Spike_02 per src/Spike_02/ImplementationPlan.md.

Read ROADMAP.md, src/Spike_02/AGENTS.md and README.md first.
Phase 0 is done (Spike_01 copy builds). Start at Phase 1 — StructuredRequirement.
Complete one phase at a time; stop for human verification.
Do not modify src/Spike_01. Do not promote to Athlon.*. No portal/API.
```
