using System.Text.Json;
using System.Text.Json.Serialization;

namespace Athlon.Spike.Contracts;

/// <summary>
/// Analyst output when the change is in bounds — never published on OOB abort (L7).
/// </summary>
public sealed record StructuredChangePayload(
    string Kind,
    string Title,
    string Summary,
    IReadOnlyList<string> AcceptanceCriteria,
    IReadOnlyList<string> Constraints,
    string Priority,
    IReadOnlyList<string>? SuspectedPaths = null);

public static class StructuredChange
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static Artifact Create(
        StructuredChangePayload payload,
        Guid workflowInstanceId,
        string producer,
        Guid? artifactId = null,
        int version = 1)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentException.ThrowIfNullOrWhiteSpace(producer);
        ChangeRequest.ValidateKind(payload.Kind);

        var payloadJson = JsonSerializer.Serialize(payload, JsonOptions);

        return new Artifact(
            Id: artifactId ?? Guid.NewGuid(),
            Type: ArtifactTypes.StructuredChange,
            Version: version,
            Producer: producer,
            CreatedUtc: DateTime.UtcNow,
            WorkflowInstanceId: workflowInstanceId,
            PayloadJson: payloadJson);
    }

    public static StructuredChangePayload Parse(Artifact artifact)
    {
        ArgumentNullException.ThrowIfNull(artifact);

        if (!string.Equals(artifact.Type, ArtifactTypes.StructuredChange, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Expected artifact type '{ArtifactTypes.StructuredChange}', got '{artifact.Type}'.",
                nameof(artifact));
        }

        return JsonSerializer.Deserialize<StructuredChangePayload>(artifact.PayloadJson, JsonOptions)
            ?? throw new InvalidOperationException("StructuredChange payload is missing or invalid.");
    }
}
