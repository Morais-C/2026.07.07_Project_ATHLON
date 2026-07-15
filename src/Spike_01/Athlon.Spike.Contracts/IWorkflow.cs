namespace Athlon.Spike.Contracts;

public interface IWorkflow
{
    string Name { get; }
    Task<WorkflowResult> RunAsync(WorkflowInput input, CancellationToken cancellationToken = default);
}
