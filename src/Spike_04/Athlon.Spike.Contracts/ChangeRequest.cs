using System.Text.Json;
using System.Text.Json.Serialization;

namespace Athlon.Spike.Contracts;

/// <summary>
/// Host change intent — one shape covers feature and bugfix (L5 / L13).
/// </summary>
public sealed record ChangeRequestPayload(
    string Kind,
    string Title,
    string Description,
    string? StepsToReproduce = null,
    string? ExpectedBehavior = null,
    string? ActualBehavior = null,
    IReadOnlyList<string>? SuspectedPaths = null);

public static class ChangeRequest
{
    public const string KindFeature = "feature";
    public const string KindBugfix = "bugfix";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static Artifact Create(
        ChangeRequestPayload payload,
        Guid workflowInstanceId,
        string producer = "Console",
        Guid? artifactId = null,
        int version = 1)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentException.ThrowIfNullOrWhiteSpace(producer);
        ValidateKind(payload.Kind);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload.Title);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload.Description);

        var payloadJson = JsonSerializer.Serialize(payload, JsonOptions);

        return new Artifact(
            Id: artifactId ?? Guid.NewGuid(),
            Type: ArtifactTypes.ChangeRequest,
            Version: version,
            Producer: producer,
            CreatedUtc: DateTime.UtcNow,
            WorkflowInstanceId: workflowInstanceId,
            PayloadJson: payloadJson);
    }

    public static ChangeRequestPayload Parse(Artifact artifact)
    {
        ArgumentNullException.ThrowIfNull(artifact);

        if (!string.Equals(artifact.Type, ArtifactTypes.ChangeRequest, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Expected artifact type '{ArtifactTypes.ChangeRequest}', got '{artifact.Type}'.",
                nameof(artifact));
        }

        var payload = JsonSerializer.Deserialize<ChangeRequestPayload>(artifact.PayloadJson, JsonOptions)
            ?? throw new InvalidOperationException("ChangeRequest payload is missing or invalid.");

        ValidateKind(payload.Kind);
        return payload;
    }

    public static void ValidateKind(string kind)
    {
        if (!string.Equals(kind, KindFeature, StringComparison.Ordinal) &&
            !string.Equals(kind, KindBugfix, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"ChangeRequest kind must be '{KindFeature}' or '{KindBugfix}', got '{kind}'.",
                nameof(kind));
        }
    }
}
