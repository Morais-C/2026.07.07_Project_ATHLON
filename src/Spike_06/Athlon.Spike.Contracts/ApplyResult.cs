namespace Athlon.Spike.Contracts;

/// <summary>
/// Outcome of Applier: copy fixture → apply PatchPackage → proof gates.
/// Gates: apply → build → OpenAPI consistency → contract tests.
/// Functional run checks are deferred to a future Tester agent.
/// </summary>
public sealed record ApplyResult(
    Guid WorkflowInstanceId,
    Guid PatchPackageArtifactId,
    string FixtureId,
    string PublishDirectory,
    bool ApplySucceeded,
    bool BuildSucceeded,
    string BuildOutput,
    bool? OpenapiConsistencySucceeded,
    string? OpenapiConsistencyOutput,
    bool? ContractTestsSucceeded,
    string? ContractTestOutput,
    string ManifestPath,
    string? FailureMessage)
{
    /// <summary>
    /// All proof gates passed: apply + build + OpenAPI consistency + contract tests.
    /// </summary>
    public bool Succeeded =>
        ApplySucceeded
        && BuildSucceeded
        && (OpenapiConsistencySucceeded ?? true)
        && (ContractTestsSucceeded ?? true)
        && FailureMessage is null;
}
