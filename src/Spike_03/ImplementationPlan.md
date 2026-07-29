# Spike_03 — Implementation Plan

> **Publishable console from artifacts** — BusinessRequirement → Analyst → Planner → Coder → Publisher → `Publish/{workflowId}/`  
> **Host:** Minimal console + real OpenRouter LLM (no CLI flags)  
> **Baseline:** Full copy of Spike_02 under `src/Spike_03/` (`Spike_03.sln`)  
> **Master plan:** [ROADMAP.md](../../ROADMAP.md)  
> **Locks:** §2 (do not reopen mid-spike)

---

## 1. Objective

Prove Athlon’s next load-bearing question **before promotion**:

**Can an artifact chain end in buildable, runnable code on disk — produced only from loaded artifacts, not chat handoff?**

**Secondary:** Fail ASAP when the raw need is not a bounded read→process→print .NET 9 console (save tokens/time).

**Training goal:** Slim `Program.cs` shows the specialized-agent pipeline with pauses and progress logs.

---

## 2. Pre-locked decisions (2026-07-27)

| # | Decision |
|---|----------|
| L1 | **Sequence:** Spike_03 **before** promotion; Spike_02 stays frozen archive. |
| L2 | **Fork by copy:** `src/Spike_03/` starts as a full copy of Spike_02. Do not modify `src/Spike_01/` or `src/Spike_02/` for Spike_03 features. |
| L3 | **Agent roster:** LLM **AnalystAgent** → **PlannerAgent** → **CoderAgent**; then deterministic **Publisher** step (not an LLM agent). |
| L4 | **Handoff:** each step receives prior **artifact id only**; never BA/Planner/Coder raw LLM completion text. |
| L5 | **Raw need type:** keep **BusinessRequirement** (host creates from hardcoded string). |
| L6 | **Bounds:** single net9.0 console; read (0+ `ReadLine`) → process → print; ≤3 source files + `.csproj`; no web/GUI/DB/network-as-feature/extra NuGet. Mental demos: Hello World, echo, calculator, converter. |
| L7 | **Out of bounds:** Analyst **fails workflow without publishing** StructuredRequirement (fail ASAP). Pause so the human can read the message, then exit. |
| L8 | **CodePackage** schema: `files[]` `{ path, content }`, `entryProject`, `targetFramework` (`net9.0`), `expectedOutputContains`. |
| L9 | **Publish:** `src/Spike_03/Publish/{workflowId}/` only; never overwrite; relative paths only (reject `..`, absolute). |
| L10 | **Success (amended 2026-07-28):** files written + `dotnet build` succeeds. **Functional run/output checks deferred** to a future Tester agent (keeps Publisher deterministic and Coder free of test data). |
| L11 | **Host:** slim console, hardcoded need, `appsettings`, no CLI flags; pause after Analyst/Planner/Coder; medium verbosity during LLM/build. |
| L12 | **Naming:** Planner (not Developer); ImplementationPlan (not Implementation-as-code); Coder; Publisher (deterministic). |

---

## 3. Scope boundaries

### In scope

| Area | Deliverable |
|------|-------------|
| Baseline | Copy Spike_02 → Spike_03; rename solution; delete unused Spike_02-only ceremony if it hurts the goal |
| Agents | Analyst (bounds + StructuredRequirement), Planner (ImplementationPlan), Coder (CodePackage) |
| Publisher | Deterministic: materialize files, `dotnet build`, write publish manifest (functional tests later) |
| Schemas | Keep structured-requirement; add implementation-plan + code-package (and rename as needed) |
| Tests | Unit/agent mocks; thesis (id-only handoff); E2E build/run against Publish folder |
| Demo | Hardcoded need → pauses → Publish folder with working console |

### Out of scope

| Area | Deferred |
|------|----------|
| Editing Spike_01 / Spike_02 | Never for this spike |
| Promotion to `Athlon.*` | After Spike_03 |
| Portal / API / SQL / LangGraph / RAG / MCP | Later |
| Multi-project apps, web, GUI, DB | Out of bounds (abort) |
| CLI flags | Not for this training spike |

---

## 4. Architecture (spike)

```text
BusinessRequirement (host)
        ↓
  AnalystAgent  →  StructuredRequirement   OR abort (no publish)
        ↓ [Enter]
  PlannerAgent  →  ImplementationPlan
        ↓ [Enter]
  CoderAgent    →  CodePackage
        ↓ [Enter]
  Publisher (deterministic)
        ↓
  Publish/{workflowId}/  + build/run proof
```

```text
Console (slim) → Agents (Analyst, Planner, Coder) → Llm / Artifacts
                      ↓
                 Publisher step → Publish/ + BuildResult
```

---

## 5. Artifact types

| Type | Producer | Notes |
|------|----------|--------|
| `BusinessRequirement` | Host | Raw need text (unchanged type name) |
| `StructuredRequirement` | Analyst | Only if in bounds |
| `ImplementationPlan` | Planner | Plan/tasks — not source files |
| `CodePackage` | Coder | Schema-validated file set |
| Publish tree | Publisher | On disk under `Publish/{workflowId}/` |
| Publish manifest / BuildResult | Publisher | Record paths + **build** outcome (`functionalTest: deferred-to-tester-agent`) |

### CodePackage payload (minimum)

```csharp
public sealed record CodePackageFile(string Path, string Content);

public sealed record CodePackagePayload(
    IReadOnlyList<CodePackageFile> Files,
    string EntryProject,
    string TargetFramework,           // "net9.0"
    string ExpectedOutputContains);
```

---

## 6. Phased delivery

### Phase 0 — Copy baseline

**Goal:** Spike_03 solution is a clean, buildable fork of Spike_02.

| Task | Output |
|------|--------|
| Copy Spike_02 → Spike_03; `Spike_03.sln` | Builds + existing tests green |
| Docs: README, AGENTS, this plan | Present |
| Spike_01 + Spike_02 untouched | Verified |

**Exit:** `dotnet build` + `dotnet test` succeed under `src/Spike_03`.

---

### Phase 1 — Rename & bounds (Analyst / Planner)

**Goal:** Specialized names + early abort.

| Task | Details |
|------|---------|
| Rename | Developer→Planner; Implementation artifact→ImplementationPlan (types/prompts/tests) |
| Analyst | Bounds gate; on fail: message → Enter pause → exit; **no** StructuredRequirement publish |
| StructuredRequirement | May add fields helpful for console bounds if needed (keep Spike_02 minimum unless required) |

**Exit:** In-bounds need reaches Planner; out-of-bounds aborts ASAP with pause.

---

### Phase 2 — CodePackage + CoderAgent

**Goal:** Planner output → validated CodePackage by id.

| Task | Details |
|------|---------|
| Schema | `schemas/code-package.schema.json` |
| CoderAgent | `IAgent`; LoadAsync(ImplementationPlan id); 1 schema retry; publish CodePackage |
| Path rules | Relative paths only in schema / validator |

**Exit:** Mock Coder publishes valid CodePackage; invalid→retry; two failures→no publish.

---

### Phase 3 — Publisher (deterministic)

**Goal:** CodePackage id → disk + build proof (no functional run).

| Task | Details |
|------|---------|
| Materialize | Write under `Publish/{workflowId}/`; fail if exists |
| Build | `dotnet build` on entry project |
| Run / assert | **Deferred** — future Tester agent (not Publisher; not Coder test data) |
| Record | Manifest next to publish tree (`buildSucceeded`, capped build log) |

**Exit:** Happy-path mock CodePackage materializes and builds in tests.

**Amendment (2026-07-28):** L10 narrowed to build-only so Read→Process→Print apps do not hang Publisher; functional checks stay out of Coder/Publisher.

---

### Phase 4 — Full chain + host UX

**Goal:** Slim Program wires Analyst→Planner→Coder→Publisher with pauses + progress logs.

| Task | Details |
|------|---------|
| Program.cs | Hardcoded need; verbose step logs; Enter after Analyst/Planner/Coder |
| Delete/simplify | Remove Spike_02-only dead ends that obscure Spike_03 thesis |
| Telemetry | Print per-LLM-agent usage; persist if straightforward |

**Exit:** `dotnet run` demo produces Publish folder for a calculator/echo/Hello-style need.

---

### Phase 5 — Thesis + E2E CI tests

**Goal:** Falsify chat handoff; prove Publisher **build** in test suite.

| Task | Details |
|------|---------|
| Thesis | Coder (and Planner) prompts from LoadAsync only — not prior raw completions |
| E2E | Fixture CodePackage → Publisher → **build** assert (run/functional later with Tester) |
| Bounds test | Out-of-bounds need aborts without CodePackage/Publish |

**Exit:** Tests green in CI-style `dotnet test`.

---

### Phase 6 — Demo & success criteria

**Goal:** Repeatable ~5–10 minute demo.

```text
1. appsettings.Local.json with OpenRouter key
2. cd src/Spike_03
3. dotnet test
4. dotnet run --project Athlon.Spike.Console
5. Pause after each agent; open artifacts; after Publisher open Publish/{workflowId}/
6. Show build/run success (and optionally an out-of-bounds abort run)
```

**Exit:** README success criteria all checked.

---

## 7. Testing strategy

| Layer | Approach |
|-------|----------|
| Analyst bounds | In-scope publishes; out-of-scope fails with no SR artifact |
| Planner / Coder | Mock LLM; schema retry |
| Thesis | Recording LLM; marker not in next prompt |
| Publisher | Deterministic CodePackage fixture → build |
| OpenRouter | Manual smoke via slim console |

---

## 8. Risks

| Risk | Mitigation |
|------|------------|
| LLM emits absolute paths / `..` | Schema + Publisher reject |
| Overwrite Publish | Fail if directory/file exists |
| Scope creep (web apps) | Analyst abort ASAP |
| Flaky E2E | Fixed expected substring; no network apps |
| Too many renames vs Spike_02 | Phase 1 dedicated; keep BusinessRequirement type name |

---

## 9. Checklist tracker

> **Single source of truth for Spike_03 phase progress.**

| Phase | Status |
|-------|--------|
| 0 — Copy baseline | ✅ Complete |
| 1 — Rename & Analyst bounds | ✅ Complete |
| 2 — CodePackage + Coder | ✅ Complete |
| 3 — Publisher (build) | ✅ Complete |
| 4 — Chain + host UX | ✅ Complete |
| 5 — Thesis + E2E tests | ⬜ Not started ← **next** |
| 6 — Demo | ⬜ Not started |

---

## 10. References

| Document | Relevance |
|----------|-----------|
| [ROADMAP.md](../../ROADMAP.md) | Spike_03 before promotion |
| [../Spike_02/](../Spike_02/) | Frozen reference (copy source) |
| [PromotionPlan.md](../../PromotionPlan.md) | After Spike_03 |
