# rest-api-v1 — ChangeRequest profile

## Schema fields

| Field | Required | Notes |
|-------|----------|-------|
| `kind` | ✅ | `feature` or `bugfix` |
| `title` | ✅ | Short summary (e.g., "Add Product resource") |
| `description` | ✅ | Detailed explanation of the change |
| `stepsToReproduce` | ❌ | For bugfixes: how to trigger the bug |
| `expectedBehavior` | ❌ | For bugfixes: what should happen |
| `actualBehavior` | ❌ | For bugfixes: what currently happens |
| `suspectedPaths` | ❌ | Hints for Analyst/Planner (e.g., `openapi.yaml`, `MiniErp/Program.cs`) |

## Feature requests

Use REST/resource-oriented language:
- "Add a Product resource with POST /products and GET /products/{id}"
- "Add customer list endpoint GET /customers"
- "Add validation for product SKU uniqueness"

## Bugfix requests

Include reproduction details:
- Steps to reproduce (e.g., "POST to /products with empty name")
- Expected behavior (e.g., "Return 400 Bad Request with validation error")
- Actual behavior (e.g., "Returns 500 Internal Server Error")

## Demo catalog

Concrete demo ChangeRequests are in `demos/change-requests.json`:
- **add-product-resource**: Feature — Add Product resource with POST/GET endpoints
- **add-customer-resource**: Feature — Add Customer resource with POST/GET endpoints
- **fix-missing-id-404**: Bugfix — Return 404 for GET /products/{id} when id not found

## Common suspectedPaths

| Path | When to hint |
|------|--------------|
| `openapi.yaml` | Adding/changing endpoints, schemas, or responses |
| `MiniErp/Program.cs` | Changing endpoint implementation |
| `MiniErp.ContractTests/HealthEndpointTests.cs` | Adding/fixing contract tests (or create new test file) |
