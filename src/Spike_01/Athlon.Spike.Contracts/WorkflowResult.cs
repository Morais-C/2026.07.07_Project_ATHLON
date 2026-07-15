namespace Athlon.Spike.Contracts;

public record WorkflowResult(
    WorkflowInstance Instance,
    Artifact InputArtifact,
    Artifact? OutputArtifact,
    LlmCompletionResult? Telemetry,
    string? FailureMessage = null)
{
    public bool Succeeded => Instance.Status == WorkflowStatus.Completed;
}
