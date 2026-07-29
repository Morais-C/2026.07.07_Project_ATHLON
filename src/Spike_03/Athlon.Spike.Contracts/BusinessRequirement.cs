using System.Text.Json;
using System.Text.Json.Serialization;

namespace Athlon.Spike.Contracts;

public static class BusinessRequirement
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static Artifact FromText(
        string text,
        Guid workflowInstanceId,
        string producer = "Console",
        Guid? artifactId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        var payload = JsonSerializer.Serialize(new BusinessRequirementPayload(text), JsonOptions);

        return new Artifact(
            Id: artifactId ?? Guid.NewGuid(),
            Type: ArtifactTypes.BusinessRequirement,
            Version: 1,
            Producer: producer,
            CreatedUtc: DateTime.UtcNow,
            WorkflowInstanceId: workflowInstanceId,
            PayloadJson: payload);
    }

    public static string GetText(Artifact artifact)
    {
        ArgumentNullException.ThrowIfNull(artifact);

        if (!string.Equals(artifact.Type, ArtifactTypes.BusinessRequirement, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Expected artifact type '{ArtifactTypes.BusinessRequirement}', got '{artifact.Type}'.",
                nameof(artifact));
        }

        var payload = JsonSerializer.Deserialize<BusinessRequirementPayload>(artifact.PayloadJson, JsonOptions)
            ?? throw new InvalidOperationException("Business requirement payload is missing or invalid.");

        return payload.Text;
    }

    private sealed record BusinessRequirementPayload(string Text);
}
