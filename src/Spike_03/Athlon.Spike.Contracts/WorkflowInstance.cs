namespace Athlon.Spike.Contracts;

public record WorkflowInstance(
    Guid Id,
    string WorkflowName,
    WorkflowStatus Status,
    DateTime StartedUtc,
    DateTime? CompletedUtc = null);
