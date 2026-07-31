# console-v1 — ChangeRequest profile (Phase 0 placeholder)

Supported kinds: **`feature`** | **`bugfix`** (single JSON shape).

## Fields

| Field | feature | bugfix |
|-------|---------|--------|
| `kind` | required | required |
| `title` | required | required |
| `description` | required | required |
| `suspectedPaths` | optional | optional |
| `stepsToReproduce` | — | optional |
| `expectedBehavior` | — | optional |
| `actualBehavior` | — | optional |

Schema: `schemas/change-request.schema.json` (Phase 1).
