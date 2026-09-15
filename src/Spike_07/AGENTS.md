# Spike_07 — Agent Instructions

Instructions for Cursor (or any implementer) working on this spike.

## Start here

1. Read [ROADMAP.md](../../ROADMAP.md) and [Solution Archetype definition](../../Project_ATHLON_VisionScope/Project_Athlon_Solution_Archetype_Definition.md).
2. Read [README.md](./README.md) for thesis and success criteria.
3. Implement strictly from [ImplementationPlan.md](./ImplementationPlan.md), **one phase at a time**.
4. Update **all** spike docs + [ROADMAP.md](../../ROADMAP.md) when a phase completes.
5. **Do not modify `src/Spike_01/` … `src/Spike_06/`.** Those stay frozen archives.

**Current state:** ⬜ **Planning / docs only** — no `Spike_07.sln` yet. Next: **Phase 0** — fork Spike_06 → Spike_07.

## Scope rules

- **In scope:** Sequential two-hop ChangeRequests on `rest-api-v1` / `mini-erp-v1`; publish-path baseline for hop 2; same four proof gates per hop; demo sequence add product → add customer.
- **Out of scope:** New archetypes/fixtures, promotion, portal/API, auth, EF/SQL, 3+ hops, git PR output, baseline-by-artifact-id.
- **No scope creep:** Reuse Spike_06 pack and Applier gates; extend host/config and tests only.

## Architecture constraints

| Rule | Detail |
|------|--------|
| Naming | Keep `Athlon.Spike.*` inside `src/Spike_07/` |
| Spike_01–06 | Frozen — never edit for Spike_07 features |
| Archetype id | `rest-api-v1` (unchanged from Spike_06) |
| Fixture id | `mini-erp-v1` (pristine baseline for hop 1 only) |
| Hop 1 baseline | `fixtures/mini-erp-v1/` |
| Hop 2 baseline | `Publish/{workflowId}/` from hop 1 (`Athlon:BaselinePublishPath`) |
| Demo sequence | `add-product-resource` → `add-customer-resource` |
| Proof (each hop) | apply → build → OpenAPI consistency → contract tests |
| Publish | New immutable folder per hop; never overwrite |
| Handoff | Artifact id only within each hop; fail fast; no Enter pauses |

## Suggested opening prompt (Phase 0)

```text
Read ROADMAP.md and src/Spike_07/AGENTS.md.
Spike_01–06 are frozen. Start Spike_07 Phase 0 per src/Spike_07/ImplementationPlan.md.
Goal: fork Spike_06 → Spike_07 (copy); verify build + test green unchanged.
No promotion. No portal/API. One phase at a time; update docs on phase end.
```

## Definition of done

All README success criteria checked; two-hop chain test green; live demo product then customer with all proof gates OK on both hops; ROADMAP marks Spike_07 complete.
