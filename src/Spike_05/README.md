# Spike_05 — Athlon Archetype packs (`console-v1`)

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
- [ ] `archetypes/console-v1/` pack with all [10 pack components](../../Project_ATHLON_VisionScope/Project_Athlon_Solution_Archetype_Definition.md#archetype-pack-minimum-contents) *(Phase 0: skeleton only)*
- [ ] `ArchetypePack` loader resolves prompts, schemas, bounds, proof pipeline by `archetypeId`
- [ ] Host runs with `archetypeId = console-v1` only (config or manifest)
- [ ] Regression: Spike_04-equivalent E2E + thesis tests green via pack
- [ ] Demo ChangeRequests live in pack (`demos/`), not only in `Program.cs`

## Archetype pack layout (Phase 0)

Fork includes `archetypes/console-v1/` with draft manifest and placeholder components. Runtime still uses spike-root `prompts/` and `schemas/` until Phase 3.

```text
src/Spike_05/
  Spike_05.sln
  fixtures/echo-v1/              # baseline (component 9)
  prompts/ schemas/              # Spike_04 copy — runtime until Phase 3
  archetypes/console-v1/
    archetype.json               # component 1 — identity, paths, caps
    bounds.md                    # component 2 (placeholder)
    code-context.md              # component 3 (placeholder)
    change-request.md            # component 4 (placeholder)
    schemas/ prompts/            # components 5–6 (empty — Phase 1)
    patch-apply.md               # component 7 (placeholder)
    proof/pipeline.json          # component 8 (placeholder)
    demos/change-requests.json   # component 10 (draft demos)
    README.md
```

See [archetypes/console-v1/README.md](./archetypes/console-v1/README.md) for component checklist.

## After Spike_05

**Spike_06** — `rest-api-v1` archetype pack (OpenAPI + contract-test proof).  
Then **promotion** — lift engine + pack model → `Athlon.*`.
