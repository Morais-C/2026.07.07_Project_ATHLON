# Spike_05 — Athlon Archetype packs (`console-v1`)

> **Status:** ✅ Complete (2026-08-04) — **frozen archive**  
> **Depends on:** Spike_04 complete (frozen at [`../Spike_04/`](../Spike_04/))  
> **Decision:** [ROADMAP.md](../../ROADMAP.md) — archetype packs **before** `rest-api-v1` and promotion  
> **Vision:** [Solution Archetype definition](../../Project_ATHLON_VisionScope/Project_Athlon_Solution_Archetype_Definition.md)  
> **Phase progress:** [ImplementationPlan.md §9 Checklist](./ImplementationPlan.md#9-checklist-tracker)

## Purpose

Spike_04 proved the **change engine** on a console baseline, but prompts, schemas, bounds, and fixtures are **spike-wide and hardcoded**. Spike_05 answers:

**Can the same proven pipeline run entirely from a formal `console-v1` archetype pack loaded by id?**

That productizes the **Athlon Solution Archetype** model before adding `rest-api-v1` (Spike_06).

## Thesis (one line)

Load **`archetypes/console-v1/`** → same ChangeRequest chain → same `echo-v1` baseline → same Publish + build proof as Spike_04, with **no** spike-root hardcoded prompt paths.

## Non-goals

- `rest-api-v1` (Spike_06)
- Promotion to `Athlon.*` (after Spike_06)
- Portal / API / git / Tester functional run

## Success criteria

- [x] Spike_05 forked from Spike_04; Spike_01–04 untouched
- [x] `archetypes/console-v1/` pack with all [10 pack components](../../Project_ATHLON_VisionScope/Project_Athlon_Solution_Archetype_Definition.md#archetype-pack-minimum-contents)
- [x] `ArchetypePack` loader resolves prompts, schemas, bounds, proof pipeline by `archetypeId`
- [x] Host runs with `archetypeId = console-v1` only (config or manifest)
- [x] Regression: Spike_04-equivalent E2E + thesis tests green via pack
- [x] Demo ChangeRequests live in pack (`demos/`), not only in `Program.cs`
- [x] Thesis: LoadAsync-only handoff + agent prompt/schema paths from pack manifest
- [x] Live demo via pack: apply OK + `dotnet build` OK under `Publish/{workflowId}/`

## How to run

From `src/Spike_05/` (so `./artifacts`, `./Publish`, `./appsettings*.json`, and `./archetypes/` resolve):

```powershell
# Tests (no LLM)
dotnet test Spike_05.sln

# Live demo (OpenRouter) — default ChangeRequest: uppercase-echo feature
# Copy appsettings.Local.json.example → appsettings.Local.json and set ApiKey
dotnet run --project Athlon.Spike.Console --no-launch-profile
```

Config: `Athlon:ArchetypeId` = `console-v1` in `appsettings.json`. Pack catalog: `archetypes/console-v1/demos/change-requests.json` (host currently hardcodes the active demo matching `uppercase-echo`).

## Live demo (2026-08-04)

| Item | Result |
|------|--------|
| Archetype | `console-v1` v1.0.0 via `ArchetypePackLoader` |
| Demo | feature — Uppercase echo |
| Chain | ChangeRequest → Analyst → CodeContext → Planner → Coder → Applier |
| Apply | OK |
| Build | OK (`Echo/Echo.csproj`) |
| Publish | `Publish/{workflowId}/` with `ToUpperInvariant()` on echo |
| Tests | `dotnet test` — 79 passed |

## Archetype pack layout

```text
src/Spike_05/
  Spike_05.sln
  fixtures/echo-v1/              # baseline (component 9)
  archetypes/console-v1/
    archetype.json               # component 1 — identity, paths, caps
    bounds.md                    # component 2
    code-context.md              # component 3
    change-request.md            # component 4
    schemas/ prompts/            # components 5–6 (sole runtime source)
    patch-apply.md               # component 7
    proof/pipeline.json          # component 8
    demos/change-requests.json   # component 10
    README.md
```

See [archetypes/console-v1/README.md](./archetypes/console-v1/README.md) for component checklist.

## After Spike_05

**Spike_06** — `rest-api-v1` archetype pack (OpenAPI + contract-test proof), same loader.  
Then **promotion** — lift engine + pack model → `Athlon.*`.
