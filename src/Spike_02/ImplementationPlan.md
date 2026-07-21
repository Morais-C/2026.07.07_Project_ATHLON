# Spike_02 — Implementation Plan

> **Agent Chain via Artifacts** — Raw need → BA Agent → StructuredRequirement → Developer Agent → Implementation  
> **Host:** Minimal console + real OpenRouter LLM (no CLI flags)  
> **Store:** File-based · **LLM:** OpenRouter via `ILLMProvider`  
> **Baseline:** Full copy of Spike_01 under `src/Spike_02/` (`Spike_02.sln`)  
> **Master plan:** [ROADMAP.md](../../ROADMAP.md)

---

## 1. Objective

Prove Athlon’s multi-agent thesis before promotion — **as a training spike, keep the host small**.

**Primary question:** Can agents chain through immutable artifacts (BA → Developer) with no shared chat thread?

**Secondary question:** Do Spike_01 abstractions (`IAgent`, `IArtifactStore`, `ILLMProvider`) support a second agent without breaking changes? (If they need changes, document that as a spike finding.)

**Training goal:** A reader can open `Program.cs` and see the sequence in ~50–80 lines with simple comments — not a mini CLI framework.

---

## 2. Spike amendment — Minimal + real LLM host (2026-07-20)

**Decision:** Prefer a **minimal console + real LLM** over Spike_01’s full CLI.

| Keep | Drop |
|------|------|
| Hardcoded sample business need in `Main` | `--text`, `--input`, `--load`, `--help` |
| `appsettings.json` (+ Local for key) + OpenRouter | Interactive multiline requirement input |
| `OpenRouterProvider` (real LLM for demos) | Full `CliOptions` parser |
| Print artifact ids/paths after each step | Fancy multi-start path walker (keep a tiny resolve-root helper) |
| Optional **one** `Press Enter to continue…` pause after BA (inspect file on disk) | Dual y/N approve prompts + `--auto-approve` flag |
| Short training comments on non-obvious steps | Commenting every line / XML docs on trivial helpers |

**Still required (do not simplify away):**

- `StructuredRequirement` schema (BA must not echo `{ "text": "..." }`)
- BA + Developer: fence strip + schema validate + **1 retry**
- Developer loads BA output **only** via `IArtifactStore.LoadAsync(id)`
- Thesis test proving no BA raw completion leakage into Developer prompt

**Amends ROADMAP L4:** mid-chain gate = optional Enter pause (or auto-continue), not a CLI approve flag.

---

## 3. Scope boundaries

### In scope

| Area | Deliverable |
|------|-------------|
| Baseline | Spike_01 copy; Spike_01 folder remains frozen |
| Host cleanup | Slim `Program.cs`: hardcoded need → wire deps → run chain → print paths/telemetry |
| Training comments | Brief comments in console + agents/workflow on *why* (handoff-by-id, schema gate) |
| Contracts | **`StructuredRequirement`** payload + artifact type; keep `ImplementationArtifact` |
| BA Agent | `BusinessAnalystAgent` — prompt → LLM → validate → publish StructuredRequirement |
| Developer Agent | Loads StructuredRequirement **by id only**; prompt embeds that artifact JSON |
| Schemas | `schemas/structured-requirement.schema.json` (+ keep implementation schema) |
| Prompts | `prompts/ba-v1.txt`, update `prompts/developer-v1.txt` for structured input |
| Sequence | Inline in `Main` **or** thin `BusinessNeedToImplementationWorkflow` — prefer whichever stays easier to read |
| Validation | BA + Developer: JSON Schema + fence strip + **1 retry** each |
| Thesis test | Developer prompt source = `LoadAsync` only |
| Telemetry | Print BA + Developer tokens/duration/est. cost (persist `telemetry.json` if already easy) |

### Out of scope

| Area | Deferred to |
|------|-------------|
| Editing `src/Spike_01/` | Never for this spike |
| CLI flags / interactive input / `--load` mode | Not needed for this training spike |
| Promotion to `Athlon.*` | After Spike_02 (ROADMAP) |
| REST API / portal | PoC Sprint 1 |
| SQL, LangGraph, RAG, MCP | Later sprints |
| Agents beyond BA + Developer | Later |

---

## 4. Architecture (spike)

```text
┌─────────────────────────────────────────────────────────┐
│                  Athlon.Spike.Console                   │
│  hardcoded need · appsettings · OpenRouter · print ids/paths │
│  (training comments; no CLI flags)                      │
└─────────────────────────┬───────────────────────────────┘
                          │
          BA → [Enter pause?] → Developer (by artifact id)
                          │
┌─────────────────────────▼───────────────────────────────┐
│                   Athlon.Spike.Agents                    │
│  BusinessAnalystAgent · DeveloperAgent                   │
└───────┬─────────────────────────────┬───────────────────┘
        │                             │
┌───────▼──────────┐         ┌────────▼────────┐
│ Athlon.Spike.Llm │         │Athlon.Spike.     │
│ OpenRouterProvider│         │Artifacts         │
└──────────────────┘         │ FileArtifactStore │
                             └───────────────────┘
```

Optional thin workflow class is fine if it keeps `Main` readable; **do not** rebuild Spike_01 CLI ceremony.

**Handoff rule:** Developer `AgentExecutionContext.InputArtifactId` = BA output artifact id.  
Developer must never be passed BA `LlmCompletionResult.Content` directly.

### Target `Program.cs` shape (training sketch)

```csharp
// 1) Sample need (no CLI) — change this string to try other scenarios
var need = "As an employee I want meal allowance...";

// 2) Load appsettings.json → OpenRouter + file artifact store

// 3) Run BA → StructuredRequirement artifact on disk
//    print id + file path

// 4) Optional: Console.WriteLine("Press Enter to continue..."); Console.ReadLine();

// 5) Run Developer with BA artifact id only (LoadAsync inside the agent)
//    print Implementation id + path + telemetry
```

---

## 5. Project dependency graph

Same projects inside `src/Spike_02/` (names unchanged):

```text
Athlon.Spike.Contracts
        ↑
Athlon.Spike.Artifacts
Athlon.Spike.Llm
        ↑
Athlon.Spike.Agents
        ↑
Athlon.Spike.Workflow   (optional thin orchestrator — keep or inline)
        ↑
Athlon.Spike.Console    ← keep this tiny
```

---

## 6. Phased delivery

### Phase 0 — Verify baseline (copy)

**Goal:** Spike_02 solution is a clean, buildable fork of Spike_01.

| Task | Output |
|------|--------|
| Confirm `Spike_02.sln` builds; tests pass | Green build |
| Docs: README, AGENTS, this plan | Present |
| Spike_01 untouched | No Spike_02 feature work in `src/Spike_01/` |
| Config | `appsettings.json` + gitignored `appsettings.Local.json` |

**Exit criteria:** `dotnet build` + `dotnet test` succeed; docs describe Spike_02 thesis.

---

### Phase 0.5 — Simplify console host (Minimal + real LLM)

**Goal:** Strip Spike_01 CLI so the training spike shows the idea, not flag parsing.

| Task | Details |
|------|---------|
| Collapse `Program.cs` | Hardcoded sample need; `appsettings.json`; wire store + OpenRouter + current Developer path |
| Delete ceremony | Remove `CliOptions`, `--text`/`--input`/`--load`/`--auto-approve`/`--help`, interactive requirement reader |
| Paths | Assume cwd is `src/Spike_02` — use local `./prompts`, `./schemas`, `./artifacts`, `./appsettings*.json` (no path walker) |
| Training comments | Numbered steps in `Main` explaining the flow |
| Pause | Optional single Enter pause after agent output (preview of mid-chain inspect) — or auto-continue |
| Smoke | `dotnet run --project Athlon.Spike.Console` still produces an Implementation artifact with real LLM |

**Exit criteria:** `Program.cs` is short and readable; no CLI flags; real LLM demo still works; Spike_01 untouched.

---

### Phase 1 — StructuredRequirement contract & schema

**Goal:** Pin the BA output shape so BA cannot be a no-op text echo.

| Task | Details |
|------|---------|
| Artifact type | `StructuredRequirement` |
| Payload fields | `title`, `actors[]`, `goal`, `acceptanceCriteriaDraft[]`, `constraints[]`, `priority` |
| Schema file | `schemas/structured-requirement.schema.json` |
| Factory/parse helpers | Same pattern as `ImplementationArtifact` (+ brief training comment on why schema exists) |
| Serialization tests | Round-trip + invalid payload fails schema |

**Exit criteria:** Invalid payloads fail schema; valid payloads round-trip.

---

### Phase 2 — BusinessAnalystAgent

**Goal:** BA produces validated StructuredRequirement artifacts.

| Task | Details |
|------|---------|
| Prompt | `prompts/ba-v1.txt` — raw need in → JSON only out |
| Agent | `BusinessAnalystAgent` implementing `IAgent` |
| Validator | Reuse `JsonSchemaValidator` for structured-requirement schema |
| Retry | **Required:** 1 retry on schema failure (errors echoed) |
| Publish | Save via `IArtifactStore`; return `AgentExecutionResult` |
| Comments | Note: “publish only after schema OK — no chat handoff” |

**Exit criteria:** Mock LLM → publishes valid artifact; invalid-then-valid recovers; two failures → no publish.

---

### Phase 3 — Developer consumes StructuredRequirement by id

**Goal:** Close the chain without chat leakage.

| Task | Details |
|------|---------|
| Context | `InputArtifactId` points at BA StructuredRequirement artifact |
| Prompt | `developer-v1.txt` embeds **loaded artifact JSON**, expects Implementation schema output |
| Guard | Reject wrong input artifact type |
| No BA content injection | Caller passes id only |
| Comments | Note: “prompt built from LoadAsync(id), never from BA completion string” |

**Exit criteria:** Developer works with StructuredRequirement input; still 1 retry on Implementation schema failure.

---

### Phase 4 — Chain the sequence (BA → Developer)

**Goal:** Nothing important lives outside a clear BA → Developer sequence.

| Task | Details |
|------|---------|
| Orchestration | Prefer **inline steps in `Program.cs`** for training clarity; optional thin `BusinessNeedToImplementationWorkflow` if `Main` would otherwise get noisy |
| Steps | BA → optional Enter pause → Developer → print summary |
| Replace demo path | Stop using single-step `RequirementToImplementationWorkflow` as the demo entry |
| Telemetry | Print BA + Developer usage; persist `telemetry.json` if straightforward |

**Exit criteria:** End-to-end with mocks; after BA, Developer runs only with BA artifact id; pause path (if kept) does not run Developer until Enter.

---

### Phase 5 — Thesis test + final host polish

**Goal:** Falsify “chat handoff” vs “artifact handoff.”

| Task | Details |
|------|---------|
| Thesis test | Assert Developer composed user prompt contains StructuredRequirement payload from store; assert it does **not** contain a distinct BA raw completion marker used only in mock LLM response |
| Console | Confirm slim `Main` runs the full chain with real LLM; training comments accurate |
| No CLI creep | Do not reintroduce flags |

**Exit criteria:** Thesis test green; `dotnet run --project Athlon.Spike.Console` runs chained workflow.

---

### Phase 6 — Demo & success criteria

**Goal:** Repeatable ~5-minute demo.

```text
1. Copy appsettings.Local.json.example → appsettings.Local.json (set OpenRouter:ApiKey)
2. cd src/Spike_02
3. dotnet run --project Athlon.Spike.Console
4. Show artifacts/{workflowId}/ — StructuredRequirement + Implementation (+ telemetry if present)
5. Optionally use the Enter pause to open the BA JSON before Developer runs
```

**Exit criteria:** All README success criteria checked.

---

## 7. Key interfaces (reference)

Reuse Spike_01 contracts. Expected additions:

```csharp
// StructuredRequirement payload (minimum)
public sealed record StructuredRequirementPayload(
    string Title,
    IReadOnlyList<string> Actors,
    string Goal,
    IReadOnlyList<string> AcceptanceCriteriaDraft,
    IReadOnlyList<string> Constraints,
    string Priority);
```

`IAgent`, `IArtifactStore`, `ILLMProvider`, `AgentExecutionResult` remain the extension points.

---

## 8. StructuredRequirement schema (minimum)

```json
{
  "title": "string",
  "actors": ["string"],
  "goal": "string",
  "acceptanceCriteriaDraft": ["string"],
  "constraints": ["string"],
  "priority": "string"
}
```

Store canonical definition in `schemas/structured-requirement.schema.json`.  
BA must transform raw prose into this shape — echoing `{ "text": "..." }` is a failed spike.

---

## 9. Risks and mitigations

| Risk | Mitigation |
|------|------------|
| BA is a no-op text wrapper | Structured schema + tests; reject thin `{ text }` as BA output |
| Chat leakage into Developer | Thesis test; caller passes artifact id only |
| Host grows complex again | Phase 0.5 rule: no CLI flags; keep `Program.cs` short |
| Abstractions insufficient for 2 agents | Document as spike finding; prefer minimal interface extension |
| Drift from Spike_01 archive | Never edit `src/Spike_01/` |

---

## 10. Testing strategy (minimal)

| Layer | Approach |
|-------|----------|
| StructuredRequirement | Schema valid/invalid + serialization |
| BA Agent | Mock LLM publish / retry / fail |
| Developer + StructuredRequirement input | Mock publish |
| **Thesis** | Developer prompt from `LoadAsync` only |
| Sequence | Happy path with mocks (BA then Developer by id) |
| OpenRouter | Manual smoke via slim console |

---

## 11. Promotion path

Deferred until Spike_02 succeeds — see [ROADMAP.md](../../ROADMAP.md).  
Promote from **Spike_02** (not Spike_01) when ready, since Spike_02 supersedes the single-agent loop.

---

## 12. Checklist tracker

> **Single source of truth for phase progress.** Update only this table when a phase finishes.  
> Do not duplicate “current phase” in README, AGENTS, or ROADMAP.

| Phase | Status |
|-------|--------|
| 0 — Verify baseline | ✅ Complete |
| 0.5 — Simplify console host | ✅ Complete |
| 1 — StructuredRequirement | ✅ Complete |
| 2 — BusinessAnalystAgent | ⬜ Not started ← **next** |
| 3 — Developer by-id handoff | ⬜ Not started |
| 4 — Chain BA → Developer | ⬜ Not started |
| 5 — Thesis test + host polish | ⬜ Not started |
| 6 — Demo | ⬜ Not started |

---

## 13. References

| Document | Relevance |
|----------|-----------|
| [ROADMAP.md](../../ROADMAP.md) | Sequencing + locks L1–L6 (L4 amended for minimal host) |
| [../Spike_01/](../Spike_01/) | Frozen reference demo |
| VisionScope Ch. 7–8 | Workflow + artifacts |
| Appendix D §D.6 | Sprint 1 (after promotion) |
