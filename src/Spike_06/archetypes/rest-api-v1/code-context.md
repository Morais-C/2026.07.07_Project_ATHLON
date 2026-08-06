# rest-api-v1 — CodeContext rules

| Rule | Detail |
|------|--------|
| Fixture id | `mini-erp-v1` |
| Path | `../../fixtures/mini-erp-v1` (relative to pack root) |
| Load | All files under fixture matching includeExtensions (skip `bin`/`obj`) |
| Caps | From `archetype.json` → `codeContext` |
| Hashes | CodeContext artifact records per-file content hash (unchanged Spike_05 semantics) |

Baseline includes API project, checked-in OpenAPI, and contract-test project.
