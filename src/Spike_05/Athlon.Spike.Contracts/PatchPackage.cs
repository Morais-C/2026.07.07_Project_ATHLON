using System.Text.Json;
using System.Text.Json.Serialization;

namespace Athlon.Spike.Contracts;

public sealed record PatchFileChange(string Path, string Operation, string UnifiedDiff);

public sealed record PatchPackagePayload(
    string FixtureId,
    IReadOnlyList<PatchFileChange> Changes,
    string EntryProject,
    string TargetFramework,
    string Summary);

public static class PatchPackage
{
    public const string OperationModify = "modify";
    public const string OperationCreate = "create";
    public const string OperationDelete = "delete";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static Artifact Create(
        PatchPackagePayload payload,
        Guid workflowInstanceId,
        string producer,
        Guid? artifactId = null,
        int version = 1)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentException.ThrowIfNullOrWhiteSpace(producer);

        var pathErrors = ValidatePaths(payload);
        if (pathErrors.Count > 0)
        {
            throw new ArgumentException(
                $"PatchPackage paths are unsafe: {string.Join("; ", pathErrors)}",
                nameof(payload));
        }

        var payloadJson = JsonSerializer.Serialize(payload, JsonOptions);

        return new Artifact(
            Id: artifactId ?? Guid.NewGuid(),
            Type: ArtifactTypes.PatchPackage,
            Version: version,
            Producer: producer,
            CreatedUtc: DateTime.UtcNow,
            WorkflowInstanceId: workflowInstanceId,
            PayloadJson: payloadJson);
    }

    public static PatchPackagePayload Parse(Artifact artifact)
    {
        ArgumentNullException.ThrowIfNull(artifact);

        if (!string.Equals(artifact.Type, ArtifactTypes.PatchPackage, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Expected artifact type '{ArtifactTypes.PatchPackage}', got '{artifact.Type}'.",
                nameof(artifact));
        }

        var payload = JsonSerializer.Deserialize<PatchPackagePayload>(artifact.PayloadJson, JsonOptions)
            ?? throw new InvalidOperationException("PatchPackage payload is missing or invalid.");

        var pathErrors = ValidatePaths(payload);
        if (pathErrors.Count > 0)
        {
            throw new InvalidOperationException(
                $"PatchPackage paths are unsafe: {string.Join("; ", pathErrors)}");
        }

        return payload;
    }

    /// <summary>
    /// Relative paths only — reject absolute paths and '..' segments (L9).
    /// Also checks operation ∈ {modify, create, delete} and non-empty unifiedDiff.
    /// </summary>
    public static IReadOnlyList<string> ValidatePaths(PatchPackagePayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(payload.FixtureId))
        {
            errors.Add("fixtureId is required.");
        }

        if (string.IsNullOrWhiteSpace(payload.EntryProject) || !IsSafeRelativePath(payload.EntryProject))
        {
            errors.Add($"entryProject must be a relative path without '..': '{payload.EntryProject}'");
        }

        if (!string.Equals(payload.TargetFramework, "net9.0", StringComparison.Ordinal))
        {
            errors.Add($"targetFramework must be 'net9.0', got '{payload.TargetFramework}'.");
        }

        if (payload.Changes is null || payload.Changes.Count == 0)
        {
            errors.Add("changes must contain at least one entry.");
            return errors;
        }

        foreach (var change in payload.Changes)
        {
            if (change is null || string.IsNullOrWhiteSpace(change.Path) || !IsSafeRelativePath(change.Path))
            {
                errors.Add($"change path must be relative without '..': '{change?.Path}'");
            }

            if (!IsKnownOperation(change?.Operation))
            {
                errors.Add(
                    $"change operation must be '{OperationModify}', '{OperationCreate}', or '{OperationDelete}': '{change?.Operation}'");
            }

            if (string.IsNullOrWhiteSpace(change?.UnifiedDiff))
            {
                errors.Add($"unifiedDiff is required for path '{change?.Path}'.");
            }
        }

        return errors;
    }

    public static bool IsSafeRelativePath(string path) => CodePackage.IsSafeRelativePath(path);

    private static bool IsKnownOperation(string? operation) =>
        string.Equals(operation, OperationModify, StringComparison.Ordinal) ||
        string.Equals(operation, OperationCreate, StringComparison.Ordinal) ||
        string.Equals(operation, OperationDelete, StringComparison.Ordinal);
}
