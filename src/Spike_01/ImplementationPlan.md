# Spike_01 — Implementation Plan

> **Artifact Slice** — Business Requirement → Developer Agent → Implementation Artifact  
> **Host:** Console application · **Store:** File-based · **LLM:** OpenRouter via `ILLMProvider`

---

## 1. Objective

Deliver the smallest working system that validates Project Athlon’s architectural thesis before investing in API, portal, or infrastructure.

**Primary question:** Can we run one agent inside one workflow and persist versioned engineering artifacts — not conversation logs?

**Secondary question:** Can we swap the LLM provider without touching agent or workflow code?

---

## 2. Scope boundaries

### In scope

| Area | Deliverable |
|------|-------------|
| Contracts | `Artifact`, `BusinessRequirement`, `ImplementationArtifact`, `WorkflowInstance`, `AgentExecutionContext` |
| Artifact store | `IArtifactStore`, `FileArtifactStore` |
| Workflow | Sequential runner; one workflow definition: `RequirementToImplementation` |
| Agent | `DeveloperAgent` — prompt compose → LLM → validate → publish |
| LLM | `ILLMProvider`, `OpenRouterProvider`, `LlmCompletionResult` (content + usage) |
| Telemetry | Run summary: tokens, duration, estimated cost — console print + optional `telemetry.json` per workflow |
| Validation | JSON syntax + JSON Schema for `ImplementationArtifact` |
| Host | `Athlon.Spike.Console` — stdin/args, structured logging |
| Human gate (stub) | `--auto-approve` flag or interactive `Approve? [y/N]` |

### Out of scope

| Area | Deferred to |
|------|-------------|
| REST API / portal | PoC Sprint 1 (Appendix D §D.6) |
| SQL / SQLite | PoC when search is needed (Appendix D §D.7) |
| LangGraph | Platform when branching/retries justify it (Ch. 4 mapping) |
| Memory / RAG | Sprint 3 (Appendix D §D.8) |
| MCP / tool execution | Ch. 10 / later sprints |
| CI, Docker, ADR repo | Sprint 0 formalization |
| Multiple agents | Sprint 2+ |

---

## 3. Architecture (spike)

```text
┌─────────────────────────────────────────────────────────┐
│                  Athlon.Spike.Console                   │
│  parse input · start workflow · print results           │
└─────────────────────────┬───────────────────────────────┘
                          │
┌─────────────────────────▼───────────────────────────────┐
│                  Athlon.Spike.Workflow                   │
│  WorkflowRunner · RequirementToImplementationWorkflow    │
└─────────────────────────┬───────────────────────────────┘
                          │
┌─────────────────────────▼───────────────────────────────┐
│                   Athlon.Spike.Agents                    │
│  DeveloperAgent (IAgent)                                 │
│    1. Load input artifact                              │
│    2. Compose prompt (prompts/developer-v1.txt)        │
│    3. ILLMProvider.CompleteAsync                       │
│    4. Validate JSON Schema                             │
│    5. IArtifactStore.SaveAsync                         │
└───────┬─────────────────────────────┬───────────────────┘
        │                             │
┌───────▼──────────┐         ┌────────▼────────┐
│ Athlon.Spike.Llm │         │Athlon.Spike.     │
│ OpenRouterProvider│         │Artifacts         │
└──────────────────┘         │ FileArtifactStore │
                             └────────┬──────────┘
                                      │
                             ┌────────▼────────┐
                             │  artifacts/     │
                             │  (JSON files)   │
                             └─────────────────┘
```

**Dependency rule:** `Contracts` has no dependencies. `Console` is the composition root only.

---

## 4. Project dependency graph

```text
Athlon.Spike.Contracts          (no refs)
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

### Phase 0 — Bootstrap (Day 1)

**Goal:** Empty solution compiles; docs and folders exist.

| Task | Output |
|------|--------|
| Create `Spike_01.sln` and projects per layout in README | 6 projects |
| Target `net10.0` (or `net9.0` if SDK unavailable — document in README) | All projects build |
| Add `.gitignore` entries: `artifacts/`, `bin/`, `obj/`, `.env` | No secrets/artifacts committed |
| Add `schemas/`, `prompts/`, `artifacts/.gitkeep` | Folder structure ready |

**Exit criteria:** `dotnet build` succeeds with no implementation logic.

---

### Phase 1 — Contracts & artifact model (Day 1–2)

**Goal:** Shared types and store interface defined.

| Task | Details |
|------|---------|
| `Artifact` base record | `Id`, `Type`, `Version`, `Producer`, `CreatedUtc`, `WorkflowInstanceId`, `PayloadJson` |
| `BusinessRequirement` | Typed wrapper or factory from raw text/JSON |
| `ImplementationArtifact` | Expected output shape (align with Ch. 8 example fields where practical) |
| `WorkflowInstance` | `Id`, `WorkflowName`, `Status`, `StartedUtc`, `CompletedUtc` |
| `IArtifactStore` | `SaveAsync`, `LoadAsync(Guid id)` — `SearchAsync` not required |
| `IAgent` | `Task<Artifact> ExecuteAsync(AgentExecutionContext context, CancellationToken ct)` |

**Exit criteria:** Unit tests for artifact serialization round-trip (optional but recommended).

---

### Phase 2 — File artifact store (Day 2)

**Goal:** Durable, immutable file persistence.

| Task | Details |
|------|---------|
| `FileArtifactStore` | Path: `{root}/{workflowInstanceId}/{artifactId}.json` |
| Immutability | `SaveAsync` throws if file already exists |
| `LoadAsync` | Scan workflow folders or maintain index file (simple: recursive scan by id) |
| Default root | `./artifacts` relative to Spike_01, overridable via `ATHLON_ARTIFACTS_PATH` |

**Exit criteria:** Save + load integration test with temp directory.

---

### Phase 3 — LLM provider (Day 2–3)

**Goal:** OpenRouter behind abstraction.

| Task | Details |
|------|---------|
| `ILLMProvider` | Returns `LlmCompletionResult` (not bare `string`) |
| `LlmCompletionResult` | `Content`, `PromptTokens`, `CompletionTokens`, `TotalTokens`, `Model`, `Duration` |
| `OpenRouterProvider` | POST `https://openrouter.ai/api/v1/chat/completions` |
| Parse `usage` | Map `prompt_tokens`, `completion_tokens` from response body |
| Cost | Prefer OpenRouter-reported cost when in response; else estimate from model id + token counts |
| Config | `OPENROUTER_API_KEY`, `OPENROUTER_MODEL` from environment |
| Headers | `Authorization`, `HTTP-Referer` / `X-Title` per OpenRouter docs |
| Error handling | Clear message on 401/429; no silent fallback |

**Exit criteria:** Smoke test logs token counts; `MockLLMProvider` returns fixed usage for tests.

---

### Phase 4 — Developer Agent (Day 3–4)

**Goal:** Agent produces validated implementation artifact.

| Task | Details |
|------|---------|
| Prompt template | `prompts/developer-v1.txt` — system + user placeholders |
| Prompt composition | Input artifact JSON embedded in user prompt |
| Output instruction | Require raw JSON only, matching schema |
| `JsonSchemaValidator` | Use `System.Text.Json` + `JsonSchema.Net` (or manual required-field check for spike) |
| `schemas/implementation-artifact.schema.json` | Define minimum fields (e.g. title, summary, tasks, acceptanceCriteria) |
| Retry policy | 1 retry on validation failure with “fix your JSON” hint (optional) |
| Telemetry handoff | Agent returns `LlmCompletionResult` usage via `AgentExecutionResult` for workflow summary |

**Exit criteria:** Given a fixed mock LLM response, agent publishes valid artifact. With real OpenRouter, end-to-end once.

---

### Phase 5 — Workflow engine (Day 4)

**Goal:** Nothing runs outside a workflow.

| Task | Details |
|------|---------|
| `IWorkflow` | `Task<WorkflowResult> RunAsync(WorkflowInput input, CancellationToken ct)` |
| `RequirementToImplementationWorkflow` | Steps: (1) save input artifact, (2) run DeveloperAgent, (3) stub approval gate, (4) complete |
| `WorkflowRunner` | Creates `WorkflowInstance`, tracks status |
| Events | Log step transitions to console (structured: step, duration, artifact id) |
| Run summary | Aggregate agent telemetry; print tokens, duration, est. cost at workflow end |
| Optional persist | `artifacts/{workflowId}/telemetry.json` — same folder as artifacts, gitignored |

**Exit criteria:** Workflow status transitions: `Started` → `AwaitingApproval` → `Completed` (or `Failed`).

---

### Phase 6 — Console host & demo (Day 5)

**Goal:** Repeatable 5-minute demo.

| Task | Details |
|------|---------|
| CLI args | `--input <file>`, `--text "..."`, `--load <guid>`, `--auto-approve` |
| Default interactive | Read multiline requirement until blank line |
| Output | Print workflow id, artifact ids, file path, **token usage, duration, est. cost** |
| Sample input | `examples/meal-allowance-requirement.txt` (optional) |

**Demo script:**

```text
1. Set OPENROUTER_API_KEY and OPENROUTER_MODEL
2. dotnet run --project Athlon.Spike.Console -- --text "As an employee I want meal allowance..."
3. Approve when prompted
4. Note output artifact GUID
5. dotnet run -- --load <guid>
6. Open artifacts/{workflowId}/{artifactId}.json in editor — show immutability
```

**Exit criteria:** All success criteria in README are met.

---

## 6. Key interfaces (reference)

```csharp
// Athlon.Spike.Contracts
public record LlmCompletionResult(
    string Content,
    string Model,
    int PromptTokens,
    int CompletionTokens,
    TimeSpan Duration,
    decimal? EstimatedCostUsd = null);

public interface IArtifactStore
{
    Task SaveAsync(Artifact artifact, CancellationToken ct = default);
    Task<Artifact?> LoadAsync(Guid id, CancellationToken ct = default);
}

public interface ILLMProvider
{
    Task<LlmCompletionResult> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken ct = default);
}

public interface IAgent
{
    string Name { get; }
    Task<Artifact> ExecuteAsync(AgentExecutionContext context, CancellationToken ct = default);
}
```

---

## 7. Implementation artifact schema (minimum)

Spike uses a deliberately small schema. Expand in PoC.

```json
{
  "title": "string",
  "summary": "string",
  "tasks": [
    {
      "id": "string",
      "description": "string",
      "estimate": "string"
    }
  ],
  "acceptanceCriteria": ["string"],
  "technicalNotes": "string"
}
```

Store canonical definition in `schemas/implementation-artifact.schema.json`.

---

## 8. Risks and mitigations

| Risk | Mitigation |
|------|------------|
| LLM returns markdown fences or prose | Prompt: “JSON only”; strip ```json blocks in validator |
| OpenRouter rate limits / cost | Use smaller model for dev; mock provider for CI tests |
| .NET 10 SDK not installed | Fall back to `net9.0`; note in README |
| Scope creep into “real platform” | Reject any task not on Phase 0–6 list without explicit spike amendment |
| File store doesn’t scale | Acceptable for spike; `IArtifactStore` enables SQL later |

---

## 9. Testing strategy (minimal)

| Layer | Approach |
|-------|----------|
| Artifact store | Integration test with temp folder |
| Validator | Unit tests with valid/invalid JSON fixtures |
| LLM | `MockLLMProvider` returning fixed JSON for agent/workflow tests |
| OpenRouter | Manual smoke only; no key in CI |
| End-to-end | One scripted demo run documented in README |

No requirement for full test project in Phase 0; add `Athlon.Spike.Tests` in Phase 2 if time allows.

---

## 10. Promotion path (post-spike)

When spike succeeds, migrate in this order (matches Appendix A §A.15):

```text
1. Athlon.Spike.Contracts     →  Athlon.Contracts
2. Athlon.Spike.Artifacts     →  Athlon.Artifacts (+ SqlArtifactStore later)
3. Athlon.Spike.Workflow      →  Athlon.Workflow
4. Athlon.Spike.Agents        →  Athlon.Agents
5. Athlon.Spike.Llm           →  Athlon.Agents or Athlon.SharedKernel
6. New: Athlon.Api + Basic Portal (PoC Sprint 1)
```

Keep spike folder as archive or delete after promotion — team decision.

---

## 11. Checklist tracker

| Phase | Status |
|-------|--------|
| 0 — Bootstrap | ⬜ Not started |
| 1 — Contracts | ⬜ Not started |
| 2 — File store | ⬜ Not started |
| 3 — LLM provider | ⬜ Not started |
| 4 — Developer Agent | ⬜ Not started |
| 5 — Workflow | ⬜ Not started |
| 6 — Console & demo | ⬜ Not started |

Update this table as phases complete.

---

## 12. References

| Document | Relevance |
|----------|-----------|
| [INDEX.md](../../Project_ATHLON_VisionScope/INDEX.md) | Manuscript index |
| Ch. 4 — Autonomous SDLC | Sprint 1 = Developer Agent first |
| Ch. 6 — Agent Runtime | Agent lifecycle, validator, publisher |
| Ch. 7 — Workflow Orchestrator | Sequential orchestration for spike |
| Ch. 8 — Artifact-Driven Engineering | Immutability, contracts between agents |
| Appendix A §A.15 | Development order |
| Appendix B §B.7 | `IArtifactStore` |
| Appendix D §D.6 | Sprint 1 goal (portal deferred) |
