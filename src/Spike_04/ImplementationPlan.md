# Spike_04 — Implementation Plan

> **Change existing console from artifacts** — Fixture + ChangeRequest → Analyst → CodeContext → Planner → Coder → Applier → `Publish/{workflowId}/`  
> **Host:** Minimal console + real OpenRouter LLM; **fail fast** (no Enter pauses / no CLI flags)  
> **Baseline:** Full copy of Spike_03 under `src/Spike_04/` (`Spike_04.sln`) + checked-in fixture  
> **Master plan:** [ROADMAP.md](../../ROADMAP.md)  
> **Locks:** §2 (do not reopen mid-spike)

---

## 1. Objective

Prove Athlon’s next load-bearing question **before promotion**:

**Can agents change an existing console via artifacts (not chat), ending in a new buildable Publish tree?**

**Secondary:** One ChangeRequest shape covers **feature** and **bug fix**; unified-diff PatchPackage; fail fast; token caps on CodeContext.

**Training goal:** Slim `Program.cs` shows incremental change pipeline with clear step logs and immediate exit on failure.

---

## 2. Pre-locked decisions (2026-07-29)

| # | Decision |
|---|----------|
| L1 | **Sequence:** Spike_04 **before** promotion; Spike_01–03 stay frozen archives. |
| L2 | **Fork by copy:** `src/Spike_04/` starts as a full copy of Spike_03. Do not modify Spike_01–03 for Spike_04 features. |
| L3 | **Agent roster:** LLM **AnalystAgent** → **PlannerAgent** → **CoderAgent**; then deterministic **Applier** (not an LLM agent). No separate Locator LLM in this spike. |
| L4 | **Handoff:** each step receives prior **artifact id only**; never prior raw LLM completion text. |
| L5 | **Inputs:** checked-in **fixture** baseline + host **ChangeRequest** (`kind`: `feature` \| `bugfix`). |
| L6 | **Bounds:** same as Spike_03 — single net9.0 console; read→process→print; ≤3 source files + `.csproj` **after** change; no web/GUI/DB/network-as-feature/extra NuGet. |
| L7 | **Out of bounds:** Analyst **fails without publishing** StructuredChange (fail ASAP). Host exits (no Enter pause). |
| L8 | **CodeContext:** deterministic load of **all** fixture source files (+ entry metadata); publish as artifact; **hard caps** (max files / max chars); abort if over. |
| L9 | **PatchPackage:** **unified diffs** per touched path; operations `modify` \| `create` \| `delete`; relative paths only (reject `..`, absolute). |
| L10 | **Publish / branch metaphor:** copy fixture → apply patches under **new** `Publish/{workflowId}/` only; never overwrite fixture or an existing Publish folder. |
| L11 | **Success:** apply succeeds + `dotnet build` succeeds. **Functional run deferred** to future Tester (Applier stays deterministic). |
| L12 | **Host:** slim console; hardcoded ChangeRequest; `appsettings`; no CLI flags; **no Enter pauses**; **fail fast** on any step error. |
| L13 | **Naming:** ChangeRequest / StructuredChange / CodeContext / PatchPackage / **Applier** (not Publisher-for-greenfield). |
| L14 | **Risk accept:** unified diffs can be flaky; mitigate with schema + 1 retry + Applier apply/build failure = fail fast (no silent partial apply). |

---

## 3. Scope boundaries

### In scope

| Area | Deliverable |
|------|-------------|
| Baseline | Copy Spike_03 → Spike_04; add `fixtures/`; strip greenfield-only demo need |
| ChangeRequest | One schema for feature + bugfix |
| CodeContext | Deterministic from fixture; caps |
| Agents | Analyst / Planner / Coder adapted for change flow |
| Applier | Copy → apply unified diffs → `dotnet build` → manifest |
| Tests | Thesis (id-only); Applier E2E build; bounds abort |
| Demo | Hardcoded feature **or** bugfix → Publish folder builds |

### Out of scope

| Area | Deferred |
|------|----------|
| Editing Spike_01–03 | Never for this spike |
| Promotion / portal / API / git / MCP / RAG | After Spike_04 / later |
| Locator LLM / selective file retrieval | Later (shape reserved via CodeContext) |
| Full-file PatchPackage alternative | Not unless spike amendment |
| Enter pauses / CLI flags | Explicitly out |
| Tester / functional run asserts | Later |

---

## 4. Architecture (spike)

```text
fixtures/{name}/                    (immutable baseline on disk)
ChangeRequest (host)                (feature | bugfix)
        ↓
  AnalystAgent  →  StructuredChange   OR abort (no publish)
        ↓
  CodeContextBuilder (deterministic) → CodeContext artifact
        ↓
  PlannerAgent  →  ImplementationPlan (touches / tasks)
        ↓
  CoderAgent    →  PatchPackage (unified diffs)
        ↓
  Applier (deterministic)
        ↓
  Publish/{workflowId}/  + build proof
```

**Branch metaphor:** each successful run is a new tree under `Publish/{workflowId}/`, analogous to a future GitHub branch — baseline untouched.

---

## 5. Artifact types & suggested schemas

| Type | Producer | Notes |
|------|----------|--------|
| `ChangeRequest` | Host | Raw change intent (`feature` \| `bugfix`) |
| `StructuredChange` | Analyst | Only if in bounds |
| `CodeContext` | Deterministic builder | All fixture files (+ hashes); caps enforced |
| `ImplementationPlan` | Planner | May reuse Spike_03 plan shape; should list intended paths |
| `PatchPackage` | Coder | Unified diffs |
| Publish tree | Applier | `Publish/{workflowId}/` |
| Apply/Build manifest | Applier | `buildSucceeded`, capped logs, `functionalTest: deferred-to-tester-agent` |

### 5.1 ChangeRequest (one shape — feature + bugfix)

```csharp
public sealed record ChangeRequestPayload(
    string Kind,                    // "feature" | "bugfix"
    string Title,
    string Description,
    // bugfix-oriented (optional for feature; recommended for bugfix)
    string? StepsToReproduce,
    string? ExpectedBehavior,
    string? ActualBehavior,
    // optional targeting hint (Analyst/Planner may ignore if wrong)
    IReadOnlyList<string>? SuspectedPaths);
```

| Kind | Required emphasis |
|------|-------------------|
| `feature` | `title` + `description` (what to add/change) |
| `bugfix` | same + prefer `stepsToReproduce` / `expectedBehavior` / `actualBehavior` |

Same JSON schema for both; Analyst validates kind ∈ {feature, bugfix} and bounds.

### 5.2 CodeContext

```csharp
public sealed record CodeContextFile(string Path, string Content, string ContentSha256);

public sealed record CodeContextPayload(
    string FixtureId,                 // e.g. "echo-v1"
    string EntryProject,              // relative .csproj
    string TargetFramework,           // "net9.0"
    IReadOnlyList<CodeContextFile> Files,
    int TotalChars,
    int MaxFilesAllowed,
    int MaxCharsAllowed);
```

**Spike_04 caps (pin in schema / builder):** e.g. `MaxFilesAllowed = 4`, `MaxCharsAllowed = 32_000` (tune in Phase 1 if needed). Over cap → fail before Planner.

### 5.3 PatchPackage (unified diffs)

```csharp
public sealed record PatchFileChange(
    string Path,                      // relative; safe path rules
    string Operation,                 // "modify" | "create" | "delete"
    string UnifiedDiff);              // standard unified diff text

public sealed record PatchPackagePayload(
    string FixtureId,                 // must match CodeContext.FixtureId
    IReadOnlyList<PatchFileChange> Changes,
    string EntryProject,
    string TargetFramework,           // "net9.0"
    string Summary);
```

**Unified diff conventions (lock):**

| Operation | Diff expectation |
|-----------|------------------|
| `modify` | Normal unified diff against baseline file content |
| `create` | Diff vs empty file (`--- /dev/null` or `--- a/path` with zero old lines — pick one convention and document in schema/prompt) |
| `delete` | Diff that removes all lines (or empty `unifiedDiff` **only if** schema allows delete-without-body — prefer explicit full-delete diff) |

**Applier rules:**

1. Verify `FixtureId` matches the fixture being copied.
2. Optionally verify baseline file SHA matches CodeContext (if present) before apply — fail fast on mismatch.
3. Apply each change in order; on first apply failure → stop, do not leave a “success” manifest with `buildSucceeded: true`.
4. Reject path escape / absolute / `..`.
5. After all applies → `dotnet build` on `entryProject`.
6. Prefer: if apply fails mid-way, delete the new Publish directory **or** leave it with `buildSucceeded: false` and clear failure — pick one in Phase 3 and test it (recommend: **keep tree + failed manifest** for debugging; never mark success).

---

## 6. Phased delivery

### Phase 0 — Copy baseline + fixture

**Goal:** Spike_04 solution builds; fixture checked in; Spike_01–03 untouched.

| Task | Output |
|------|--------|
| Copy Spike_03 → Spike_04; `Spike_04.sln` | Builds + existing tests still meaningful or trimmed later |
| Add `fixtures/{name}/` tiny console (≤3 sources) | Builds standalone |
| Docs: README, AGENTS, this plan | Present |

**Exit:** `dotnet build` (+ decide whether to keep Spike_03 tests green until adapted).

---

### Phase 1 — ChangeRequest + Analyst + CodeContext

**Goal:** Feature/bugfix input; bounds; CodeContext artifact.

| Task | Details |
|------|---------|
| Schemas | `change-request`, `structured-change`, `code-context` |
| Analyst | Bounds + StructuredChange; abort without publish |
| CodeContextBuilder | Load all fixture files; hashes; enforce caps |

**Exit:** In-bounds CR → StructuredChange + CodeContext; over-cap / OOB → fail fast.

---

### Phase 2 — Planner + Coder → PatchPackage

**Goal:** Plan + unified-diff package by artifact id.

| Task | Details |
|------|---------|
| Planner | Loads StructuredChange + CodeContext ids (or a single composed input artifact — prefer **one input id** via a small handoff artifact if needed; else Planner loads CodeContext id and host passes change id — **pin in Phase 2**: Coder/Planner each take **one** primary input artifact id; compose a `ChangeBundle` if two ids are awkward) |
| Coder | PatchPackage schema + 1 retry; path rules |
| Prompt | Instruct unified diff format strictly |

**Exit:** Mock Coder publishes valid PatchPackage; invalid→retry; two failures→no publish.

**Handoff note:** Spike_03 agents take one `inputArtifactId`. Prefer introducing a thin **`ChangeBundle`** artifact `{ structuredChangeId, codeContextId }` for Planner, then Planner output id for Coder — keeps L4 clean. Pin in Phase 2 implementation.

---

### Phase 3 — Applier (deterministic)

**Goal:** Fixture + PatchPackage → new Publish tree + build.

| Task | Details |
|------|---------|
| Copy fixture → `Publish/{workflowId}/` | Fail if exists |
| Apply unified diffs | Fail fast on apply error |
| `dotnet build` | Record manifest |

**Exit:** Fixture PatchPackage (hand-written) applies and builds in tests.

---

### Phase 4 — Full chain + host UX

**Goal:** Slim Program wires chain; fail fast; progress logs.

| Task | Details |
|------|---------|
| Program.cs | Hardcoded ChangeRequest (feature **or** bugfix sample); no Enter pauses |
| Remove | Spike_03 greenfield-only ceremony that obscures change thesis |
| Errors | Any exception / OOB → non-zero exit immediately |

**Exit:** `dotnet run` produces Publish folder for fixture + sample change (with API key).

---

### Phase 5 — Thesis + E2E CI tests

**Goal:** Falsify chat handoff; prove Applier build.

| Task | Details |
|------|---------|
| Thesis | Planner/Coder prompts from LoadAsync only |
| E2E | Fixture + hand-written or mock PatchPackage → Applier → build |
| Bounds | OOB / over-cap abort without Publish success |

**Exit:** `dotnet test` green.

---

### Phase 6 — Demo & success criteria

**Goal:** Repeatable demo (~5–10 min).

```text
1. appsettings.Local.json with OpenRouter key
2. cd src/Spike_04
3. dotnet test
4. dotnet run --project Athlon.Spike.Console
5. Open fixtures/ vs Publish/{workflowId}/; show unified diffs applied + buildSucceeded
```

**Exit:** README success criteria all checked.

---

## 7. Testing strategy

| Layer | Approach |
|-------|----------|
| Analyst bounds / kind | feature + bugfix publish; OOB no StructuredChange |
| CodeContext caps | Over-cap fixture (test double) aborts |
| Planner / Coder | Mock LLM; schema retry; unified diff validation light checks |
| Thesis | Recording LLM; marker not in next prompt |
| Applier | Hand-written PatchPackage on fixture → build |
| OpenRouter | Manual smoke via slim console |

---

## 8. Risks

| Risk | Mitigation |
|------|------------|
| LLM emits bad unified diffs | Schema + 1 retry; Applier fail fast; prompt examples |
| Partial apply | Fail on first hunk error; manifest `buildSucceeded: false` |
| Token blow-up | Caps on CodeContext; fixture stays tiny |
| Scope creep (git/portal) | L1 / out-of-scope table |
| Two input ids awkward | ChangeBundle artifact (Phase 2) |

---

## 9. Checklist tracker

> **Single source of truth for Spike_04 phase progress.**

| Phase | Status |
|-------|--------|
| 0 — Copy baseline + fixture | ✅ Complete |
| 1 — ChangeRequest + Analyst + CodeContext | ✅ Complete |
| 2 — Planner + Coder → PatchPackage | ✅ Complete |
| 3 — Applier (apply + build) | ✅ Complete |
| 4 — Chain + host UX (fail fast) | ✅ Complete |
| 5 — Thesis + E2E tests | ⬜ Not started ← **next** |
| 6 — Demo | ⬜ Not started |

---

## 10. References

| Document | Relevance |
|----------|-----------|
| [ROADMAP.md](../../ROADMAP.md) | Spike_04 before promotion |
| [../Spike_03/](../Spike_03/) | Frozen reference (copy source) |
| [PromotionPlan.md](../../PromotionPlan.md) | After Spike_04 |
