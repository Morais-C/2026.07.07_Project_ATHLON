# Spike_02 — Agent Chain via Artifacts

> **Status:** Baseline copied from Spike_01 — implement per [ImplementationPlan.md](./ImplementationPlan.md)  
> **Depends on:** Spike_01 complete (frozen at [`../Spike_01/`](../Spike_01/))  
> **Decision:** [ROADMAP.md](../../ROADMAP.md) — Spike_02 **before** promotion to `Athlon.*`

## Purpose

Spike_02 answers Athlon’s next load-bearing question:

**Can agents chain through artifacts?**

```text
Raw business need
        ↓
   BA Agent  →  StructuredRequirement (validated JSON artifact)
        ↓
   [optional approve — inspect BA artifact on disk]
        ↓
   Developer Agent  →  loads StructuredRequirement **by id only**
        ↓
   ImplementationArtifact (validated JSON)
        ↓
   File Artifact Store (immutable)
```

Spike_01 proved one agent → one artifact. Spike_02 proves **agent → artifact → agent** with no shared chat thread.

## Baseline strategy

`src/Spike_02/` started as a **100% copy** of Spike_01 (solution renamed `Spike_02.sln`).  
Keep project names `Athlon.Spike.*` inside this folder. **Do not modify `src/Spike_01/`.**

Evolve this copy: add BA agent, structured schema, new workflow — reuse existing `IAgent`, `IArtifactStore`, `ILLMProvider`.

## Pre-locked decisions (do not reopen mid-spike)

| Lock | Detail |
|------|--------|
| Structured output | BA publishes `StructuredRequirement`, not Spike_01’s thin `{ text }` wrapper |
| BA rigor | Schema + fence strip + 1 retry (same as Developer) |
| Mid-chain gate | Stub approve after BA, before Developer |
| Thesis test | Developer prompt built only from `LoadAsync(id)` of BA artifact |
| Workflow | `BusinessNeedToImplementationWorkflow` |

## What this spike does *not* include

Portal, API, SQL, LangGraph, RAG, MCP, agents beyond BA + Developer, promotion to `Athlon.*`.

## Prerequisites / config

Same as Spike_01: `net9.0`, OpenRouter via `.env` (`OPENROUTER_API_KEY`, `OPENROUTER_MODEL`).  
Copy `.env.example` → `.env` (gitignored). Prefer cheap models (e.g. `deepseek/deepseek-v4-flash`) for demos.

## Running (once Spike_02 phases complete)

```powershell
cd src/Spike_02
dotnet run --project Athlon.Spike.Console -- --text "As an employee I want meal allowance..." --auto-approve
```

Expected: BA artifact written → (auto)approve → Developer loads that id → Implementation artifact + telemetry.

## Success criteria

- [ ] Raw need in → BA publishes validated `StructuredRequirement` artifact
- [ ] Optional mid-chain approve can inspect BA artifact on disk before Developer runs
- [ ] Developer Agent loads BA output **only** via `IArtifactStore.LoadAsync(id)`
- [ ] Thesis test proves Developer prompt never contains BA raw LLM completion text
- [ ] Implementation artifact still schema-validated (1 retry) and immutable on disk
- [ ] Run prints per-agent and/or aggregated tokens, duration, est. cost
- [ ] 5-minute demo shows two artifacts in the same workflow folder (BA + Implementation)

## Next after Spike_02

Promote to `Athlon.*`, then PoC Sprint 1 (API + portal). See [ROADMAP.md](../../ROADMAP.md).
