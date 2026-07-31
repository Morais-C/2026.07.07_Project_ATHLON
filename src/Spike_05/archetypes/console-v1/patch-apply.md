# console-v1 — Patch / apply rules

How Coder expresses changes and how Applier materializes them. Conventions match Spike_04 Applier behavior.

## PatchPackage format

- **Artifact type:** `PatchPackage` (produced by Coder, consumed by Applier)
- **Schema:** `schemas/patch-package.schema.json`
- **Changes array:** 1–4 items; each item has `path`, `operation`, `unifiedDiff`

### Operations

| Operation | unifiedDiff convention |
|-----------|------------------------|
| `modify` | Standard unified diff: `--- a/{path}` / `+++ b/{path}` against CodeContext content |
| `create` | `--- /dev/null` then `+++ b/{path}` with added lines only |
| `delete` | `--- a/{path}` then `+++ /dev/null` removing all lines (never empty unifiedDiff) |

### Path safety

- All paths **relative** to fixture/publish root
- Reject `..`, absolute paths, drive letters, UNC roots
- `entryProject` must end with `.csproj` and resolve inside publish tree
- `targetFramework` must be exactly `net9.0`

### Diff quality (Coder prompt)

- Hunk headers `@@ -oldStart,oldCount +newStart,newCount @@` must match line counts
- Include enough unchanged context lines for clean apply against exact CodeContext content
- Use `\n` for newlines inside the JSON string

## Apply sequence (Applier)

1. Load PatchPackage artifact by id (handoff rule).
2. Verify `fixtureId` matches pack baseline (`echo-v1`).
3. Copy entire fixture → `Publish/{workflowId}/` (skip `bin`, `obj`, `.git`).
4. Apply each change in order; any failure → `applySucceeded: false`, keep tree for inspection.
5. Run `dotnet build` on `entryProject` under publish directory.
6. Write `apply-manifest.json` with `applySucceeded`, `buildSucceeded`, `failureMessage`, `buildOutput`.

## Failure behavior

- **Fail fast:** no success manifest when apply or build fails
- Publish directory is **immutable** per workflow id — refuse if folder already exists
- Functional run/output checks deferred to future Tester agent

## Proof gates

See `proof/pipeline.json` — Gate 1 (apply) then Gate 2 (`dotnet build`).
