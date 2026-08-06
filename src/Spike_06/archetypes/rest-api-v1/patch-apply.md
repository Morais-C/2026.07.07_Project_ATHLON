# rest-api-v1 — Patch / apply rules

Same Spike_05 Applier semantics, extended proof gates in Spike_06:

1. Copy fixture → `Publish/{workflowId}/` (branch metaphor; never overwrite fixture in place).
2. Verify `fixtureId` matches pack baseline (`mini-erp-v1`).
3. Apply unified diffs from PatchPackage.
4. **Proof gates (ordered):** apply → `dotnet build` → OpenAPI consistency → contract tests.
5. On any gate failure: keep publish tree; record failure in manifest; do not mark workflow success.

## Contract mutability

Checked-in OpenAPI is the **baseline** source of truth. A ChangeRequest may update OpenAPI when adding/changing controllers/endpoints; consistency is evaluated on the **post-apply** publish tree (valid OpenAPI + operations covered by contract tests).
