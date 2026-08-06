namespace Athlon.Spike.Contracts;

public record AgentExecutionResult(
    Artifact OutputArtifact,
    LlmCompletionResult Telemetry);
