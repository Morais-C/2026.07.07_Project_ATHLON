using System.Text.Json;
using System.Text.Json.Serialization;

namespace Athlon.Spike.Contracts;

public sealed record CodePackageFile(string Path, string Content);

public sealed record CodePackagePayload(
    IReadOnlyList<CodePackageFile> Files,
    string EntryProject,
    string TargetFramework,
    string ExpectedOutputContains);

public static class CodePackage
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static Artifact Create(
        CodePackagePayload payload,
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
                $"CodePackage paths are unsafe: {string.Join("; ", pathErrors)}",
                nameof(payload));
        }

        var payloadJson = JsonSerializer.Serialize(payload, JsonOptions);

        return new Artifact(
            Id: artifactId ?? Guid.NewGuid(),
            Type: ArtifactTypes.CodePackage,
            Version: version,
            Producer: producer,
            CreatedUtc: DateTime.UtcNow,
            WorkflowInstanceId: workflowInstanceId,
            PayloadJson: payloadJson);
    }

    public static CodePackagePayload Parse(Artifact artifact)
    {
        ArgumentNullException.ThrowIfNull(artifact);

        if (!string.Equals(artifact.Type, ArtifactTypes.CodePackage, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Expected artifact type '{ArtifactTypes.CodePackage}', got '{artifact.Type}'.",
                nameof(artifact));
        }

        var payload = JsonSerializer.Deserialize<CodePackagePayload>(artifact.PayloadJson, JsonOptions)
            ?? throw new InvalidOperationException("CodePackage payload is missing or invalid.");

        var pathErrors = ValidatePaths(payload);
        if (pathErrors.Count > 0)
        {
            throw new InvalidOperationException(
                $"CodePackage paths are unsafe: {string.Join("; ", pathErrors)}");
        }

        return payload;
    }

    /// <summary>
    /// Relative paths only — reject absolute paths and '..' segments (L9).
    /// </summary>
    public static IReadOnlyList<string> ValidatePaths(CodePackagePayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(payload.EntryProject) || !IsSafeRelativePath(payload.EntryProject))
        {
            errors.Add($"entryProject must be a relative path without '..': '{payload.EntryProject}'");
        }

        if (payload.Files is null || payload.Files.Count == 0)
        {
            errors.Add("files must contain at least one entry.");
            return errors;
        }

        foreach (var file in payload.Files)
        {
            if (file is null || string.IsNullOrWhiteSpace(file.Path) || !IsSafeRelativePath(file.Path))
            {
                errors.Add($"file path must be relative without '..': '{file?.Path}'");
            }
        }

        return errors;
    }

    public static bool IsSafeRelativePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var trimmed = path.Trim();

        if (System.IO.Path.IsPathRooted(trimmed))
        {
            return false;
        }

        // UNC / drive-style / leading slash variants Path.IsPathRooted may miss on some inputs
        if (trimmed.StartsWith('/') ||
            trimmed.StartsWith('\\') ||
            trimmed.StartsWith("\\\\", StringComparison.Ordinal) ||
            (trimmed.Length >= 2 && char.IsLetter(trimmed[0]) && trimmed[1] == ':'))
        {
            return false;
        }

        var parts = trimmed.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 0 && !parts.Any(static part => part is "..");
    }
}
