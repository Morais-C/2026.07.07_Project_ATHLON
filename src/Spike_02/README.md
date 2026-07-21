# Spike_02 — Agent Chain via Artifacts

> **Depends on:** Spike_01 complete (frozen at [`../Spike_01/`](../Spike_01/))  
> **Decision:** [ROADMAP.md](../../ROADMAP.md) — Spike_02 **before** promotion to `Athlon.*`  
> **Host style:** Minimal console + real OpenRouter LLM (no CLI flags) — training-friendly  
> **Phase progress:** [ImplementationPlan.md §12 Checklist](./ImplementationPlan.md#12-checklist-tracker) (single source of truth)

## Purpose

Spike_02 answers Athlon’s next load-bearing question:

**Can agents chain through artifacts?**

```text
Raw business need (hardcoded sample in Program.cs)
        ↓
   BA Agent  →  StructuredRequirement (validated JSON artifact)
        ↓
   [optional Enter pause — inspect BA artifact on disk]
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

Evolve this copy: slim the console, add BA agent + structured schema, chain BA → Developer — reuse `IAgent`, `IArtifactStore`, `ILLMProvider`.

## Pre-locked decisions (do not reopen mid-spike)

| Lock | Detail |
|------|--------|
| Structured output | BA publishes `StructuredRequirement`, not Spike_01’s thin `{ text }` wrapper |
| BA rigor | Schema + fence strip + 1 retry (same as Developer) |
| Mid-chain gate | Optional Enter pause after BA (inspect on disk) — **not** a CLI `--auto-approve` flag |
| Thesis test | Developer prompt built only from `LoadAsync(id)` of BA artifact |
| Host | Minimal + real LLM: hardcoded need, `appsettings.json`, no flag parser |
| Training comments | Short step comments in `Program.cs` / agents — explain *why*, not every line |

## What this spike does *not* include

Portal, API, SQL, LangGraph, RAG, MCP, agents beyond BA + Developer, promotion to `Athlon.*`, CLI flags (`--text`, `--input`, `--load`, etc.).

## Prerequisites / config

`net9.0`, OpenRouter via `appsettings.json` + gitignored `appsettings.Local.json`.  
Copy `appsettings.Local.json.example` → `appsettings.Local.json` and set `OpenRouter:ApiKey`.  
Prefer cheap models (e.g. `deepseek/deepseek-v4-flash`) for demos.

## Running (once phases complete)

```powershell
cd src/Spike_02
dotnet run --project Athlon.Spike.Console
```

Must run with cwd = `src/Spike_02` (local `./prompts`, `./schemas`, `./artifacts`, `./appsettings*.json`).  
Edit the hardcoded sample need in `Program.cs` to try other scenarios.  
Expected: BA artifact written → (optional Enter) → Developer loads that id → Implementation artifact + telemetry.

## Success criteria

- [ ] Hardcoded raw need → BA publishes validated `StructuredRequirement` artifact
- [ ] Optional Enter pause can inspect BA artifact on disk before Developer runs
- [ ] Developer Agent loads BA output **only** via `IArtifactStore.LoadAsync(id)`
- [ ] Thesis test proves Developer prompt never contains BA raw LLM completion text
- [ ] Implementation artifact still schema-validated (1 retry) and immutable on disk
- [ ] Run prints per-agent and/or aggregated tokens, duration, est. cost
- [ ] Demo shows two artifacts in the same workflow folder (BA + Implementation)
- [ ] Console stays minimal (no CLI flag parser)

## Next after Spike_02

Promote to `Athlon.*`, then PoC Sprint 1 (API + portal). See [ROADMAP.md](../../ROADMAP.md).
