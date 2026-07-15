namespace Athlon.Spike.Contracts;

public interface IAgent
{
    string Name { get; }
    Task<AgentExecutionResult> ExecuteAsync(AgentExecutionContext context, CancellationToken cancellationToken = default);
}
