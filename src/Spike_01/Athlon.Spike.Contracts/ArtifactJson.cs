using System.Text.Json;
using System.Text.Json.Serialization;

namespace Athlon.Spike.Contracts;

public static class ArtifactJson
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    public static string Serialize(Artifact artifact) =>
        JsonSerializer.Serialize(artifact, JsonOptions);

    public static Artifact Deserialize(string json) =>
        JsonSerializer.Deserialize<Artifact>(json, JsonOptions)
        ?? throw new InvalidOperationException("Artifact JSON is missing or invalid.");
}
