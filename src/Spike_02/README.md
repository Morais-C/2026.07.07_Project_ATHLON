# Spike_02 — Agent Chain via Artifacts

> **Status:** Planned (not started)  
> **Depends on:** Spike_01 complete  
> **Decision:** See [ROADMAP.md](../../ROADMAP.md) — Spike_02 **before** promotion to `Athlon.*`

## Primary question

**Can agents chain through artifacts?**

```text
Raw business need
        ↓
   BA Agent  →  BusinessRequirement artifact (structured)
        ↓
   Developer Agent  →  ImplementationArtifact
        ↓
   File Artifact Store (immutable)
```

Developer must consume the BA output **by artifact id**, not by chat history.

## Next session

1. Write `ImplementationPlan.md` and `AGENTS.md` (same discipline as Spike_01).
2. Implement one phase at a time; stop for human verification between phases.
3. Do **not** promote Spike_01/02 to `Athlon.*` until this spike succeeds.
4. Out of scope: portal, API, SQL, RAG, MCP, LangGraph, agents beyond BA + Developer.

## Reference

- Working demo: [`../Spike_01/`](../Spike_01/)
- Master plan: [`../../ROADMAP.md`](../../ROADMAP.md)
