namespace Athlon.Spike.Contracts;

/// <summary>
/// Outcome of Applier: copy fixture → apply PatchPackage → dotnet build.
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
    string ManifestPath,
    string? FailureMessage)
{
    public bool Succeeded =>
        ApplySucceeded && BuildSucceeded && FailureMessage is null;
}
