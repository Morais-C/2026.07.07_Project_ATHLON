# Athlon vs GitLab Duo vs Devin vs Mendix Mentor — REST API archetype

> **Context:** Strategic positioning for the first **commercial** **Solution Archetype** — **`rest-api-v1`** (Spike_06).  
> **Vision:** [Chapter 2 — Solution Archetypes](Project_Athlon_Book_Chapter_02_Vision_of_Project_Athlon.md#solution-archetypes)  
> **Execution proof:** Spike_04 (console incremental change); Spike_05 (`console-v1` pack); Spike_06 (`rest-api-v1` pack **proved**).  
> **Last updated:** 2026-09-15

---

## What we mean by “REST API” here

The **REST API Solution Archetype** is Athlon’s governed change pipeline for a bounded backend:

| Dimension | REST API archetype (target) |
|-----------|----------------------------|
| **Stack** | e.g. ASP.NET minimal API / Web API, OpenAPI-first, net9+ |
| **Intake** | ChangeRequest (feature \| bugfix) + optional OpenAPI touch hints |
| **Bounds** | Max endpoints/resources, allowed auth pattern, no arbitrary NuGet, no undeclared external deps |
| **Contract** | OpenAPI artifact is source of truth; code and tests must align |
| **Chain** | Analyst → CodeContext → Planner → Coder (patch) → Applier → build + contract tests |
| **Proof** | `dotnet build`, OpenAPI diff/consistency, API tests (Tester agent later) |
| **Output** | Immutable artifact chain + buildable tree or PR; audit manifest |

This is **not** “generate any backend from a paragraph.” It is **reliable change** on **known API baselines** inside explicit caps.

---

## One-sentence positioning

| Product | One line |
|---------|----------|
| **Athlon (REST API archetype)** | Governed ChangeRequest → artifact chain → patch + **deterministic proof** on a **bounded REST API** baseline you own. |
| **GitLab Duo Agent Platform** | DevSecOps **system of record** orchestrating agents across the lifecycle inside GitLab. |
| **Devin** | Autonomous **AI software engineer** in a sandbox—issue to code/PR on **your repo**, general scope. |
| **Mendix Mentor** | **Agentic low-code** delivery inside Mendix’s model-driven platform—not repo-first REST APIs. |

---

## Comparison matrix (REST API change scenario)

Scenario: *“Add `GET /customers/{id}` and fix 404 when id is malformed”* on an **existing** API the team already owns.

| Criterion | **Athlon** | **GitLab Duo** | **Devin** | **Mendix Mentor** |
|-----------|------------|----------------|-----------|-------------------|
| **Primary home** | Athlon workflow + artifacts (+ future portal/API) | GitLab (issues, MRs, CI) | Devin cloud / CLI + your Git | Mendix Studio Pro |
| **Unit of work** | ChangeRequest → StructuredChange → … → PatchPackage | Issue/MR + agent chat/flows | Natural-language task / ticket | Prompt + visual model in Mendix |
| **Handoff model** | **Artifact id only** (immutable JSON on disk) | GitLab context + chat/session | Session + repo state | Model + agent session |
| **Scope enforcement** | **Analyst abort** if outside REST API archetype bounds | Policy/rules; soft on “any repo” | Prompt + human review; generalist | Mendix platform bounds |
| **REST / OpenAPI as contract** | **First-class** (planned archetype pack) | Via project context; not archetype-SKU | Inferred from repo | OData/REST exposure of Mendix apps—not generic OpenAPI repos |
| **Change style** | Unified diff on **existing baseline** (Spike_04) | MR commits via agents | Edits in sandbox → PR | Model/code generation in Mendix stack |
| **Deterministic proof** | **Applier + build** (+ contract tests planned); fail-fast manifest | CI/CD in GitLab; agent doesn’t own proof layer | Runs tests in sandbox; success = agent report | Build/deploy within Mendix pipeline |
| **Audit trail** | Per-workflow artifact folder + manifest | GitLab audit + Duo logs | Devin session logs | Mendix governance/observability |
| **Human gates** | At artifact boundaries (product intent) | MR approval, policies | Review PR / intervene in session | Approvals in Mendix governance |
| **Vendor lock-in** | Your repo + artifacts (goal: optional Git) | **GitLab** ecosystem | Cognition + git remote | **Mendix runtime** |
| **Best buyer** | Eng org wanting **governed API change** on **their** .NET/OpenAPI repos | Teams already on **GitLab Ultimate** | Teams wanting **async generalist** engineer | Enterprises on **Mendix** for apps + agents |

---

## Strengths and gaps by product

### Athlon (REST API archetype — target)

**Strengths**

- Same ChangeRequest for feature and bugfix; structured intake.
- Artifact chain optimized for **compliance and replay** (who decided what, when).
- **Bounded archetype** → higher reliability than “change anything.”
- OpenAPI-centric proof story (contract tests as gate).
- Repo-native: customer keeps standard ASP.NET + OpenAPI, not a proprietary app server.

**Gaps (today / roadmap)**

- REST API archetype **proved as pack** (Spike_06); promotion to `Athlon.*` is next.
- No GitLab/Jira-native assign flow until portal/API sprint.
- Tester/functional run deferred; need API-level test agent for credible “done.”
- Smaller brand vs GitLab/Cognition/Mendix.

---

### GitLab Duo Agent Platform

**Strengths**

- Already the **SDLC system of record** for many enterprises.
- GA agent platform (2026): orchestrate across code, security, ops.
- Integrates **external agents** (e.g. Claude Code, Codex CLI).
- GitLab Duo + **Amazon Q**: issue → MR, review, unit tests, upgrades.
- Strong **compliance** story inside GitLab.

**Gaps (vs Athlon REST API thesis)**

- Not sold as **Solution Archetype SKUs** (REST API pack with Analyst hard abort).
- Handoff is **platform/session/MR-centric**, not immutable artifact-id thesis.
- Proof is **your CI**; Duo doesn’t standardize “OpenAPI + patch + manifest” as product contract.
- Value assumes **GitLab adoption**; less compelling for GitHub-only or Azure DevOps shops.

**When customer picks GitLab over Athlon**

- Already standardized on GitLab Ultimate; want agents **inside** existing DevSecOps.
- Need broad lifecycle (security, deploy, ops) more than archetype-bounded API change.

**When Athlon wins**

- Need **artifact-auditable** change factory **outside** or **above** a single vendor SCM.
- REST API governance (OpenAPI as gate) as **product**, not CI script glue.

---

### Devin (Cognition)

**Strengths**

- Mature **autonomous engineer** brand; sandbox with shell, editor, browser.
- Strong async workflow: assign ticket → plan → implement → PR.
- Works on **real repos**; Jira/Linear integration.
- Good for **backlog burn-down** and general implementation tasks.

**Gaps (vs Athlon REST API thesis)**

- **Generalist**—no guaranteed REST API bounds or OpenAPI-first proof pipeline.
- Session/chat lineage; weaker **immutable artifact chain** as system of record.
- Success narrative is “PR merged,” not “StructuredChange + PatchPackage + manifest.”
- Enterprise buyers may worry about **black-box autonomy** without archetype gates.

**When customer picks Devin over Athlon**

- Want one **general AI teammate** for mixed tasks, not a governed API change factory.
- Willing to standardize on Cognition’s runtime and review model.

**When Athlon wins**

- Regulated or audit-heavy teams need **explicit bounds** and **replayable artifacts** per change.
- API program wants **contract-first** discipline enforced by platform, not hope.

---

### Mendix Mentor

**Strengths**

- **Enterprise low-code + agentic SDLC** in one vendor stack.
- Model-driven coherence; governance and observability **built in**.
- Mentor orchestrates agents across plan/build/validate in Studio Pro.
- MCP integration with Cursor/Claude Code for hybrid teams.

**Gaps (vs Athlon REST API thesis)**

- Delivers **Mendix applications**, not arbitrary **ASP.NET OpenAPI repos**.
- REST exposure is **platform-shaped**, not “your existing API codebase + patch.”
- Different buyer: **low-code center of excellence**, not API platform team on git.

**When customer picks Mendix over Athlon**

- Strategic commitment to **Mendix** for digital apps; agents accelerate **that** stack.
- Business-led delivery with visual model as source of truth.

**When Athlon wins**

- API team owns **.NET services in git**; Mendix runtime is wrong fit.
- Need **incremental change on baseline** (Spike_04 model), not greenfield app gen in IDE.

---

## “Isn’t this AI low-code?”

| | AI low-code / app builders | Horizontal agents (Devin, Copilot) | **Athlon REST API archetype** |
|--|------------------------------|-----------------------------------|-------------------------------|
| **Output** | App in vendor or generated stack | PR / branch | Artifact chain + verified API tree/PR |
| **Baseline** | Often greenfield | Existing repo | **Existing API** + OpenAPI |
| **Scope** | Platform rules | Soft | **Hard Analyst abort** |
| **Proof** | Platform deploy | Tests in agent env | **Deterministic build + contract tests** |

Athlon shares the **natural-language intake** of low-code and the **agent roster** of DevSecOps platforms—but productizes **bounded REST API change** on **customer-owned code**, with **artifacts as the interface**.

---

## Recommended talk track (boss / customer)

1. **Problem:** “We need to change our APIs faster without losing control—features and bugfixes, audit trail, proof before merge.”
2. **Why not only Devin/Copilot:** “Great for general coding; we need **REST API bounds** and **immutable artifacts**, not only a PR.”
3. **Why not only GitLab Duo:** “Keep GitLab for SCM/CI; Athlon is the **governed change layer** for **API archetypes** with OpenAPI proof—portable above the vendor.”
4. **Why not Mendix:** “Wrong runtime if our APIs live in **.NET + OpenAPI repos**.”
5. **Proof today:** Spike_04 showed artifact chain + patch + build on console; Spike_05 productizes **`console-v1`** as a pack; Spike_06 proved **`rest-api-v1`** (OpenAPI + contract tests) before promotion.

---

## Next build steps (execution link)

| Step | Links to |
|------|----------|
| Spike_05 — `console-v1` pack + loader | [Spike_05 plan](../src/Spike_05/ImplementationPlan.md) |
| Spike_06 — `rest-api-v1` pack (bounds, OpenAPI schema, contract tests) | [Spike_06 plan](../src/Spike_06/ImplementationPlan.md) — **proved (pack)** |
| Promote Spike_06 → `Athlon.*` | [PromotionPlan.md](../PromotionPlan.md) |
| Contract-bound clients (web/mobile) | After REST API hub; consume same OpenAPI artifact |

---

## References (external products)

- [GitLab Duo Agent Platform GA](https://about.gitlab.com/) (2026)
- [GitLab Duo with Amazon Q](https://docs.gitlab.com/user/duo_amazon_q/)
- [Devin — Cognition](https://cognition.com/)
- [Mendix Studio Pro / Mentor](https://www.mendix.com/products/studio-pro/)
- Project Athlon spikes: [Spike_04](../src/Spike_04/README.md)
