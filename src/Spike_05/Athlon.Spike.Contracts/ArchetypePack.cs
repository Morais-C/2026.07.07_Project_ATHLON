namespace Athlon.Spike.Contracts;

/// <summary>
/// Loaded and validated archetype pack with absolute paths resolved from manifest.
/// </summary>
public sealed record ArchetypePack(
    string ArchetypeId,
    string Version,
    string Status,
    string DisplayName,
    string Description,
    string PackRoot,
    ArchetypePackPaths Paths,
    ArchetypeBaseline Baseline,
    ArchetypeCodeContextCaps CodeContext,
    IReadOnlyList<string> ChangeRequestKinds,
    IReadOnlyList<ArchetypeArtifactChainEntry> ArtifactChain,
    ArchetypeAgentPaths Analyst,
    ArchetypeAgentPaths Planner,
    ArchetypeAgentPaths Coder)
{
    public string ResolveSchemaPath(string relativePath) =>
        Path.GetFullPath(Path.Combine(PackRoot, relativePath));

    public string ResolvePromptPath(string relativePath) =>
        Path.GetFullPath(Path.Combine(PackRoot, relativePath));
}

public sealed record ArchetypePackPaths(
    string Bounds,
    string CodeContext,
    string ChangeRequest,
    string SchemasDirectory,
    string PromptsDirectory,
    string PatchApply,
    string ProofPipeline,
    string Demos);

public sealed record ArchetypeBaseline(
    string FixtureId,
    string FixtureRoot);

public sealed record ArchetypeCodeContextCaps(
    int MaxFilesAllowed,
    int MaxCharsAllowed,
    IReadOnlyList<string> IncludeExtensions);

public sealed record ArchetypeArtifactChainEntry(
    string Type,
    string Producer,
    string? Schema);

public sealed record ArchetypeAgentPaths(
    string PromptPath,
    string OutputSchemaPath);
