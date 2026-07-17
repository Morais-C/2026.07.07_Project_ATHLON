# Spike_02 — Implementation Plan

> **Agent Chain via Artifacts** — Raw need → BA Agent → StructuredRequirement → Developer Agent → Implementation  
> **Host:** Console · **Store:** File-based · **LLM:** OpenRouter via `ILLMProvider`  
> **Baseline:** Full copy of Spike_01 under `src/Spike_02/` (`Spike_02.sln`)  
> **Master plan:** [ROADMAP.md](../../ROADMAP.md)

---

## 1. Objective

Prove Athlon’s multi-agent thesis before promotion:

**Primary question:** Can agents chain through immutable artifacts (BA → Developer) with no shared chat thread?

**Secondary question:** Do Spike_01 abstractions (`IAgent`, `IArtifactStore`, `ILLMProvider`) support a second agent without breaking changes? (If they need changes, document that as a spike finding.)

---

## 2. Scope boundaries

### In scope

| Area | Deliverable |
|------|-------------|
| Baseline | Spike_01 copy; Spike_01 folder remains frozen |
| Contracts | `RawNeed` (or workflow input text), **`StructuredRequirement`** payload + artifact type, existing `ImplementationArtifact` |
| BA Agent | `BusinessAnalystAgent` — prompt → LLM → validate → publish StructuredRequirement |
| Developer Agent | Updated to **load StructuredRequirement by id only**; prompt embeds that artifact JSON |
| Schemas | `schemas/structured-requirement.schema.json` (+ keep implementation schema) |
| Prompts | `prompts/ba-v1.txt`, update `prompts/developer-v1.txt` for structured input |
| Workflow | `BusinessNeedToImplementationWorkflow`: BA → mid-gate → Developer → final-gate → complete |
| Validation | BA + Developer: JSON Schema + fence strip + **1 retry** each |
| Thesis test | Developer prompt source = `LoadAsync` only |
| Host | Console CLI (extend Spike_01 args; `--auto-approve` skips both gates) |
| Telemetry | Tokens/duration/cost — aggregate and/or per agent step |

### Out of scope

| Area | Deferred to |
|------|-------------|
| Editing `src/Spike_01/` | Never for this spike |
| Promotion to `Athlon.*` | After Spike_02 (ROADMAP) |
| REST API / portal | PoC Sprint 1 |
| SQL, LangGraph, RAG, MCP | Later sprints |
| Agents beyond BA + Developer | Later |

---

## 3. Architecture (spike)

```text
┌─────────────────────────────────────────────────────────┐
│                  Athlon.Spike.Console                   │
│  raw need · start workflow · print artifact ids         │
└─────────────────────────┬───────────────────────────────┘
                          │
┌─────────────────────────▼───────────────────────────────┐
│                  Athlon.Spike.Workflow                   │
│  BusinessNeedToImplementationWorkflow                    │
│    1. BA Agent → StructuredRequirement artifact          │
│    2. Mid-chain approve (stub)                           │
│    3. Developer Agent (input = BA artifact id)           │
│    4. Final approve (stub) → complete                    │
└─────────────────────────┬───────────────────────────────┘
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

**Handoff rule:** Developer `AgentExecutionContext.InputArtifactId` = BA output artifact id.  
Developer must never be passed BA `LlmCompletionResult.Content` directly.

---

## 4. Project dependency graph

Unchanged from Spike_01 (same projects inside `src/Spike_02/`):

```text
Athlon.Spike.Contracts
        ↑
Athlon.Spike.Artifacts
Athlon.Spike.Llm
        ↑
Athlon.Spike.Agents
        ↑
Athlon.Spike.Workflow
        ↑
Athlon.Spike.Console
```

---

## 5. Phased delivery

### Phase 0 — Verify baseline (copy)

**Goal:** Spike_02 solution is a clean, buildable fork of Spike_01.

| Task | Output |
|------|--------|
| Confirm `Spike_02.sln` builds; tests pass | Green build |
| Docs: README, AGENTS, this plan | Present |
| Spike_01 untouched | No Spike_02 feature work in `src/Spike_01/` |
| `.env` local only (from `.env.example`) | Gitignored |

**Exit criteria:** `dotnet build` + `dotnet test` succeed; docs describe Spike_02 thesis (not Spike_01 copy-paste leftovers).

---

### Phase 1 — StructuredRequirement contract & schema

**Goal:** Pin the BA output shape so BA cannot be a no-op text echo.

| Task | Details |
|------|---------|
| Artifact type | e.g. `StructuredRequirement` (keep Spike_01 `BusinessRequirement` only if useful for RawNeed — prefer clear names) |
| Payload fields | `title`, `actors[]`, `goal`, `acceptanceCriteriaDraft[]`, `constraints[]`, `priority` |
| Schema file | `schemas/structured-requirement.schema.json` |
| Factory/parse helpers | Same pattern as `ImplementationArtifact` |
| Serialization tests | Round-trip |

**Exit criteria:** Invalid payloads fail schema; valid payloads round-trip.

---

### Phase 2 — BusinessAnalystAgent

**Goal:** BA produces validated StructuredRequirement artifacts.

| Task | Details |
|------|---------|
| Prompt | `prompts/ba-v1.txt` — raw need in → JSON only out |
| Agent | `BusinessAnalystAgent` implementing `IAgent` |
| Validator | Reuse/extend `JsonSchemaValidator` for structured-requirement schema |
| Retry | **Required:** 1 retry on schema failure (errors echoed) |
| Publish | Save via `IArtifactStore`; return `AgentExecutionResult` |

**Exit criteria:** Mock LLM → publishes valid artifact; invalid-then-valid recovers; two failures → no publish.

---

### Phase 3 — Developer consumes StructuredRequirement by id

**Goal:** Close the chain without chat leakage.

| Task | Details |
|------|---------|
| Context | `InputArtifactId` points at BA StructuredRequirement artifact |
| Prompt | `developer-v1.txt` embeds **loaded artifact JSON**, expects Implementation schema output |
| Guard | Reject wrong input artifact type |
| No BA content injection | Workflow passes id only |

**Exit criteria:** Developer works with StructuredRequirement input; still 1 retry on Implementation schema failure.

---

### Phase 4 — BusinessNeedToImplementationWorkflow

**Goal:** Nothing runs outside the chained workflow.

| Task | Details |
|------|---------|
| New workflow | `BusinessNeedToImplementationWorkflow` |
| Steps | BA → mid-gate → Developer → final-gate → complete / fail |
| Status | Log transitions; keep `Started` / `AwaitingApproval` / `Completed` / `Failed` (may reuse or extend statuses for mid-gate) |
| Deprecate/replace | Stop using single-step `RequirementToImplementationWorkflow` as the demo path |
| Telemetry | Persist `telemetry.json`; print BA + Developer usage (aggregated and/or per step) |

**Exit criteria:** End-to-end with mocks; mid-gate rejection leaves BA artifact on disk and does not run Developer (or does not complete — document chosen behavior).

---

### Phase 5 — Thesis test + console wiring

**Goal:** Falsify “chat handoff” vs “artifact handoff.”

| Task | Details |
|------|---------|
| Thesis test | Assert Developer composed user prompt contains StructuredRequirement payload from store; assert it does **not** contain a distinct BA raw completion marker/string used only in mock LLM response |
| Console | Wire new workflow; `--auto-approve` skips mid + final gates; interactive prompts for both when not set |
| CLI | Keep `--text`, `--input`, `--load`, `--help` |

**Exit criteria:** Thesis test green; console runs chained workflow.

---

### Phase 6 — Demo & success criteria

**Goal:** Repeatable 5-minute demo.

```text
1. Set .env (OPENROUTER_API_KEY, OPENROUTER_MODEL)
2. dotnet run --project Athlon.Spike.Console -- --text "As an employee I want meal allowance..." --auto-approve
3. Show artifacts/{workflowId}/ — StructuredRequirement + Implementation + telemetry
4. Optionally re-run without --auto-approve and inspect BA file at mid-gate
5. --load <guid> for each artifact
```

**Exit criteria:** All README success criteria checked.

---

## 6. Key interfaces (reference)

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

## 7. StructuredRequirement schema (minimum)

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

## 8. Risks and mitigations

| Risk | Mitigation |
|------|------------|
| BA is a no-op text wrapper | Structured schema + tests; reject thin `{ text }` as BA output |
| Chat leakage into Developer | Thesis test; workflow passes artifact id only |
| Abstractions interfaces insufficient for 2 agents | Document required changes as spike finding; prefer minimal interface extension |
| Dual gates confuse demo | `--auto-approve` skips both; docs show interactive mid-gate path |
| Drift from Spike_01 archive | Never edit `src/Spike_01/` |

---

## 9. Testing strategy (minimal)

| Layer | Approach |
|-------|----------|
| StructuredRequirement | Schema valid/invalid + serialization |
| BA Agent | Mock LLM publish / retry / fail |
| Developer + StructuredRequirement input | Mock publish |
| **Thesis** | Developer prompt from `LoadAsync` only |
| Workflow | Mid-gate reject; full auto-approve happy path |
| OpenRouter | Manual smoke only |

---

## 10. Promotion path

Deferred until Spike_02 succeeds — see [ROADMAP.md](../../ROADMAP.md).  
Promote from **Spike_02** (not Spike_01) when ready, since Spike_02 supersedes the single-agent loop.

---

## 11. Checklist tracker

| Phase | Status |
|-------|--------|
| 0 — Verify baseline | ✅ Complete |
| 1 — StructuredRequirement | ⬜ Not started |
| 2 — BusinessAnalystAgent | ⬜ Not started |
| 3 — Developer by-id handoff | ⬜ Not started |
| 4 — Chained workflow | ⬜ Not started |
| 5 — Thesis test + console | ⬜ Not started |
| 6 — Demo | ⬜ Not started |

---

## 12. References

| Document | Relevance |
|----------|-----------|
| [ROADMAP.md](../../ROADMAP.md) | Sequencing + locks L1–L6 |
| [../Spike_01/](../Spike_01/) | Frozen reference demo |
| VisionScope Ch. 7–8 | Workflow + artifacts |
| Appendix D §D.6 | Sprint 1 (after promotion) |
