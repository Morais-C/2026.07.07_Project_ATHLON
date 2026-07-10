# Spike_01 — Artifact Slice

> **Status:** Planning  
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
| Vertical slice | Ch. 3, Appendix D §D.2 | One path works end-to-end in a console app |
| Human-in-the-loop (stub) | Ch. 4 | Console prompt or flag before marking workflow complete |

---

## What this spike does *not* include

- HTTP API or web portal
- SQL / SQLite (file-based store only)
- RabbitMQ, LangGraph, Docker Compose
- Memory, RAG, MCP, Git integration
- Multiple agents (BA, Architect, Reviewer, …)
- Full observability stack (structured console logs only)

See [ImplementationPlan.md](./ImplementationPlan.md) for phased delivery and promotion path to PoC Sprint 1.

---

## Solution layout (target)

```text
src/Spike_01/
  README.md
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
| [.NET 10 SDK](https://dotnet.microsoft.com/download) | Matches VisionScope reference stack (Appendix A) |
| [OpenRouter](https://openrouter.ai/) API key | Set via environment variable (see below) |
| Cursor / VS Code | Optional; recommended in playbook |

---

## Configuration

Set these environment variables before running:

```powershell
$env:OPENROUTER_API_KEY = "sk-or-..."
$env:OPENROUTER_MODEL    = "anthropic/claude-3.5-sonnet"   # or any OpenRouter model id
```

Optional:

```powershell
$env:ATHLON_ARTIFACTS_PATH = "C:\path\to\artifacts"      # default: ./artifacts under Spike_01
```

Never commit API keys. Add `artifacts/`, `.env`, and `user-secrets` paths to `.gitignore` when the solution is scaffolded.

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
5. Implementation artifact is saved; console prints artifact id and file path.
6. Optional: `--load <guid>` retrieves and displays a stored artifact.

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

- [ ] A business requirement submitted via console produces a validated `ImplementationArtifact`
- [ ] Both input and output artifacts are persisted and loadable by id
- [ ] Agent logic has no direct HTTP calls to OpenRouter (goes through `ILLMProvider`)
- [ ] Invalid LLM output is rejected; workflow does not publish a bad artifact
- [ ] A 5-minute demo can be run without explaining “it’s just ChatGPT”

---

## Next steps

1. Follow [ImplementationPlan.md](./ImplementationPlan.md) phase by phase.
2. On spike success, promote contracts and interfaces to `Athlon.*` (drop `Spike` prefix).
3. PoC Sprint 1 adds Basic Portal, CI, and optional SQL artifact store.
