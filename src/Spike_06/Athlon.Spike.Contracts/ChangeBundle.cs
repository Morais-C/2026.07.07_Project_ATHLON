using System.Text.Json;
using System.Text.Json.Serialization;

namespace Athlon.Spike.Contracts;

/// <summary>
/// Thin handoff for Planner: one input artifact id that points at StructuredChange + CodeContext (L4).
/// </summary>
public sealed record ChangeBundlePayload(Guid StructuredChangeId, Guid CodeContextId);

public static class ChangeBundle
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static Artifact Create(
        ChangeBundlePayload payload,
        Guid workflowInstanceId,
        string producer = "Workflow",
        Guid? artifactId = null,
        int version = 1)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentException.ThrowIfNullOrWhiteSpace(producer);

        if (payload.StructuredChangeId == Guid.Empty)
        {
            throw new ArgumentException("structuredChangeId is required.", nameof(payload));
        }

        if (payload.CodeContextId == Guid.Empty)
        {
            throw new ArgumentException("codeContextId is required.", nameof(payload));
        }

        var payloadJson = JsonSerializer.Serialize(payload, JsonOptions);

        return new Artifact(
            Id: artifactId ?? Guid.NewGuid(),
            Type: ArtifactTypes.ChangeBundle,
            Version: version,
            Producer: producer,
            CreatedUtc: DateTime.UtcNow,
            WorkflowInstanceId: workflowInstanceId,
            PayloadJson: payloadJson);
    }

    public static ChangeBundlePayload Parse(Artifact artifact)
    {
        ArgumentNullException.ThrowIfNull(artifact);

        if (!string.Equals(artifact.Type, ArtifactTypes.ChangeBundle, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Expected artifact type '{ArtifactTypes.ChangeBundle}', got '{artifact.Type}'.",
                nameof(artifact));
        }

        var payload = JsonSerializer.Deserialize<ChangeBundlePayload>(artifact.PayloadJson, JsonOptions)
            ?? throw new InvalidOperationException("ChangeBundle payload is missing or invalid.");

        if (payload.StructuredChangeId == Guid.Empty || payload.CodeContextId == Guid.Empty)
        {
            throw new InvalidOperationException("ChangeBundle ids must be non-empty GUIDs.");
        }

        return payload;
    }
}
