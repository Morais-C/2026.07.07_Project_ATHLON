# Project Athlon — Execution Roadmap

> **Living master plan** for build order (distinct from the VisionScope manuscript).  
> **Last updated:** 2026-07-17  
> **Manuscript:** [Project_ATHLON_VisionScope/INDEX.md](./Project_ATHLON_VisionScope/INDEX.md)

---

## Current status

| Milestone | Status |
|-----------|--------|
| **Spike_01 — Artifact Slice** | ✅ Complete (2026-07-15) |
| **Spike_02 — Agent chain via artifacts** | ⬜ Next |
| **Promotion to `Athlon.*`** | ⬜ After Spike_02 |
| **PoC Sprint 1 — API + Basic Portal** | ⬜ After promotion |

---

## Decision log

### 2026-07-17 — Spike_02 before promotion

**Decision:** Do **Spike_02 first**; **defer** promotion of Spike_01 code into `Athlon.*` and PoC Sprint 1 (API/portal) until Spike_02 succeeds.

**Primary concern:** *Can agents chain through artifacts?*  
Spike_01 proved one agent → one artifact. Athlon’s stronger thesis needs a second proof: **BA Agent → structured artifact → Developer Agent → implementation artifact**, with no shared chat thread.

**Rationale:**

- Multi-agent handoff is the largest remaining architectural uncertainty.
- Portal/API packaging can wait; polishing the wrong loop wastes effort.
- Spike_01 remains the working reference demo and archive until promotion.

**Order:**

```text
1. Spike_01 (done) — single Developer Agent loop
2. Spike_02 (next) — BA → Developer handoff via immutable artifacts
3. Promote proven spike code → Athlon.Contracts / Artifacts / Workflow / Agents / Llm
4. PoC Sprint 1 — Athlon.Api + Basic Portal + CI
```

**Out of scope until after Spike_02:** SQL artifact store, LangGraph, RAG/MCP, multiple extra agents beyond BA + Developer.

**VisionScope mapping:** Spike_02 de-risks the multi-step sequential workflow before Appendix D Sprint 1 packaging (portal). Playbook Sprint 1 deliverables stay valid; we insert an intentional spike between Spike_01 and promotion.

---

## Spike_01 (complete) — summary

- Path: Business requirement → workflow → Developer Agent → validated `ImplementationArtifact`
- Host: console · Store: file · LLM: OpenRouter via `ILLMProvider`
- Location: [`src/Spike_01/`](./src/Spike_01/)
- Docs: [README](./src/Spike_01/README.md) · [ImplementationPlan](./src/Spike_01/ImplementationPlan.md)

---

## Spike_02 (next) — intent

| Item | Detail |
|------|--------|
| **Thesis** | Agents collaborate by exchanging immutable artifacts, not conversation logs |
| **Minimal chain** | Raw need → **BA Agent** → BusinessRequirement artifact → **Developer Agent** → Implementation artifact |
| **Likely host** | Console (same discipline as Spike_01) until promotion |
| **Docs** | Start in [`src/Spike_02/`](./src/Spike_02/) — write README + ImplementationPlan in the next session |

Success means a demo where the Developer Agent’s only input is a BA-produced artifact id/file — not pasted chat.

---

## After Spike_02 — promotion & Sprint 1

When Spike_02 succeeds, promote in plan order (Appendix A §A.15):

```text
Athlon.Spike.*  →  Athlon.Contracts / Artifacts / Workflow / Agents / Llm
Then: Athlon.Api + Basic Portal (Appendix D §D.6)
```

Keep spike folders as archive or delete after promotion — team decision at that time.

---

## Fresh session starter

```text
Continue Project Athlon from ROADMAP.md.

Spike_01 is complete. Decision 2026-07-17: Spike_02 before promotion.
Primary question: Can agents chain through artifacts? (BA → Developer)

Read ROADMAP.md and src/Spike_01/README.md for context.
Create src/Spike_02/ README + ImplementationPlan (same style as Spike_01).
Do not promote to Athlon.* yet. No portal/API in Spike_02.
```
