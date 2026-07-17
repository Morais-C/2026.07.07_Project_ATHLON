namespace Athlon.Spike.Contracts;

public record AgentExecutionContext(
    Guid WorkflowInstanceId,
    Guid InputArtifactId);
