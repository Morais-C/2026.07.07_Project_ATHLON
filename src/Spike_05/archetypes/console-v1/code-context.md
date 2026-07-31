# console-v1 — CodeContext rules (Phase 0 placeholder)

Machine caps are declared in `archetype.json` → `codeContext`:

| Rule | Value |
|------|-------|
| Max files | 4 |
| Max chars | 32,000 |
| Extensions | `.cs`, `.csproj` |

Builder loads **all** fixture files under the baseline path; abort if over cap before Planner runs.
