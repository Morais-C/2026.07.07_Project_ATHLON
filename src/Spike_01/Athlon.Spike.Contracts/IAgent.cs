namespace Athlon.Spike.Contracts;

public interface IAgent
{
    string Name { get; }
    Task<Artifact> ExecuteAsync(AgentExecutionContext context, CancellationToken cancellationToken = default);
}
