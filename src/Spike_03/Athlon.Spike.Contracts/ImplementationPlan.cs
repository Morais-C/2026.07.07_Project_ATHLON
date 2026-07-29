using System.Text.Json;
using System.Text.Json.Serialization;

namespace Athlon.Spike.Contracts;

public sealed record ImplementationTask(string Id, string Description, string Estimate);

public sealed record ImplementationPlanPayload(
    string Title,
    string Summary,
    IReadOnlyList<ImplementationTask> Tasks,
    IReadOnlyList<string> AcceptanceCriteria,
    string TechnicalNotes);

public static class ImplementationPlan
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static Artifact Create(
        ImplementationPlanPayload payload,
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
            Type: ArtifactTypes.ImplementationPlan,
            Version: version,
            Producer: producer,
            CreatedUtc: DateTime.UtcNow,
            WorkflowInstanceId: workflowInstanceId,
            PayloadJson: payloadJson);
    }

    public static ImplementationPlanPayload Parse(Artifact artifact)
    {
        ArgumentNullException.ThrowIfNull(artifact);

        if (!string.Equals(artifact.Type, ArtifactTypes.ImplementationPlan, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Expected artifact type '{ArtifactTypes.ImplementationPlan}', got '{artifact.Type}'.",
                nameof(artifact));
        }

        return JsonSerializer.Deserialize<ImplementationPlanPayload>(artifact.PayloadJson, JsonOptions)
            ?? throw new InvalidOperationException("ImplementationPlan payload is missing or invalid.");
    }
}
