using Athlon.Spike.Contracts;

namespace Athlon.Spike.Workflow;

/// <summary>
/// Deterministic: fixture on disk → CodeContext artifact (all source files + hashes + caps).
/// Over-cap → fail fast with no publish (L8).
/// </summary>
public sealed class CodeContextBuilder
{
    public const string ProducerName = "CodeContextBuilder";

    private static readonly HashSet<string> IncludedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cs",
        ".csproj"
    };

    private static readonly HashSet<string> SkippedDirectoryNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "bin",
        "obj",
        ".git"
    };

    private readonly IArtifactStore _artifactStore;
    private readonly int _maxFilesAllowed;
    private readonly int _maxCharsAllowed;

    public CodeContextBuilder(
        IArtifactStore artifactStore,
        int maxFilesAllowed = CodeContext.DefaultMaxFilesAllowed,
        int maxCharsAllowed = CodeContext.DefaultMaxCharsAllowed)
    {
        _artifactStore = artifactStore ?? throw new ArgumentNullException(nameof(artifactStore));
        if (maxFilesAllowed < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxFilesAllowed));
        }

        if (maxCharsAllowed < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxCharsAllowed));
        }

        _maxFilesAllowed = maxFilesAllowed;
        _maxCharsAllowed = maxCharsAllowed;
    }

    public async Task<Artifact> BuildAndPublishAsync(
        Guid workflowInstanceId,
        string fixtureId,
        string fixtureRoot,
        string entryProject,
        string targetFramework = "net9.0",
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fixtureId);
        ArgumentException.ThrowIfNullOrWhiteSpace(fixtureRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(entryProject);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetFramework);

        var rootFull = Path.GetFullPath(fixtureRoot);
        if (!Directory.Exists(rootFull))
        {
            throw new DirectoryNotFoundException($"Fixture root not found: '{rootFull}'.");
        }

        var entryFull = Path.GetFullPath(Path.Combine(rootFull, entryProject));
        var rootWithSep = EnsureTrailingSeparator(rootFull);
        if (!entryFull.StartsWith(rootWithSep, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(entryFull, rootFull, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"entryProject resolved outside fixture root: '{entryProject}'.");
        }

        if (!File.Exists(entryFull))
        {
            throw new FileNotFoundException(
                $"entryProject was not found under fixture: '{entryProject}'.",
                entryFull);
        }

        var files = LoadFixtureFiles(rootFull);
        var totalChars = files.Sum(f => f.Content.Length);

        if (files.Count > _maxFilesAllowed)
        {
            throw new InvalidOperationException(
                $"CodeContext over file cap: {files.Count} files > maxFilesAllowed {_maxFilesAllowed}.");
        }

        if (totalChars > _maxCharsAllowed)
        {
            throw new InvalidOperationException(
                $"CodeContext over char cap: {totalChars} chars > maxCharsAllowed {_maxCharsAllowed}.");
        }

        var payload = new CodeContextPayload(
            FixtureId: fixtureId,
            EntryProject: NormalizeRelativePath(entryProject),
            TargetFramework: targetFramework,
            Files: files,
            TotalChars: totalChars,
            MaxFilesAllowed: _maxFilesAllowed,
            MaxCharsAllowed: _maxCharsAllowed);

        var artifact = CodeContext.Create(payload, workflowInstanceId, producer: ProducerName);
        await _artifactStore.SaveAsync(artifact, cancellationToken).ConfigureAwait(false);
        return artifact;
    }

    private static IReadOnlyList<CodeContextFile> LoadFixtureFiles(string rootFull)
    {
        var rootWithSep = EnsureTrailingSeparator(rootFull);
        var results = new List<CodeContextFile>();

        foreach (var path in Directory.EnumerateFiles(rootFull, "*", SearchOption.AllDirectories))
        {
            var relativeDir = Path.GetDirectoryName(path);
            if (relativeDir is not null && ContainsSkippedDirectory(rootWithSep, relativeDir))
            {
                continue;
            }

            var extension = Path.GetExtension(path);
            if (!IncludedExtensions.Contains(extension))
            {
                continue;
            }

            var relative = Path.GetRelativePath(rootFull, path);
            var normalized = NormalizeRelativePath(relative);
            var content = File.ReadAllText(path);
            results.Add(new CodeContextFile(
                Path: normalized,
                Content: content,
                ContentSha256: CodeContext.ComputeSha256(content)));
        }

        if (results.Count == 0)
        {
            throw new InvalidOperationException(
                $"No .cs / .csproj files found under fixture root '{rootFull}'.");
        }

        return results
            .OrderBy(f => f.Path, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool ContainsSkippedDirectory(string rootWithSep, string directoryFull)
    {
        var relative = Path.GetRelativePath(
            Path.TrimEndingDirectorySeparator(rootWithSep),
            directoryFull);

        if (string.IsNullOrEmpty(relative) || relative == ".")
        {
            return false;
        }

        var segments = relative.Split(
            new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
            StringSplitOptions.RemoveEmptyEntries);

        return segments.Any(SkippedDirectoryNames.Contains);
    }

    private static string NormalizeRelativePath(string path) =>
        path.Replace('\\', '/').TrimStart('/');

    private static string EnsureTrailingSeparator(string path)
    {
        var full = Path.GetFullPath(path);
        return Path.TrimEndingDirectorySeparator(full) + Path.DirectorySeparatorChar;
    }
}
