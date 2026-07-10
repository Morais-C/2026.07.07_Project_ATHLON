# Project Athlon — Content & Structure TODO

Open editorial issues flagged during the manuscript review (formatting pass Ch. 1–10 + earlier structural reviews). These are **content/structure** items — not presentation/formatting.

**Scope:** `Project_ATHLON_VisionScope/`  
**Last updated:** 2026-07-10  
**Formatting review progress:** **COMPLETE (2026-07-10)** — INDEX + Ch. 1–10, Ch. 12, Appendices A–D · Ch. 11 skipped (frozen)

---

## How to use this file

- [ ] = open · [x] = resolved
- **Canonical home** = where the definitive version should live; other chapters should cross-reference
- Prefer **Pass 2A** (dedup + headings) before **Pass 2B** (chapter splits / moves)

---

## Priority 1 — Content duplication (high ROI)

### Error-recovery escalation ladder

Near-verbatim duplicate in two chapters.

| Location | Section |
|----------|---------|
| Ch. 6 | §6.9 Error Recovery |
| Ch. 7 | §7.8 Error Recovery |

- [ ] **Canonicalize in Ch. 6** (agent-runtime perspective)
- [ ] **Ch. 7:** replace body with short orchestrator-specific notes + cross-ref to Ch. 6 §6.9

---

### Human-approval gates

Same concept restated in four places with different wording.

| Location | Section |
|----------|---------|
| Ch. 3 | Principle 1 — Human-in-the-Loop |
| Ch. 4 | Human Approval Gates *(most concrete numbered list)* |
| Ch. 6 | §6.10 Human Approval |
| Ch. 7 | §7.7 Human Approval |

- [ ] **Canonicalize in Ch. 4** (numbered gate list)
- [ ] Ch. 3 / 6 / 7: shorten to principle-level mention + “see Ch. 4”

---

### SDLC pipeline diagrams

Similar `Business Request → … → Agent` flow diagrams recur across multiple chapters (at least Ch. 4, 6, 7, 8, 12).

- [ ] Audit all pipeline / agent-sequence ` ```text ` diagrams
- [ ] Keep **one canonical diagram** (suggest Ch. 4 or Ch. 12 synthesis)
- [ ] Elsewhere: shorter variants or cross-references only

---

### Chapter 10 — duplicate section headings

`Project_Athlon_Book_Chapter_10_The_MCP_Integration_Layer.md`

| Issue | Locations | Notes |
|-------|-----------|-------|
| Two **Observability** sections | §10.23 (~line 1258) · §10.29 (~line 1703) | Different scope (MCP-server telemetry vs platform observability) but duplicate H1 titles confuse readers/search |
| Two **Best Practices** sections | ~line 1406 · ~line 1905 | Mid-chapter block appears **before** §10.25 resumes |
| Two **Common Anti-Patterns** sections | ~line 1418 · ~line 1920 | Same ordering problem |

- [ ] Rename §10.23 → e.g. **“MCP Server Observability”** (or merge under one Observability parent with subsections)
- [ ] Rename §10.29 → e.g. **“Platform Observability”**
- [ ] Rename first anti-patterns block → e.g. **“MCP Anti-Patterns”**; keep end block as **“Architectural Anti-Patterns”** — or merge into one section
- [ ] **Reorder:** move mid-chapter Best Practices / Anti-Patterns to after §10.28 (or delete if redundant with end-of-chapter versions)

---

### Appendix A / B overlap

Two implementation guides answer many of the same questions.

| Topic | Appendix A | Appendix B |
|-------|------------|------------|
| Development order | §A.15 Suggested Development Order | §B.4 Recommended Development Order |
| Testing | §A.17 Testing Strategy | §B.17 Testing Strategy |
| Observability | §A.18 Observability | §B.16 Observability |

- [ ] Define boundary: **A = incremental principles / what order** · **B = layer-by-layer how to build**
- [ ] Deduplicate overlapping sections; add explicit cross-references
- [ ] `INDEX.md` Reading Paths already hints at this split — align appendix bodies with it

---

### Chapter 12 + Appendices — triple “how to build” pass

Third overlapping implementation narrative alongside Appendix A and B.

| Location | Section |
|----------|---------|
| Ch. 12 | §12.22–§12.33 *From Reference Architecture to Reference Implementation* (~line 932+) |
| Appendix A | Repository structure, catalogs, development order, standards |
| Appendix B | Solution layout, component specs, CI/CD, deployment |

- [ ] **Ch. 12** should end at synthesis (through ~§12.21); move §12.22+ into Appendix B **or** reduce to a short pointer
- [ ] Ensure only **one** authoritative development-order section remains

---

## Priority 2 — Chapter structure & placement

### Chapter 5 — preview not signaled in body

Index marks Ch. 5 as *(preview)*; chapter body still reads like a standalone architecture chapter (ADR-001–005).

- [ ] Add opening disclaimer: maps the territory; Ch. 6–11 go deep; Ch. 12 synthesizes
- [ ] Optional: trim ADR detail here since Ch. 5 ADRs are formally introduced in the ledger starting Ch. 5 / continued in later chapters

---

### Chapter 11 — two chapters in one file

Duplicate Learning Objectives were merged, but the **conceptual seam** remains.

| Part | Span | Pivot |
|------|------|-------|
| A — Reasoning engineering (conceptual) | §11.1 – §11.26 | |
| B — .NET / LangGraph implementation | §11.27+ | §11.27 *From Language Models to Reasoning Engines* (~line 1484) |

- [ ] **Do not edit** until user lifts Ch. 11 freeze — then choose:
  - **Option A:** Split into two chapters (renumber Part V/VI)
  - **Option B:** Keep one chapter; add hard `---` transition + “Part B assumes Ch. 6–10”
- [ ] Cross-ref Ch. 6 §6.5 Prompt Builder to reduce overlap with Ch. 11 reasoning content

**Status:** Ch. 11 explicitly **frozen** — no formatting or content edits during current pass.

---

### Chapter 11 — placement vs Chapter 6

Intra-agent reasoning (Ch. 11) sits **after** inter-agent topics (orchestration, artifacts, memory, MCP). Ch. 6 already covers Prompt Builder / agent lifecycle.

- [ ] Decide: keep “platform first, reasoning second” arc **or** move reasoning adjacent to Ch. 6
- [ ] Minimum fix without renumbering: cross-references + tighter Ch. 6 pointer to Ch. 11

---

### Chapter 12 — §12.1 “Looking Back” chapter map is wrong

`Project_Athlon_Book_Chapter_12_Project_Athlon_Reference_Architecture.md` §12.1 (~lines 38–46) misattributes topics to chapter numbers. Examples:

| Stated | Actual chapter topic |
|--------|----------------------|
| Ch. 2 → specialized engineering agents | Ch. 2 — Vision of Project Athlon |
| Ch. 3 → workflow orchestration | Ch. 3 — Engineering Principles |
| Ch. 5 → workflow execution backbone | Ch. 5 — Architecture *(preview)* |
| Ch. 6–8 → Artifact-Driven Engineering | Ch. 6 Agent Runtime · Ch. 7 Orchestrator · Ch. 8 ADE |

- [ ] Rewrite §12.1 recap to match `INDEX.md` chapter list (synthesis chapter should accurately map the book)

---

### Chapter 12 — two chapters in one file

Same pattern as Ch. 11.

| Part | Span | Pivot |
|------|------|-------|
| A — Reference architecture synthesis | §12.1 – §12.21 | |
| B — Implementation guide | §12.22+ | §12.22 *From Reference Architecture to Reference Implementation* (~line 932) |

- [x] Ch. 12 formatting review — **done 2026-07-10** (see Formatting Review Log)
- [ ] Align with Appendix A/B dedup (see Priority 1)

---

### Chapter 9 — duplicate unnumbered headings

`Project_Athlon_Book_Chapter_09_Memory_and_Knowledge_Architecture.md`

- [ ] Two `# Reference Architecture` sections (mid-chapter + end) — renumber or rename (e.g. §9.x / “Complete Memory Architecture”)

---

## Priority 3 — Cross-manuscript consistency (lower urgency)

### Title punctuation — em dash vs triple hyphen

| Style | Chapters |
|-------|----------|
| `---` (three hyphens) | Ch. 1–5 |
| `—` (em dash) | Ch. 6–12, Appendices |

- [ ] Pick one convention for `# Project Athlon …` and `# Chapter N …` titles

---

### Section heading level — H1 vs H2 numbering

| Style | Chapters |
|-------|----------|
| `## 5.1 …` (H2) | Ch. 5 |
| `# 6.1 …` (H1) | Ch. 6–12 |

- [ ] Normalize section heading levels (affects TOC depth in some renderers)

---

### Bullet list spacing

- [ ] Ch. 1–2 / 5 use `-   ` (three spaces after dash); Ch. 6+ often use `- ` (one space) — cosmetic consistency only

---

## Priority 4 — Pending review (may surface new items)

Files not yet walked in the one-by-one formatting pass:

| File | Status |
|------|--------|
| `Project_Athlon_Book_Chapter_11_Engineering_Agent_Behavior.md` | **SKIP — frozen** |
| `Project_Athlon_Book_Chapter_12_Project_Athlon_Reference_Architecture.md` | **Formatting done** — content issues in Priority 2 |
| `Project_Athlon_Book_Chapter_Appendix_A_Building_Project_Athlon.md` | **Formatting done** — A/B overlap remains in Priority 1 |
| `Project_Athlon_Book_Chapter_Appendix_B_Reference_Implementation_Guide.md` | **Formatting done** — A/B overlap remains in Priority 1 |
| `Project_Athlon_Book_Chapter_Appendix_C_Project_Athlon_Pattern_Catalog.md` | **Formatting done** |
| `Project_Athlon_Book_Chapter_Appendix_D_The_Project_Athlon_Playbook.md` | **Formatting done** |

- [x] Run formatting verification on Appendices A–D — **complete 2026-07-10**
- [x] Ch. 12 review — added §12.1 chapter-map error to Priority 2

---

## Formatting Review Log

Presentation-only fixes applied file-by-file. Does not resolve content/structure items in Priority 1–3.

| File | Date | Status | Notes |
|------|------|--------|-------|
| `INDEX.md` | 2026-07-10 | OK | No changes |
| Ch. 1–5 | 2026-07-10 | Fixed | Separators, lists, tables, ` ```text ` |
| Ch. 6–7 | 2026-07-10 | Fixed | Fence tags, diagram compaction, lists |
| Ch. 8 | 2026-07-10 | Fixed | JSON compaction |
| Ch. 9 | 2026-07-10 | Fixed | Inline code, bullet lists |
| Ch. 10 | 2026-07-10 | Fixed | Inline code, diagram alignment |
| Ch. 11 | — | **SKIP** | Frozen — no edits |
| Ch. 12 | 2026-07-10 | Fixed | Bare fences → ` ```text ` / inline / bullets; solution trees; §12.18, §12.28, §12.37 lists |
| Appendix A | 2026-07-10 | Fixed | Repo/docs/src trees; 10+ catalog sections → bullets; A.15–A.22 fence spacing |
| Appendix B | 2026-07-10 | Fixed | 20+ bare fences → ` ```text ` / bullets; B.4 dev order; B.23 milestones |
| Appendix C | 2026-07-10 | Fixed | Pattern relationship diagram → ` ```text ` (compact) |
| Appendix D | 2026-07-10 | Fixed | Blank lines before/after ` ```text ` in §D.6, §D.15 |

---

## Suggested remediation sequence

### Pass 2A — Quick wins (~1–2 hours)

1. Error-recovery ladder → canonical Ch. 6
2. Human-approval gates → canonical Ch. 4
3. Ch. 10 heading renames + section reorder
4. Ch. 5 preview disclaimer paragraph

### Pass 2B — Structural (editorial decision)

**Option B (recommended — less disruptive than full renumbering):**

- Ch. 12 ends at synthesis; §12.22+ → Appendix B
- Appendix A owns development **order**; Appendix B owns **how to build**; delete duplicates
- Ch. 11 / 12: explicit transition blocks unless splitting chapters

**Option A:** Split Ch. 11 and Ch. 12 at their seams into separate chapter numbers.

**Option C:** Move Ch. 11 reasoning content adjacent to Ch. 6 (full renumbering).

---

## Resolved (structural — tracked previously in `INDEX.md`)

These are **done**; listed here for context only.

- [x] Part labels I–VI consistent across all chapters
- [x] Ch. 5 renamed + Part II label + index *(preview)* marker
- [x] Ch. 5 / Ch. 12 title collision resolved
- [x] Duplicate Learning Objectives merged (Ch. 11, Ch. 12)
- [x] ADR-001–042 unbroken chain; Ch. 3 / Ch. 5 ADR-001 collision → cross-reference
- [x] Filename cleanup (Ch. 5, Ch. 12, Appendix C typo)
- [x] Business Analyst Agent naming standardized

---

## Changelog

| Date | Change |
|------|--------|
| 2026-07-10 | Initial TODO created from formatting review Ch. 1–10 + prior LLM structural reviews |
| 2026-07-10 | **Formatting review complete** (16/17 files; Ch. 11 frozen) |
| 2026-07-10 | Appendix D formatting review complete |
| 2026-07-10 | Appendix B formatting review complete |
| 2026-07-10 | Appendix A formatting review complete |
| 2026-07-10 | Ch. 12 formatting review complete; §12.1 chapter-map error added to Priority 2 |
