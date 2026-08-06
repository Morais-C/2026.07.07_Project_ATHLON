# rest-api-v1 — ChangeRequest profile

| Field | Notes |
|-------|-------|
| `kind` | `feature` \| `bugfix` |
| `title` / `description` | Resource-oriented language OK (e.g. “add product resource…”) |
| `suspectedPaths` | Optional hints — OpenAPI, `MiniErp/Program.cs`, contract tests |

## Demo direction (Phase 0)

Concrete demo ChangeRequests are authored in Phase 0 exit / early Phase 1 under `demos/change-requests.json` (e.g. add product, add customer). Host may hardcode the active demo until catalog wiring is complete.
