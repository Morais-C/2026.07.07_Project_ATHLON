namespace Athlon.Spike.Contracts;

public sealed record PublishResult(
    Guid WorkflowInstanceId,
    Guid CodePackageArtifactId,
    string PublishDirectory,
    bool BuildSucceeded,
    string BuildOutput,
    string ManifestPath,
    string? FailureMessage)
{
    /// <summary>Materialize + build only. Functional run checks are deferred to a future Tester agent.</summary>
    public bool Succeeded => BuildSucceeded && FailureMessage is null;
}
