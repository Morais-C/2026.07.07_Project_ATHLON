using Athlon.Spike.Contracts;

namespace Athlon.Spike.Artifacts;

public sealed class FileArtifactStore : IArtifactStore
{
    private readonly string _rootPath;

    public FileArtifactStore(string? rootPath = null)
    {
        _rootPath = rootPath
            ?? Environment.GetEnvironmentVariable("ATHLON_ARTIFACTS_PATH")
            ?? Path.Combine(Directory.GetCurrentDirectory(), "artifacts");
    }

    public string RootPath => _rootPath;

    public async Task SaveAsync(Artifact artifact, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(artifact);

        var directory = Path.Combine(_rootPath, artifact.WorkflowInstanceId.ToString("D"));
        var filePath = Path.Combine(directory, $"{artifact.Id:D}.json");

        if (File.Exists(filePath))
        {
            throw new InvalidOperationException(
                $"Artifact '{artifact.Id}' already exists at '{filePath}'. Artifacts are immutable.");
        }

        Directory.CreateDirectory(directory);

        var json = ArtifactJson.Serialize(artifact);
        await File.WriteAllTextAsync(filePath, json, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Artifact?> LoadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(_rootPath))
        {
            return null;
        }

        foreach (var filePath in Directory.EnumerateFiles(_rootPath, $"{id:D}.json", SearchOption.AllDirectories))
        {
            var json = await File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);
            return ArtifactJson.Deserialize(json);
        }

        return null;
    }
}
