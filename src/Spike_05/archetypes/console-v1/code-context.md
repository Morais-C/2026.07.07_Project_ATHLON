# console-v1 — CodeContext rules

How the baseline is represented for Planner and Coder. Caps are declared in `archetype.json` → `codeContext` and enforced by **CodeContextBuilder** (deterministic).

## Baseline source

| Field | Value |
|-------|-------|
| Fixture id | `echo-v1` |
| Path | `../../fixtures/echo-v1` (relative to pack root) |
| Entry project | `Echo/Echo.csproj` |
| Target framework | `net9.0` |

## Loading rules

1. Copy **all** fixture files under the baseline path recursively.
2. Include only files with extensions `.cs` and `.csproj`.
3. Skip directories: `bin`, `obj`, `.git`.
4. For each file: store relative path, full UTF-8 content, and SHA-256 hash (`contentSha256`).
5. Compute `totalChars` as sum of file content lengths.

## Caps (fail fast)

| Rule | Value | On violation |
|------|-------|--------------|
| `maxFilesAllowed` | 4 | InvalidOperationException — no CodeContext artifact |
| `maxCharsAllowed` | 32,000 | InvalidOperationException — no CodeContext artifact |

Caps are echoed in the published CodeContext artifact so downstream agents and tests can verify limits.

## Immutability

CodeContext is a **snapshot** of the fixture at chain start. The Applier copies the fixture to `Publish/{workflowId}/` before applying patches — the checked-in baseline is never modified in place (branch metaphor).

## Schema

`schemas/code-context.schema.json`
