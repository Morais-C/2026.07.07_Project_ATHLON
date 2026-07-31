# Athlon Solution Archetype — Formal Definition

> **Status:** Normative (vision / product).  
> **Terms:** **Athlon Solution Archetype** (formal) · **Athlon Archetype** (shorthand) · **Solution Archetype** (same).  
> **Related:** [Chapter 2 — Vision](Project_Athlon_Book_Chapter_02_Vision_of_Project_Athlon.md#solution-archetypes) · [Artifact-driven engineering (Ch. 8)](Project_Athlon_Book_Chapter_08_Artifact_Driven_Engineering.md) · [REST API positioning](Project_Athlon_REST_API_Competitive_Positioning.md)

---

## Role in the Athlon vision

Project Athlon rests on two load-bearing ideas:

| Pillar | Question it answers |
|--------|---------------------|
| **Artifact-driven engineering** | *How* do agents collaborate? → Immutable artifacts; handoff by id; audit trail. |
| **Athlon Solution Archetypes** | *What* software can the platform change reliably? → Bounded types with enforced scope and proof. |

**Athlon Archetypes are the product cornerstone:** they turn a general agent stack into a **governed change factory** that enterprises can buy, scope, and audit. Without archetypes, Athlon is “another agent pipeline.” With them, it is **repeatable change for defined software shapes**.

---

## Formal definition

An **Athlon Solution Archetype** is a **versioned, productized specification** that defines a class of software the Athlon platform is allowed to change autonomously, including:

1. **Identity** — stable id (e.g. `console-v1`, `rest-api-v1`), display name, and lifecycle status (`training` | `preview` | `ga` | `deprecated`).
2. **Bounds** — machine-enforceable rules evaluated at intake (Analyst) and during planning/coding; violation → **fail fast without publishing** downstream artifacts.
3. **Baseline model** — how existing code is represented (fixture, repo snapshot, CodeContext caps) and what must not be overwritten in place.
4. **Intake** — supported ChangeRequest kinds and required/optional fields for that archetype.
5. **Artifact chain** — ordered artifact types, schemas, producers (LLM vs deterministic), and handoff rule (**next step receives artifact id only**).
6. **Change mechanism** — how modifications are expressed (e.g. unified-diff PatchPackage) and applied (e.g. Applier → new Publish tree or PR).
7. **Proof gates** — deterministic verification steps that must succeed before success is recorded (build, contract tests, deploy smoke, etc.); LLM output alone is never sufficient.
8. **Archetype pack** — deliverable bundle: JSON schemas, prompts, Analyst bounds text, caps, example fixtures, and proof pipeline configuration.

An archetype is **not** a prompt, a template repo, or a marketing label. It is a **contract** between the organization, the platform, and the agents: *this shape of software, this chain, this proof, this audit trail.*

---

## What an archetype is / is not

| Is | Is not |
|----|--------|
| A **SKU** for governed autonomous change | “We can code anything” |
| **Machine-enforced** scope (Analyst abort) | Hope-based prompting |
| A **pack** (schemas + bounds + proof) | A one-off demo script |
| Bound to **customer-owned** code/runtime (for repo-native archetypes) | A proprietary low-code runtime (unless explicitly defined) |
| Composable (e.g. API hub + contract-bound clients) | A single monolithic “app generator” |

---

## Archetype pack (minimum contents)

Every Athlon Archetype MUST document or ship:

| # | Component | Purpose |
|---|-----------|---------|
| 1 | `archetypeId`, version, status | Identity and compatibility |
| 2 | Bounds checklist | Analyst + Planner rejection rules |
| 3 | CodeContext rules | What files load, max files/chars, hashes |
| 4 | ChangeRequest profile | Feature/bugfix fields emphasized |
| 5 | Artifact type list + schemas | Chain contract |
| 6 | Agent roster + prompt paths | Who produces what |
| 7 | Patch/apply rules | Diff format, path safety, apply failure behavior |
| 8 | Proof pipeline | Ordered gates + success manifest fields |
| 9 | Reference baseline | Checked-in fixture or sample repo |
| 10 | Demo ChangeRequests | At least one in-bounds feature and one bugfix |

Optional: composition links (e.g. `web-client-v1` **requires** `rest-api-v1` OpenAPI artifact).

---

## Composition

Archetypes MAY **compose** in layers:

```text
rest-api-v1          (contract owner — OpenAPI + server)
    ↑ consumed by
web-client-v1        (UI; only declared API operations)
mobile-client-v1
watch-client-v1      (subset of mobile/API surface)
```

Composition rules:

- A **client** archetype references a **contract artifact** from a hub archetype (e.g. OpenAPI id or pinned spec hash).
- Client bounds include **forbidden undeclared network** and **operation allowlists** derived from the contract.
- Each archetype still has its own pack, proof gates, and artifact chain; composition is a **dependency**, not a merged mega-archetype.

---

## Reference instances (execution)

| Archetype id | Status | Proof (current) | Location |
|--------------|--------|-----------------|----------|
| `console-v1` | **Pack content complete** (Spike_05 Phase 1 ✅) · behavior proved (Spike_03/04) | Greenfield publish ([Spike_03](../src/Spike_03/)) + incremental patch + `dotnet build` ([Spike_04](../src/Spike_04/)); formal **pack** → [Spike_05](../src/Spike_05/) |
| `rest-api-v1` | **Planned** (Spike_06) | OpenAPI + build + contract tests (target) | [Competitive brief](Project_Athlon_REST_API_Competitive_Positioning.md) |

**Distinction:** Spike_04 runs console change with prompts/schemas at spike root (hardcoded paths). Spike_05 delivers the **archetype pack** (`archetypes/console-v1/`) and loader — the productized form of this table’s 10 components.

Spike folders remain **frozen archives**; product archetypes live under `Athlon.*` after promotion (post Spike_06).

---

## Lifecycle

```text
Define pack → Prove on fixture (spike) → Second archetype pack (Spike_06) → Promote to Athlon.* → Preview SKU → GA → Deprecate
```

**Current execution:** Spike_05 = first formal pack (`console-v1`); Spike_06 = `rest-api-v1`; then promotion. See [ROADMAP.md](../ROADMAP.md).

New work that changes bounds, schemas, or proof gates for an archetype **bumps archetype version** (`rest-api-v2`), not silent prompt edits.

---

## Glossary (single terms)

| Term | Meaning |
|------|---------|
| **Athlon Archetype** | Shorthand for Athlon Solution Archetype |
| **Archetype pack** | Versioned bundle implementing one archetype |
| **Bounds** | Rules that trigger fail-fast abort when violated |
| **Proof gate** | Deterministic check; failure blocks success manifest |
| **Baseline** | Immutable-on-disk reference copied before apply (branch metaphor) |
| **Hub archetype** | Owns shared contract (e.g. REST API / OpenAPI) |
| **Client archetype** | Consumes hub contract under stricter UI/platform caps |

---

## One-sentence definition (elevator)

> An **Athlon Solution Archetype** is a versioned contract that defines a bounded class of software Athlon may change—specifying intake, artifact chain, apply rules, and deterministic proof—so every ChangeRequest produces an auditable, verified result instead of open-ended codegen.
