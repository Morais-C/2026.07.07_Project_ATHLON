namespace Athlon.Spike.Contracts;

public record Artifact(
    Guid Id,
    string Type,
    int Version,
    string Producer,
    DateTime CreatedUtc,
    Guid WorkflowInstanceId,
    string PayloadJson);
