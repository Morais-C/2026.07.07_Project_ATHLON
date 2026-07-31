using System.Text.Json;
using System.Text.Json.Serialization;

namespace Athlon.Spike.Contracts;

// BA output shape — schema exists so BA cannot echo thin { "text": "..." }
public sealed record StructuredRequirementPayload(
    string Title,
    IReadOnlyList<string> Actors,
    string Goal,
    IReadOnlyList<string> AcceptanceCriteriaDraft,
    IReadOnlyList<string> Constraints,
    string Priority);

public static class StructuredRequirement
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static Artifact Create(
        StructuredRequirementPayload payload,
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
            Type: ArtifactTypes.StructuredRequirement,
            Version: version,
            Producer: producer,
            CreatedUtc: DateTime.UtcNow,
            WorkflowInstanceId: workflowInstanceId,
            PayloadJson: payloadJson);
    }

    public static StructuredRequirementPayload Parse(Artifact artifact)
    {
        ArgumentNullException.ThrowIfNull(artifact);

        if (!string.Equals(artifact.Type, ArtifactTypes.StructuredRequirement, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Expected artifact type '{ArtifactTypes.StructuredRequirement}', got '{artifact.Type}'.",
                nameof(artifact));
        }

        return JsonSerializer.Deserialize<StructuredRequirementPayload>(artifact.PayloadJson, JsonOptions)
            ?? throw new InvalidOperationException("StructuredRequirement payload is missing or invalid.");
    }
}
