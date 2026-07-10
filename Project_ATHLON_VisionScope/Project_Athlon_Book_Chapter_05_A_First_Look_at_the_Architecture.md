# Project Athlon --- Building the Autonomous SDLC

# Part II — Principles & Vision

# Chapter 5 --- A First Look at the Architecture

> **Project Athlon** is an open platform for Agentic Software
> Engineering. The Autonomous SDLC is its flagship reference
> implementation.

This chapter is a deliberate **preview** of the full platform. It maps
every major subsystem at a glance so you know where the book is headed.
Chapters 6–11 explain each area in depth. Chapter 12 synthesizes the
whole. ADRs introduced here are expanded in the formal ledger as the
narrative progresses.

## 5.1 Architectural Vision

Athlon separates *engineering workflow* from *LLM implementation*.
Models, tools and orchestration engines can evolve without changing the
platform's core contracts.

### Goals

-   Model agnostic
-   Tool agnostic
-   Human governed
-   Observable
-   Extensible
-   Incrementally adoptable

---

## 5.2 Logical Architecture

```text
                 Human User
                     │
                     ▼
              Athlon Portal/API
                     │
         Human Approval Service
                     │
             Workflow Orchestrator
                     │
    ┌────────┬────────┬────────┐
    ▼        ▼        ▼        ▼
 Business Architect Developer Reviewer
 Analyst    Agent      Agent    Agent
    │        │          │        │
    └────────┴─────┬────┴────────┘
                   ▼
               QA Agent
                   ▼
         Documentation Agent
                   ▼
       Git / CI / Deployment Tools
```

### Core Components

| Component | Responsibility |
| --- | --- |
| Portal/API | Receives requests and exposes status |
| Orchestrator | Executes workflows and manages state |
| Agent Runtime | Hosts specialized AI agents |
| Artifact Store | Stores versioned outputs |
| Memory | Supplies reusable context |
| MCP Layer | Access to external tools |
| LLM Provider | Routes requests to AI models |

---

## 5.3 Agent Runtime

Every agent follows the same lifecycle.

```text
Input
  │
Context Retrieval
  │
Prompt Assembly
  │
LLM Invocation
  │
Schema Validation
  │
Artifact Generation
  │
Quality Checks
  │
Next Agent
```

### Standard Agent Contract

Each agent exposes:

-   Name
-   Version
-   Purpose
-   Accepted artifacts
-   Produced artifacts
-   Required tools
-   Validation rules
-   Confidence score

---

## 5.4 Artifact-Driven Engineering

Agents exchange immutable artifacts rather than conversations.

Example:

```json
{
  "artifactId": "USR-001",
  "type": "UserStory",
  "version": "1.0",
  "producer": "BusinessAnalyst",
  "status": "Approved",
  "payload": {
    "title": "Support remote meal allowance",
    "acceptanceCriteria": []
  }
}
```

### Benefits

-   Auditability
-   Repeatability
-   Easier testing
-   Model independence
-   Traceability

---

## 5.5 Workflow Orchestrator

Responsibilities:

-   Start workflows
-   Route artifacts
-   Handle retries
-   Pause for approvals
-   Execute parallel branches
-   Record execution history

The orchestrator never contains business knowledge; it coordinates
execution.

---

## 5.6 Memory Architecture

Four logical layers:

1.  Session Memory
2.  Project Memory
3.  Engineering Knowledge
4.  Organizational Knowledge

Initially these may be Markdown files and structured JSON. Vector search
can be introduced later without changing agent contracts.

---

## 5.7 MCP Integration

Agents never manipulate external systems directly.

Instead they request capabilities from MCP servers.

Typical servers:

-   Filesystem
-   Git
-   SQL Server
-   Docker
-   Browser
-   GitHub
-   Azure DevOps

This keeps agents portable and secure.

---

## 5.8 LLM Provider Abstraction

Define an interface such as:

```text
ILLMProvider
 ├─ OpenAIProvider
 ├─ AnthropicProvider
 ├─ GeminiProvider
 └─ LocalModelProvider
```

Routing strategies may consider quality, latency and cost.

---

## 5.9 Security

Security principles include:

-   Least privilege for tools
-   Human approval for sensitive actions
-   Prompt injection protection
-   Secret isolation
-   Immutable audit trail

---

## 5.10 Observability

Capture for every execution:

-   Agent
-   Model
-   Prompt version
-   Tokens
-   Duration
-   Cost
-   Validation results
-   Human approvals

These metrics enable continuous evaluation.

---

## 5.11 Extensibility

New agents should be added by implementing the common contract.

Examples:

-   Delphi Modernization Agent
-   Payroll Compliance Agent
-   Security Review Agent
-   Architecture Migration Agent

No orchestrator changes should be required.

---

## 5.12 Initial ADRs

**ADR-001** --- Start with sequential orchestration *(previewed
informally in Chapter 3)*.

**ADR-002** --- Exchange structured artifacts, not chat transcripts.

**ADR-003** --- Human approval is mandatory for critical stages.

**ADR-004** --- Abstract LLM providers.

**ADR-005** --- Integrate external capabilities through MCP.

---

## Chapter Summary

This architecture deliberately emphasizes durable software engineering
principles over framework-specific features. By separating
orchestration, agents, artifacts, memory and tools through well-defined
contracts, Project Athlon can evolve from a simple proof of concept into
an extensible platform for Agentic Software Engineering.
