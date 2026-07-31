# console-v1 — ChangeRequest profile

Supported intake kinds for this archetype. Single JSON shape for both kinds.

## Kinds

| Kind | Use |
|------|-----|
| `feature` | Add or change behavior in the existing console |
| `bugfix` | Correct incorrect behavior with reproduction context |

## Required fields

| Field | feature | bugfix |
|-------|---------|--------|
| `kind` | ✓ | ✓ |
| `title` | ✓ | ✓ |
| `description` | ✓ | ✓ |

## Optional fields

| Field | feature | bugfix | Notes |
|-------|---------|--------|-------|
| `suspectedPaths` | optional | optional | Relative paths under fixture root; Analyst may refine |
| `stepsToReproduce` | — | optional | Recommended for bugfix |
| `expectedBehavior` | — | optional | Recommended for bugfix |
| `actualBehavior` | — | optional | Recommended for bugfix |

## Schema

`schemas/change-request.schema.json`

## Demo samples

See `demos/change-requests.json` for in-bounds **uppercase-echo** (feature) and **trim-echo** (bugfix) examples used in Spike_04/05 demos.

## Chain entry

Host persists ChangeRequest as the workflow input artifact. Analyst loads it by id — never from pasted chat text.
