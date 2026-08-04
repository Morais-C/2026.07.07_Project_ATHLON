# Spike_06 — `rest-api-v1` archetype pack (planned)

> **Status:** Next — Spike_05 complete (`console-v1` proved as pack).  
> **Master plan:** [ROADMAP.md](../../ROADMAP.md)  
> **Vision:** [REST API competitive positioning](../../Project_ATHLON_VisionScope/Project_Athlon_REST_API_Competitive_Positioning.md)  
> **Baseline:** Fork [Spike_05](../Spike_05/) → `src/Spike_06/` (engine + `ArchetypePackLoader`)

## Purpose

Add the first **commercial** Athlon Solution Archetype pack — **`rest-api-v1`** — reusing the Spike_05 `ArchetypePackLoader` and the same artifact chain pattern.

## Thesis (draft)

Load **`archetypes/rest-api-v1/`** → ChangeRequest on a REST API fixture → Analyst bounds enforce OpenAPI/API caps → patch apply → **build + contract-test proof**.

## Proof gates (target)

1. `dotnet build`
2. OpenAPI consistency / diff gate
3. Contract or API tests (Tester agent extension optional)

## Non-goals

- Client archetypes (web/mobile) — later wave
- Promotion (follows Spike_06)
- Portal / API

## Next step

Author **ImplementationPlan.md** (phased) and fork Spike_05 into a full `Spike_06.sln`. Fresh-session starter lives in [ROADMAP.md](../../ROADMAP.md).
