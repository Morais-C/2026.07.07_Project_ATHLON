# Project Athlon — Building the Autonomous SDLC

## Index

A reference architecture and implementation playbook for Agentic Software Engineering — from vision, through architecture, to a sprint-by-sprint build guide.

**Execution status (build order):** see the living master plan → [ROADMAP.md](../ROADMAP.md)  
*(Spike_01 complete · next: Spike_02 agent chain · promotion deferred)*

---

## Table of Contents

### Part I — Foundations

| # | Chapter | File |
|---|---------|------|
| 1 | The New Era of Software Engineering | [link](Project_Athlon_Book_Chapter_01_The_New_Era_of_Software_Engineering.md) |
| 2 | Vision of Project Athlon | [link](Project_Athlon_Book_Chapter_02_Vision_of_Project_Athlon.md) |

### Part II — Principles & Vision

| # | Chapter | File |
|---|---------|------|
| 3 | Engineering Principles | [link](Project_Athlon_Book_Chapter_03_Engineering_Principles.md) |
| 4 | What Is an Autonomous SDLC? | [link](Project_Athlon_Book_Chapter_04_What_Is_an_Autonomous_SDLC.md) |
| 5 | A First Look at the Architecture *(preview)* | [link](Project_Athlon_Book_Chapter_05_A_First_Look_at_the_Architecture.md) |

### Part III — Architecture

| # | Chapter | File |
|---|---------|------|
| 6 | The Agent Runtime | [link](Project_Athlon_Book_Chapter_06_The_Agent_Runtime.md) |
| 7 | The Workflow Orchestrator | [link](Project_Athlon_Book_Chapter_07_The_Workflow_Orchestrator.md) |
| 8 | Artifact-Driven Engineering | [link](Project_Athlon_Book_Chapter_08_Artifact_Driven_Engineering.md) |

### Part IV — Knowledge & Execution

| # | Chapter | File |
|---|---------|------|
| 9 | Memory & Knowledge Architecture | [link](Project_Athlon_Book_Chapter_09_Memory_and_Knowledge_Architecture.md) |
| 10 | The MCP Integration Layer | [link](Project_Athlon_Book_Chapter_10_The_MCP_Integration_Layer.md) |

### Part V — Engineering Intelligence

| # | Chapter | File |
|---|---------|------|
| 11 | Engineering Agent Behavior | [link](Project_Athlon_Book_Chapter_11_Engineering_Agent_Behavior.md) |

### Part VI — The Autonomous Enterprise

| # | Chapter | File |
|---|---------|------|
| 12 | Project Athlon Reference Architecture *(synthesis)* | [link](Project_Athlon_Book_Chapter_12_Project_Athlon_Reference_Architecture.md) |

### Appendices

| # | Appendix | File |
|---|----------|------|
| A | Building Project Athlon | [link](Project_Athlon_Book_Chapter_Appendix_A_Building_Project_Athlon.md) |
| B | Reference Implementation Guide | [link](Project_Athlon_Book_Chapter_Appendix_B_Reference_Implementation_Guide.md) |
| C | Project Athlon Pattern Catalog | [link](Project_Athlon_Book_Chapter_Appendix_C_Project_Athlon_Pattern_Catalog.md) |
| D | The Project Athlon Playbook | [link](Project_Athlon_Book_Chapter_Appendix_D_The_Project_Athlon_Playbook.md) |

---

## Cleanup Tracker

Open items from the manuscript review, in suggested priority order.

- [x] **Colliding ADR IDs** — Resolved. Ch.3 and Ch.5 stated the same underlying decision ("start simple/sequential") under two independent `ADR-001` labels. Ch.3 now cross-references Ch.5's formal ledger instead of declaring its own ID; Ch.5's `ADR-001` notes that it was previewed informally in Ch.3. Verified the full ledger end to end: `ADR-001`–`ADR-005` (Ch.5) → `ADR-006`–`ADR-009` (Ch.6) → `ADR-010`–`ADR-014` (Ch.7) → `ADR-015`–`ADR-020` (Ch.8) → `ADR-021`–`ADR-026` (Ch.9) → `ADR-027`–`ADR-036` (Ch.10) → `ADR-037`–`ADR-042` (Ch.11) is now a single unbroken, uniquely-owned sequence with no gaps or duplicates.
- [x] **Duplicated "Learning Objectives" blocks** — Resolved. Ch.11 and Ch.12 both had two blocks; in each case they were merged into a single list at the very top of the chapter (12 items for Ch.11, 10 for Ch.12) and the mid-chapter repeat was deleted. Ch.12's now-orphaned `### Part 1 — The Platform Comes Together` sub-heading was also removed since the chapter is no longer split into two parts.
- [x] **Part headers out of sync with this index** — Resolved. All twelve chapter files now carry Part headings matching the Part I–VI structure in this index.
- [x] **Near-duplicate titles** — Resolved. Ch.5 renamed to "A First Look at the Architecture" to distinguish its preview role from Ch.12's synthesis chapter, "Project Athlon Reference Architecture".
- [x] **Filename cleanup** — Resolved. Ch.12 file renamed to `Project_Athlon_Book_Chapter_12_Project_Athlon_Reference_Architecture.md`; Ch.5 file renamed to `Project_Athlon_Book_Chapter_05_A_First_Look_at_the_Architecture.md`.
- [x] **Role naming drift** — Resolved. Standardized on **Business Analyst Agent** throughout; Ch.12's two "Product Analyst Agent" references (§12.11, §12.25) updated to match Ch.4–11 and Appendix A.
- [x] **Appendix C filename typo** — "Apendix" → "Appendix" has already been fixed.

---

## VisionScope v1 — Pending Issues

**Manuscript version:** Project VisionScope v1  
**Status:** Structural cleanup complete (see Cleanup Tracker above). **Content and structure** work remains open.

Full tracker: **[ToDo_Content_Structure.md](ToDo_Content_Structure.md)**

### What this covers

Editorial issues that are **not** formatting — duplicated prose, chapter seams, overlapping build guides, and cross-chapter consistency. Distinct from the resolved organizational fixes in the Cleanup Tracker.

### Open work (summary)

| Priority | Area | Examples |
|----------|------|----------|
| **1 — Deduplication** | Repeated content (partially done) | ~~Error recovery~~ ✓ · ~~Approval gates~~ ✓ · ~~Ch. 10 Observability / Best Practices headings~~ ✓ · SDLC pipeline diagrams · Ch. 10 Integration/ADR mid-chapter blocks · Appendix A/B overlap · Ch. 12 §12.22+ vs appendices |
| **2 — Structure** | Chapter assembly & placement | ~~Ch. 5 preview disclaimer~~ ✓ · **Ch. 12 §12.1 chapter map** · Ch. 11 / 12 two-in-one seams · Ch. 11 placement vs Ch. 6 · Ch. 9 duplicate Reference Architecture headings |
| **3 — Consistency** | Cross-manuscript style | Title punctuation (`---` vs `—`); H1 vs H2 section numbering; bullet spacing |
| **4 — Review** | Formatting | 16/17 files done · Ch. 11 not reviewed · content work in Priorities 1–3 |

### Suggested next pass

1. ~~**Pass 2A**~~ — **COMPLETE** (error recovery, approval gates, Ch. 10 headings, Ch. 5 disclaimer).
2. **Pass 2B** — Start with Ch. 12 §12.1 → pipeline diagrams → Ch. 10 ADR blocks → Appendix A/B + Ch. 12 §12.22+ consolidation → Ch. 11/12 seams.

Mark items resolved in `ToDo_Content_Structure.md` as they are completed.

**Pass 2A (quick wins):** **COMPLETE**. **Pass 2B** and Priorities 1–3 remain open — see [ToDo_Content_Structure.md](ToDo_Content_Structure.md) § *When resuming*.

---

## Reading Paths

- **New to the platform?** Read Part I → Part II in order for the vision and principles before the architecture gets detailed.
- **Evaluating the architecture?** Start at Chapter 12 (synthesis), then drop into Chapters 6–11 for the subsystem you care about.
- **Building it?** Appendix A (incremental principles) → Appendix B (implementation guide) → Appendix C (patterns) → Appendix D (sprint playbook).
