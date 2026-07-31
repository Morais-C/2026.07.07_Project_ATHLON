# console-v1 — Patch / apply rules (Phase 0 placeholder)

- **Format:** unified diff only (`PatchPackage` artifact)
- **Path safety:** paths relative to fixture root; reject `..` traversal
- **Apply:** Applier copies fixture → `Publish/{workflowId}/`, applies diffs, runs `dotnet build`
- **Failure:** fail fast; no partial success manifest

Full conventions copied from Spike_04 Applier in Phase 1.
