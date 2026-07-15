using System.Text.Json;
using System.Text.Json.Serialization;

namespace Athlon.Spike.Contracts;

public sealed record ImplementationTask(string Id, string Description, string Estimate);

public sealed record ImplementationArtifactPayload(
    string Title,
    string Summary,
    IReadOnlyList<ImplementationTask> Tasks,
    IReadOnlyList<string> AcceptanceCriteria,
    string TechnicalNotes);

public static class ImplementationArtifact
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static Artifact Create(
        ImplementationArtifactPayload payload,
        Guid workflowInstanceId,
        string producer,
        Guid? artifactId = null,
        int version = 1)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentException.ThrowIfNullOrWhiteSpace(producer);

        var payloadJson = JsonSerializer.Serialize(payload, JsonOptions);

        return new Artifact(
            Id: artifactId ?? Guid.NewGuid(),
            Type: ArtifactTypes.Implementation,
            Version: version,
            Producer: producer,
            CreatedUtc: DateTime.UtcNow,
            WorkflowInstanceId: workflowInstanceId,
            PayloadJson: payloadJson);
    }

    public static ImplementationArtifactPayload Parse(Artifact artifact)
    {
        ArgumentNullException.ThrowIfNull(artifact);

        if (!string.Equals(artifact.Type, ArtifactTypes.Implementation, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Expected artifact type '{ArtifactTypes.Implementation}', got '{artifact.Type}'.",
                nameof(artifact));
        }

        return JsonSerializer.Deserialize<ImplementationArtifactPayload>(artifact.PayloadJson, JsonOptions)
            ?? throw new InvalidOperationException("Implementation artifact payload is missing or invalid.");
    }
}
