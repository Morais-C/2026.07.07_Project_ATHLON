# console-v1 — Bounds checklist

Analyst + Planner + Coder rejection rules. Extracted from Spike_04 analyst prompt and pre-locked decisions (L6). Violation → **fail fast** without publishing downstream artifacts.

## In scope (ALL must be true)

| Rule | Detail |
|------|--------|
| Target runtime | Single existing **.NET 9** console application (checked-in fixture baseline) |
| App pattern | **Read** (0 or more `Console.ReadLine` inputs) → **process** → **print** to stdout |
| File cap | At most **3 source files** plus the **.csproj** after the change (≤ 4 paths touched) |
| Change kinds | `feature` or `bugfix` only (single ChangeRequest JSON shape) |
| Dependencies | No extra NuGet packages beyond fixture baseline |
| Forbidden | Web, GUI, database, network-as-a-feature, multi-project solutions beyond fixture |

## Mental demos that fit

Echo tweaks, calculator fixes, string converters, Hello World variants — minimal edits to an existing console.

## Analyst behavior

1. Evaluate bounds **first** before producing StructuredChange.
2. If **out of bounds**, return only: `{ "inBounds": false, "reason": "<short explanation>" }` — no StructuredChange published.
3. If **in bounds**, return StructuredChange JSON matching `schemas/structured-change.schema.json`.

## Planner / Coder bounds (same scope)

- Relative paths only — never absolute paths or `..`
- `intendedPaths` must exist in CodeContext or be new files under the same project
- PatchPackage: unified diffs only; operations `modify` \| `create` \| `delete`; `targetFramework` must be `net9.0`

## CodeContext caps (deterministic gate)

| Cap | Value |
|-----|-------|
| Max files loaded | 4 |
| Max total chars | 32,000 |
| Extensions | `.cs`, `.csproj` |

Over-cap fixture → CodeContextBuilder aborts before Planner runs.

## Out-of-scope examples

REST API, Blazor/WPF/WinForms, SQL persistence, calling external HTTP APIs as a feature, adding packages, rewriting into a multi-project solution.
