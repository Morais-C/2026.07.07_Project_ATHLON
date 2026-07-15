namespace Athlon.Spike.Contracts;

public record WorkflowInput(
    string RequirementText,
    bool AutoApprove = false);
