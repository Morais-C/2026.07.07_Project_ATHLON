using System.Text.Json;
using System.Text.Json.Serialization;

namespace Athlon.Spike.Contracts;

/// <summary>
/// Loads and validates an archetype pack from disk by id.
/// Resolves manifest-relative paths to absolute paths under the pack root.
/// </summary>
public static class ArchetypePackLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static ArchetypePack Load(string spikeRoot, string archetypeId)
    {
        if (string.IsNullOrWhiteSpace(spikeRoot))
        {
            throw new ArgumentException("Spike root is required.", nameof(spikeRoot));
        }

        if (string.IsNullOrWhiteSpace(archetypeId))
        {
            throw new ArgumentException("Archetype id is required.", nameof(archetypeId));
        }

        var normalizedSpikeRoot = Path.GetFullPath(spikeRoot);
        var packRoot = Path.GetFullPath(Path.Combine(normalizedSpikeRoot, "archetypes", archetypeId));
        var manifestPath = Path.Combine(packRoot, "archetype.json");

        if (!Directory.Exists(packRoot))
        {
            throw new ArchetypePackException(
                $"Archetype pack directory not found: '{packRoot}'.");
        }

        if (!File.Exists(manifestPath))
        {
            throw new ArchetypePackException(
                $"Archetype manifest not found: '{manifestPath}'.");
        }

        ArchetypeManifestDocument manifest;
        try
        {
            var json = File.ReadAllText(manifestPath);
            manifest = JsonSerializer.Deserialize<ArchetypeManifestDocument>(json, JsonOptions)
                ?? throw new ArchetypePackException($"Archetype manifest is empty: '{manifestPath}'.");
        }
        catch (JsonException ex)
        {
            throw new ArchetypePackException(
                $"Archetype manifest JSON is invalid: '{manifestPath}'.", ex);
        }

        ValidateIdentity(manifest, archetypeId, manifestPath);
        ValidateRequiredSections(manifest, manifestPath);

        var paths = ResolvePaths(packRoot, manifest.Paths!);
        ValidatePathsExist(paths, packRoot);

        var baseline = ResolveBaseline(packRoot, manifest.Baseline!);
        ValidateBaseline(baseline);

        var codeContext = ResolveCodeContext(manifest.CodeContext!);
        var changeRequestKinds = manifest.ChangeRequest!.Kinds!
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Select(k => k.Trim())
            .ToList();

        if (changeRequestKinds.Count == 0)
        {
            throw new ArchetypePackException(
                $"Archetype manifest '{manifestPath}' must declare at least one ChangeRequest kind.");
        }

        var artifactChain = manifest.ArtifactChain!
            .Select(entry => new ArchetypeArtifactChainEntry(
                Type: RequireNonEmpty(entry.Type, "artifactChain.type", manifestPath),
                Producer: RequireNonEmpty(entry.Producer, "artifactChain.producer", manifestPath),
                Schema: entry.Schema))
            .ToList();

        var analyst = ResolveAgentPaths(packRoot, manifest.Agents!.Analyst!, "Analyst", manifestPath);
        var planner = ResolveAgentPaths(packRoot, manifest.Agents.Planner!, "Planner", manifestPath);
        var coder = ResolveAgentPaths(packRoot, manifest.Agents.Coder!, "Coder", manifestPath);

        var proofPipelinePath = ResolvePackPath(packRoot, manifest.ProofPipeline!);
        RequireExistingFile(proofPipelinePath, "proofPipeline", manifestPath);

        return new ArchetypePack(
            ArchetypeId: manifest.ArchetypeId!,
            Version: manifest.Version!,
            Status: manifest.Status!,
            DisplayName: manifest.DisplayName!,
            Description: manifest.Description ?? string.Empty,
            PackRoot: packRoot,
            Paths: paths,
            Baseline: baseline,
            CodeContext: codeContext,
            ChangeRequestKinds: changeRequestKinds,
            ArtifactChain: artifactChain,
            Analyst: analyst,
            Planner: planner,
            Coder: coder);
    }

    private static void ValidateIdentity(
        ArchetypeManifestDocument manifest,
        string requestedArchetypeId,
        string manifestPath)
    {
        var manifestId = RequireNonEmpty(manifest.ArchetypeId, "archetypeId", manifestPath);
        if (!string.Equals(manifestId, requestedArchetypeId, StringComparison.Ordinal))
        {
            throw new ArchetypePackException(
                $"Archetype id mismatch in '{manifestPath}': requested '{requestedArchetypeId}', manifest has '{manifestId}'.");
        }

        _ = RequireNonEmpty(manifest.Version, "version", manifestPath);
        _ = RequireNonEmpty(manifest.Status, "status", manifestPath);
        _ = RequireNonEmpty(manifest.DisplayName, "displayName", manifestPath);
    }

    private static void ValidateRequiredSections(
        ArchetypeManifestDocument manifest,
        string manifestPath)
    {
        if (manifest.Paths is null)
        {
            throw new ArchetypePackException($"Archetype manifest '{manifestPath}' is missing 'paths'.");
        }

        if (manifest.Baseline is null)
        {
            throw new ArchetypePackException($"Archetype manifest '{manifestPath}' is missing 'baseline'.");
        }

        if (manifest.CodeContext is null)
        {
            throw new ArchetypePackException($"Archetype manifest '{manifestPath}' is missing 'codeContext'.");
        }

        if (manifest.ChangeRequest?.Kinds is null || manifest.ChangeRequest.Kinds.Count == 0)
        {
            throw new ArchetypePackException($"Archetype manifest '{manifestPath}' is missing 'changeRequest.kinds'.");
        }

        if (manifest.ArtifactChain is null || manifest.ArtifactChain.Count == 0)
        {
            throw new ArchetypePackException($"Archetype manifest '{manifestPath}' is missing 'artifactChain'.");
        }

        if (manifest.Agents?.Analyst is null || manifest.Agents.Planner is null || manifest.Agents.Coder is null)
        {
            throw new ArchetypePackException($"Archetype manifest '{manifestPath}' is missing agent definitions.");
        }

        if (string.IsNullOrWhiteSpace(manifest.ProofPipeline))
        {
            throw new ArchetypePackException($"Archetype manifest '{manifestPath}' is missing 'proofPipeline'.");
        }
    }

    private static ArchetypePackPaths ResolvePaths(string packRoot, ArchetypePathsDocument pathsDocument)
    {
        return new ArchetypePackPaths(
            Bounds: ResolvePackPath(packRoot, pathsDocument.Bounds!),
            CodeContext: ResolvePackPath(packRoot, pathsDocument.CodeContext!),
            ChangeRequest: ResolvePackPath(packRoot, pathsDocument.ChangeRequest!),
            SchemasDirectory: ResolvePackPath(packRoot, pathsDocument.Schemas!),
            PromptsDirectory: ResolvePackPath(packRoot, pathsDocument.Prompts!),
            PatchApply: ResolvePackPath(packRoot, pathsDocument.PatchApply!),
            ProofPipeline: ResolvePackPath(packRoot, pathsDocument.Proof!),
            Demos: ResolvePackPath(packRoot, pathsDocument.Demos!));
    }

    private static void ValidatePathsExist(ArchetypePackPaths paths, string packRoot)
    {
        RequireExistingFile(paths.Bounds, "paths.bounds", packRoot);
        RequireExistingFile(paths.CodeContext, "paths.codeContext", packRoot);
        RequireExistingFile(paths.ChangeRequest, "paths.changeRequest", packRoot);
        RequireExistingDirectory(paths.SchemasDirectory, "paths.schemas", packRoot);
        RequireExistingDirectory(paths.PromptsDirectory, "paths.prompts", packRoot);
        RequireExistingFile(paths.PatchApply, "paths.patchApply", packRoot);
        RequireExistingFile(paths.ProofPipeline, "paths.proof", packRoot);
        RequireExistingFile(paths.Demos, "paths.demos", packRoot);
    }

    private static ArchetypeBaseline ResolveBaseline(string packRoot, ArchetypeBaselineDocument baselineDocument)
    {
        var fixtureId = RequireNonEmpty(baselineDocument.FixtureId, "baseline.fixtureId", packRoot);
        var fixturePath = RequireNonEmpty(baselineDocument.FixturePath, "baseline.fixturePath", packRoot);
        var fixtureRoot = Path.GetFullPath(Path.Combine(packRoot, fixturePath));

        return new ArchetypeBaseline(fixtureId, fixtureRoot);
    }

    private static void ValidateBaseline(ArchetypeBaseline baseline)
    {
        if (!Directory.Exists(baseline.FixtureRoot))
        {
            throw new ArchetypePackException(
                $"Baseline fixture directory not found: '{baseline.FixtureRoot}' (fixtureId '{baseline.FixtureId}').");
        }
    }

    private static ArchetypeCodeContextCaps ResolveCodeContext(ArchetypeCodeContextDocument codeContextDocument)
    {
        if (codeContextDocument.MaxFilesAllowed < 1)
        {
            throw new ArchetypePackException("codeContext.maxFilesAllowed must be at least 1.");
        }

        if (codeContextDocument.MaxCharsAllowed < 1)
        {
            throw new ArchetypePackException("codeContext.maxCharsAllowed must be at least 1.");
        }

        var extensions = codeContextDocument.IncludeExtensions?
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Select(e => e.Trim())
            .ToList() ?? [];

        if (extensions.Count == 0)
        {
            throw new ArchetypePackException("codeContext.includeExtensions must declare at least one extension.");
        }

        return new ArchetypeCodeContextCaps(
            codeContextDocument.MaxFilesAllowed,
            codeContextDocument.MaxCharsAllowed,
            extensions);
    }

    private static ArchetypeAgentPaths ResolveAgentPaths(
        string packRoot,
        ArchetypeAgentDocument agentDocument,
        string agentName,
        string manifestPath)
    {
        var promptRelative = RequireNonEmpty(agentDocument.Prompt, $"agents.{agentName}.prompt", manifestPath);
        var schemaRelative = RequireNonEmpty(agentDocument.OutputSchema, $"agents.{agentName}.outputSchema", manifestPath);

        var promptPath = ResolvePackPath(packRoot, promptRelative);
        var schemaPath = ResolvePackPath(packRoot, schemaRelative);

        RequireExistingFile(promptPath, $"agents.{agentName}.prompt", manifestPath);
        RequireExistingFile(schemaPath, $"agents.{agentName}.outputSchema", manifestPath);

        return new ArchetypeAgentPaths(promptPath, schemaPath);
    }

    private static string ResolvePackPath(string packRoot, string relativePath) =>
        Path.GetFullPath(Path.Combine(packRoot, relativePath));

    private static string RequireNonEmpty(string? value, string fieldName, string contextPath)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArchetypePackException(
                $"Archetype manifest '{contextPath}' is missing required field '{fieldName}'.");
        }

        return value.Trim();
    }

    private static void RequireExistingFile(string absolutePath, string fieldName, string contextPath)
    {
        if (!File.Exists(absolutePath))
        {
            throw new ArchetypePackException(
                $"Archetype pack file not found for '{fieldName}' in '{contextPath}': '{absolutePath}'.");
        }
    }

    private static void RequireExistingDirectory(string absolutePath, string fieldName, string contextPath)
    {
        if (!Directory.Exists(absolutePath))
        {
            throw new ArchetypePackException(
                $"Archetype pack directory not found for '{fieldName}' in '{contextPath}': '{absolutePath}'.");
        }
    }

    private sealed class ArchetypeManifestDocument
    {
        public string? ArchetypeId { get; set; }

        public string? Version { get; set; }

        public string? Status { get; set; }

        public string? DisplayName { get; set; }

        public string? Description { get; set; }

        public ArchetypePathsDocument? Paths { get; set; }

        public ArchetypeBaselineDocument? Baseline { get; set; }

        public ArchetypeCodeContextDocument? CodeContext { get; set; }

        public ArchetypeChangeRequestDocument? ChangeRequest { get; set; }

        public List<ArchetypeArtifactChainDocument>? ArtifactChain { get; set; }

        public ArchetypeAgentsDocument? Agents { get; set; }

        public string? ProofPipeline { get; set; }
    }

    private sealed class ArchetypePathsDocument
    {
        public string? Bounds { get; set; }

        public string? CodeContext { get; set; }

        public string? ChangeRequest { get; set; }

        public string? Schemas { get; set; }

        public string? Prompts { get; set; }

        public string? PatchApply { get; set; }

        public string? Proof { get; set; }

        public string? Demos { get; set; }
    }

    private sealed class ArchetypeBaselineDocument
    {
        public string? FixtureId { get; set; }

        public string? FixturePath { get; set; }
    }

    private sealed class ArchetypeCodeContextDocument
    {
        public int MaxFilesAllowed { get; set; }

        public int MaxCharsAllowed { get; set; }

        public List<string>? IncludeExtensions { get; set; }
    }

    private sealed class ArchetypeChangeRequestDocument
    {
        public List<string>? Kinds { get; set; }
    }

    private sealed class ArchetypeArtifactChainDocument
    {
        public string? Type { get; set; }

        public string? Producer { get; set; }

        public string? Schema { get; set; }
    }

    private sealed class ArchetypeAgentsDocument
    {
        public ArchetypeAgentDocument? Analyst { get; set; }

        public ArchetypeAgentDocument? Planner { get; set; }

        public ArchetypeAgentDocument? Coder { get; set; }
    }

    private sealed class ArchetypeAgentDocument
    {
        public string? Prompt { get; set; }

        [JsonPropertyName("outputSchema")]
        public string? OutputSchema { get; set; }
    }
}
