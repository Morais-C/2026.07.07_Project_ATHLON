# Spike_01 — Artifact Slice

> **Status:** Complete  
> **Current state:** Phases 0–6 implemented. Run the demo script in [ImplementationPlan.md](./ImplementationPlan.md) §5 Phase 6.  
> **Target framework:** `net9.0` for this spike (deliberate — no spike feature requires .NET 10). `net10.0` is the promotion target when code moves to `Athlon.*` per VisionScope Appendix A.  
> **VisionScope:** v1 — first executable proof of the Autonomous SDLC thesis

## Purpose

Spike_01 is the smallest end-to-end loop that proves Project Athlon’s core architectural bet:

**engineering work flows through immutable, versioned artifacts — not chat.**

```text
Business Requirement
        ↓
   Workflow (1 step)
        ↓
  Developer Agent  →  OpenRouter
        ↓
   JSON validation
        ↓
  File Artifact Store
```

This spike deliberately trades production concerns (API, portal, SQL, messaging, MCP, memory) for speed and clarity. It is a sandbox under `src/Spike_01/` until pieces are promoted into the main platform layout described in Appendix B.

---

## What this spike proves

| Principle | VisionScope reference | Spike embodiment |
|-----------|----------------------|------------------|
| Artifact-driven engineering | Ch. 8, Appendix A §A.2 | Structured JSON artifacts persisted to disk |
| Workflow-first execution | Ch. 7, Pattern 1 | Agent runs only inside a workflow instance |
| Agent runtime lifecycle | Ch. 6 | Input → prompt → LLM → validate → publish |
| Replaceable LLM provider | Ch. 5 §5.8, ADR-004 | `ILLMProvider` with OpenRouter implementation |
| Measurable execution | Ch. 6 §6.11, Ch. 5 §5.10 | Token count, duration, estimated cost printed per run |
| Vertical slice | Ch. 3, Appendix D §D.2 | One path works end-to-end in a console app |
| Human-in-the-loop (stub) | Ch. 4 | Console prompt or flag before marking workflow complete |

---

## What this spike does *not* include

- HTTP API or web portal
- SQL / SQLite (file-based store only)
- RabbitMQ, LangGraph, Docker Compose
- Memory, RAG, MCP, Git integration
- Multiple agents (BA, Architect, Reviewer, …)
- Full observability stack (OpenTelemetry, Grafana — deferred; **run-summary telemetry** on console is in scope)

See [ImplementationPlan.md](./ImplementationPlan.md) for phased delivery and promotion path to PoC Sprint 1.

---

## Solution layout (target)

```text
src/Spike_01/
  README.md
  AGENTS.md                     # Cursor / implementer entry point
  ImplementationPlan.md
  Spike_01.sln
  Athlon.Spike.Contracts/       # Artifact, workflow, agent contracts
  Athlon.Spike.Artifacts/       # IArtifactStore + FileArtifactStore
  Athlon.Spike.Workflow/        # Sequential single-step orchestrator
  Athlon.Spike.Agents/          # DeveloperAgent
  Athlon.Spike.Llm/             # ILLMProvider + OpenRouterProvider
  Athlon.Spike.Console/         # Entry point
  schemas/                      # JSON Schema for artifact validation
  prompts/                      # Versioned prompt templates
  artifacts/                    # Runtime output (gitignored)
```

Projects use the `Athlon.Spike.*` prefix so it is obvious what is experimental vs. platform code (`Athlon.Contracts`, `Athlon.Artifacts`, …).

---

## Prerequisites

| Requirement | Notes |
|-------------|-------|
| [.NET 9 SDK](https://dotnet.microsoft.com/download) | Required to build and run this spike (`net9.0`) |
| [.NET 10 SDK](https://dotnet.microsoft.com/download) | Promotion target — VisionScope reference stack (Appendix A); not required for Spike_01 |
| [OpenRouter](https://openrouter.ai/) API key | Set via environment variable (see below) |
| Cursor / VS Code | Optional; recommended in playbook |

---

## Configuration

Preferred: edit `src/Spike_01/.env` (gitignored; loaded automatically by the console):

```text
OPENROUTER_API_KEY=sk-or-v1-...
OPENROUTER_MODEL=anthropic/claude-3.5-sonnet
```

Copy from `.env.example` if needed. Shell env vars still override `.env` values.

Alternatively set variables in PowerShell before running:

```powershell
$env:OPENROUTER_API_KEY = "sk-or-..."
$env:OPENROUTER_MODEL    = "anthropic/claude-3.5-sonnet"   # or any OpenRouter model id
```

Optional:

```powershell
$env:ATHLON_ARTIFACTS_PATH = "C:\path\to\artifacts"      # default: ./artifacts under Spike_01
```

Never commit API keys — `.env` is listed in `.gitignore`.

---

## Running (once implemented)

```powershell
cd src/Spike_01
dotnet run --project Athlon.Spike.Console
```

Expected demo flow:

1. Console accepts a business requirement (stdin or `--input` file).
2. Workflow starts; input is saved as an artifact.
3. Developer Agent calls OpenRouter and produces structured output.
4. Output is validated against JSON Schema.
5. Implementation artifact is saved; console prints artifact id, file path, and **run telemetry** (tokens, duration, estimated cost).
6. Optional: `--load <guid>` retrieves and displays a stored artifact.

### Run telemetry (console)

Each completed workflow prints a short summary — no infrastructure required:

```text
Workflow complete
  Output artifact : a1b2c3d4-...
  Model           : anthropic/claude-3.5-sonnet
  Prompt tokens   : 842
  Completion tokens: 312
  Total tokens    : 1154
  Duration        : 4.2s
  Est. cost (USD) : $0.0038
```

OpenRouter returns token `usage` in the API response; cost is taken from the response when present, otherwise estimated from OpenRouter’s published per-model rates. This establishes the Ch. 6 habit — *every execution is measurable* — before Grafana exists.

---

## Artifact storage

Artifacts are stored as **immutable JSON files** on disk:

```text
artifacts/
  {workflowInstanceId}/
    {artifactId}.json
```

Each file includes metadata (id, type, version, producer, created timestamp) and payload. New versions are new files — never in-place overwrites.

This satisfies Appendix B’s `IArtifactStore` contract without a database. SQL or SQLite can be added in PoC when search and multi-client access are needed.

---

## VisionScope mapping

| Playbook milestone | Spike_01 coverage |
|--------------------|-------------------|
| Appendix D Sprint 0 | Partial — solution structure, no CI/Docker yet |
| Appendix D Sprint 1 | Core loop only — no portal |
| Appendix A §A.15 steps 1–4 | Contracts → Artifacts → Workflow → Developer Agent |

Full manuscript index: [Project_ATHLON_VisionScope/INDEX.md](../../Project_ATHLON_VisionScope/INDEX.md)

---

## Success criteria

Spike_01 is **done** when:

- [x] A business requirement submitted via console produces a validated `ImplementationArtifact`
- [x] Both input and output artifacts are persisted and loadable by id
- [x] Agent logic has no direct HTTP calls to OpenRouter (goes through `ILLMProvider`)
- [x] Invalid LLM output triggers one schema-validation retry (with errors echoed to the model); workflow fails only if the second attempt is still invalid — no bad artifact is published
- [x] Each run prints token usage, duration, and estimated cost alongside artifact ids
- [x] A 5-minute demo can be run without explaining “it’s just ChatGPT”

---

## Next steps

1. ~~Implement Spike_01~~ — **done** (Phases 0–6 + live demo).
2. **Next (decision 2026-07-17):** [Spike_02](../Spike_02/) — BA → Developer handoff via artifacts. See project [ROADMAP.md](../../ROADMAP.md).
3. **After Spike_02:** promote proven code to `Athlon.*`, then PoC Sprint 1 (API + Basic Portal + CI).

Promotion is **deferred** until Spike_02 answers: *Can agents chain through artifacts?*
