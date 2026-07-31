namespace Athlon.Spike.Contracts;

public interface IArtifactStore
{
    Task SaveAsync(Artifact artifact, CancellationToken cancellationToken = default);
    Task<Artifact?> LoadAsync(Guid id, CancellationToken cancellationToken = default);
}
