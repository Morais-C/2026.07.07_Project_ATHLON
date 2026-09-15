# Spike_07 — Sequential ChangeRequests on `rest-api-v1`

> **Status:** ⬜ Planned — docs only; implementation not started  
> **Depends on:** Spike_06 complete (frozen at [`../Spike_06/`](../Spike_06/))  
> **Decision:** [ROADMAP.md](../../ROADMAP.md) — sequential CR / evolving Publish baseline **before** promotion  
> **Vision:** [REST API competitive positioning](../../Project_ATHLON_VisionScope/Project_Athlon_REST_API_Competitive_Positioning.md) · [Solution Archetype definition](../../Project_ATHLON_VisionScope/Project_Athlon_Solution_Archetype_Definition.md)  
> **Phase progress:** [ImplementationPlan.md §9 Checklist](./ImplementationPlan.md#9-checklist-tracker)

## Purpose

Spike_06 proved a **single-hop** ChangeRequest on the pristine `mini-erp-v1` fixture with full proof gates. Spike_07 answers:

**Can two ChangeRequests run in sequence on `rest-api-v1` / `mini-erp-v1`, where CR₂’s baseline is the Publish tree from CR₁ (not the pristine fixture), with the same four proof gates on each hop?**

## Thesis (one line)

CR₁ from **`fixtures/mini-erp-v1/`** → Publish₁ → CR₂ from **`Publish/{workflowId}/`** (Publish₁) → Publish₂ — each hop: **apply + build + OpenAPI consistency + contract tests**.

## Non-goals

- New archetype packs or fixtures (reuse Spike_06 `rest-api-v1` / `mini-erp-v1`)
- Promotion to `Athlon.*` (after Spike_07)
- Portal / API / git / auth / EF/SQL
- Three-or-more-hop chains (two hops only in this spike)
- Merging or rebasing Publish trees (each hop writes a new immutable folder)

## Success criteria

- [ ] Spike_07 forked from Spike_06; Spike_01–06 untouched
- [ ] Host/config supports a **locked demo sequence** (add product → add customer)
- [ ] CR₁ baseline = pristine fixture; CR₂ baseline = CR₁ Publish path (not fixture)
- [ ] Both hops pass all four proof gates (apply → build → OpenAPI → contract tests)
- [ ] Automated two-hop chain test (handwritten or fixture-based) green in CI
- [ ] Live demo: product resource then customer resource on the evolving tree
- [ ] ROADMAP marks Spike_07 complete; promotion unblocked

## How to run

> **Placeholder** — commands will be added after **Phase 0** (fork Spike_06 → Spike_07).

From `src/Spike_07/` (after fork):

```powershell
# TBD — mirror Spike_06 run path with sequential demo chain config
dotnet build Spike_07.sln
dotnet test Spike_07.sln
```

Config (planned): `Athlon:ArchetypeId` = `rest-api-v1`; `Athlon:DemoSequence` = ordered demo ids; `Athlon:BaselineSource` = `fixture` | `publish` with `Athlon:BaselinePublishPath` when chaining.

## Layout

> **Planned** — same shape as Spike_06 after Phase 0 fork.

```text
src/Spike_07/
  Spike_07.sln
  fixtures/mini-erp-v1/          # pristine baseline for hop 1 only
  archetypes/rest-api-v1/        # reuse Spike_06 pack (copied at fork)
  Publish/{workflowId}/          # hop outputs; hop 2 reads hop 1 path as baseline
```

## After Spike_07

**Promotion** — lift engine + pack model → `Athlon.*`. Then PoC Sprint 1 (portal/API).
