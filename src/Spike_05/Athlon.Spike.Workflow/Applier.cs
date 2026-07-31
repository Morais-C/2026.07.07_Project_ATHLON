using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Athlon.Spike.Contracts;

namespace Athlon.Spike.Workflow;

/// <summary>
/// Deterministic applier: fixture + PatchPackage (by id) → Publish/{workflowId}/ + dotnet build.
/// Keep tree + failed manifest on apply/build failure (never mark success). Functional run → Tester later.
/// </summary>
public sealed class Applier
{
    private const int MaxCapturedOutputChars = 64 * 1024;

    private static readonly JsonSerializerOptions ManifestJsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly HashSet<string> SkippedDirectoryNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "bin",
        "obj",
        ".git"
    };

    private readonly IArtifactStore _artifactStore;
    private readonly string _publishRoot;
    private readonly TimeSpan _buildTimeout;

    public Applier(
        IArtifactStore artifactStore,
        string? publishRoot = null,
        TimeSpan? buildTimeout = null)
    {
        _artifactStore = artifactStore ?? throw new ArgumentNullException(nameof(artifactStore));
        _publishRoot = string.IsNullOrWhiteSpace(publishRoot)
            ? Path.Combine(Directory.GetCurrentDirectory(), "Publish")
            : Path.GetFullPath(publishRoot);
        _buildTimeout = buildTimeout ?? TimeSpan.FromMinutes(2);
    }

    public async Task<ApplyResult> ApplyAsync(
        Guid workflowInstanceId,
        Guid patchPackageArtifactId,
        string expectedFixtureId,
        string fixtureRoot,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedFixtureId);
        ArgumentException.ThrowIfNullOrWhiteSpace(fixtureRoot);

        var artifact = await _artifactStore.LoadAsync(patchPackageArtifactId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException(
                $"PatchPackage artifact '{patchPackageArtifactId}' was not found.");

        if (!string.Equals(artifact.Type, ArtifactTypes.PatchPackage, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Applier expects a PatchPackage artifact, got '{artifact.Type}'.");
        }

        if (artifact.WorkflowInstanceId != workflowInstanceId)
        {
            throw new InvalidOperationException(
                $"PatchPackage workflow id '{artifact.WorkflowInstanceId}' does not match '{workflowInstanceId}'.");
        }

        var package = PatchPackage.Parse(artifact);
        if (!string.Equals(package.FixtureId, expectedFixtureId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"PatchPackage fixtureId '{package.FixtureId}' does not match expected '{expectedFixtureId}'.");
        }

        var fixtureRootFull = Path.GetFullPath(fixtureRoot);
        if (!Directory.Exists(fixtureRootFull))
        {
            throw new DirectoryNotFoundException($"Fixture root not found: '{fixtureRootFull}'.");
        }

        var publishDirectory = Path.Combine(_publishRoot, workflowInstanceId.ToString("D"));
        if (Directory.Exists(publishDirectory))
        {
            throw new InvalidOperationException(
                $"Publish directory already exists (immutable): '{publishDirectory}'.");
        }

        var publishDirectoryFull = EnsureTrailingSeparator(Path.GetFullPath(publishDirectory));
        Directory.CreateDirectory(publishDirectory);

        Console.WriteLine($"Copying fixture     : {fixtureRootFull}");
        Console.WriteLine($"Publish directory   : {publishDirectoryFull}");

        CopyFixture(fixtureRootFull, publishDirectoryFull);

        string? applyFailure = null;
        try
        {
            ApplyChanges(publishDirectoryFull, package);
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            applyFailure = ex.Message;
            Console.WriteLine($"Apply failed        : {applyFailure}");
        }

        bool buildSucceeded = false;
        var buildOutput = string.Empty;

        if (applyFailure is null)
        {
            var entryProjectPath = Path.GetFullPath(Path.Combine(publishDirectoryFull, package.EntryProject));
            if (!entryProjectPath.StartsWith(publishDirectoryFull, StringComparison.OrdinalIgnoreCase))
            {
                applyFailure = $"entryProject resolved outside publish directory: '{package.EntryProject}'.";
            }
            else if (!File.Exists(entryProjectPath))
            {
                applyFailure = $"entryProject file was not found after apply: '{package.EntryProject}'.";
            }
            else
            {
                Console.WriteLine($"Building            : {package.EntryProject}");
                (buildSucceeded, buildOutput) = await RunDotnetBuildAsync(
                    $"build \"{entryProjectPath}\" --nologo",
                    publishDirectoryFull,
                    _buildTimeout,
                    cancellationToken).ConfigureAwait(false);

                if (buildSucceeded)
                {
                    Console.WriteLine("Build succeeded. (Functional run checks deferred to Tester agent.)");
                }
            }
        }

        var applySucceeded = applyFailure is null;
        string? failure = applyFailure;
        if (applySucceeded && !buildSucceeded)
        {
            failure = "dotnet build failed.";
        }

        var manifestPath = await WriteManifestAsync(
            publishDirectoryFull,
            workflowInstanceId,
            patchPackageArtifactId,
            package,
            applySucceeded,
            buildSucceeded,
            Truncate(buildOutput),
            failure,
            cancellationToken).ConfigureAwait(false);

        return new ApplyResult(
            workflowInstanceId,
            patchPackageArtifactId,
            package.FixtureId,
            publishDirectoryFull,
            ApplySucceeded: applySucceeded,
            BuildSucceeded: buildSucceeded,
            BuildOutput: Truncate(buildOutput),
            ManifestPath: manifestPath,
            FailureMessage: failure);
    }

    private static void CopyFixture(string fixtureRootFull, string publishDirectoryFull)
    {
        var rootWithSep = EnsureTrailingSeparator(fixtureRootFull);

        foreach (var sourcePath in Directory.EnumerateFiles(fixtureRootFull, "*", SearchOption.AllDirectories))
        {
            var relativeDir = Path.GetDirectoryName(sourcePath);
            if (relativeDir is not null && ContainsSkippedDirectory(rootWithSep, relativeDir))
            {
                continue;
            }

            var relative = Path.GetRelativePath(fixtureRootFull, sourcePath);
            var destPath = Path.GetFullPath(Path.Combine(publishDirectoryFull, relative));
            if (!destPath.StartsWith(publishDirectoryFull, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Refusing path escape while copying: '{relative}'.");
            }

            var destDir = Path.GetDirectoryName(destPath);
            if (!string.IsNullOrEmpty(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            File.Copy(sourcePath, destPath, overwrite: false);
        }
    }

    private static void ApplyChanges(string publishDirectoryFull, PatchPackagePayload package)
    {
        foreach (var change in package.Changes)
        {
            if (!PatchPackage.IsSafeRelativePath(change.Path))
            {
                throw new InvalidOperationException($"Refusing unsafe patch path: '{change.Path}'.");
            }

            var fullPath = Path.GetFullPath(Path.Combine(publishDirectoryFull, change.Path));
            if (!fullPath.StartsWith(publishDirectoryFull, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Refusing path escape: '{change.Path}'.");
            }

            var operation = change.Operation;
            if (string.Equals(operation, PatchPackage.OperationCreate, StringComparison.Ordinal))
            {
                if (File.Exists(fullPath))
                {
                    throw new InvalidOperationException(
                        $"create failed — file already exists: '{change.Path}'.");
                }

                var created = UnifiedDiffApplier.ApplyToContent(string.Empty, change.UnifiedDiff);
                var dir = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                File.WriteAllText(fullPath, created, Encoding.UTF8);
            }
            else if (string.Equals(operation, PatchPackage.OperationDelete, StringComparison.Ordinal))
            {
                if (!File.Exists(fullPath))
                {
                    throw new InvalidOperationException(
                        $"delete failed — file not found: '{change.Path}'.");
                }

                // Validate delete diff against current content, then remove the file.
                _ = UnifiedDiffApplier.ApplyToContent(File.ReadAllText(fullPath), change.UnifiedDiff);
                File.Delete(fullPath);
            }
            else if (string.Equals(operation, PatchPackage.OperationModify, StringComparison.Ordinal))
            {
                if (!File.Exists(fullPath))
                {
                    throw new InvalidOperationException(
                        $"modify failed — file not found: '{change.Path}'.");
                }

                var original = File.ReadAllText(fullPath);
                var updated = UnifiedDiffApplier.ApplyToContent(original, change.UnifiedDiff);
                File.WriteAllText(fullPath, updated, Encoding.UTF8);
            }
            else
            {
                throw new InvalidOperationException($"Unknown patch operation: '{operation}'.");
            }
        }
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

    private static string EnsureTrailingSeparator(string path)
    {
        var full = Path.GetFullPath(path);
        return Path.TrimEndingDirectorySeparator(full) + Path.DirectorySeparatorChar;
    }

    private static async Task<(bool Succeeded, string Output)> RunDotnetBuildAsync(
        string arguments,
        string workingDirectory,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi };
        var output = new StringBuilder();
        var outputLock = new object();
        var capped = false;

        void AppendCapped(string? line)
        {
            if (line is null || capped)
            {
                return;
            }

            lock (outputLock)
            {
                if (output.Length >= MaxCapturedOutputChars)
                {
                    if (!capped)
                    {
                        output.AppendLine();
                        output.AppendLine($"… (truncated at {MaxCapturedOutputChars} chars)");
                        capped = true;
                    }

                    return;
                }

                output.AppendLine(line);
                if (output.Length >= MaxCapturedOutputChars)
                {
                    output.AppendLine();
                    output.AppendLine($"… (truncated at {MaxCapturedOutputChars} chars)");
                    capped = true;
                }
            }
        }

        process.OutputDataReceived += (_, e) => AppendCapped(e.Data);
        process.ErrorDataReceived += (_, e) => AppendCapped(e.Data);

        if (!process.Start())
        {
            return (false, "Failed to start dotnet process.");
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.StandardInput.Close();

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout);

        try
        {
            await process.WaitForExitAsync(timeoutCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch
            {
                // ignore kill failures
            }

            return (false, output + $"{Environment.NewLine}Process timed out after {timeout}.");
        }

        return (process.ExitCode == 0, output.ToString());
    }

    private static string Truncate(string text) =>
        text.Length <= MaxCapturedOutputChars
            ? text
            : text[..MaxCapturedOutputChars] + $"{Environment.NewLine}… (truncated)";

    private static async Task<string> WriteManifestAsync(
        string publishDirectory,
        Guid workflowInstanceId,
        Guid patchPackageArtifactId,
        PatchPackagePayload package,
        bool applySucceeded,
        bool buildSucceeded,
        string buildOutput,
        string? failureMessage,
        CancellationToken cancellationToken)
    {
        var manifestPath = Path.Combine(publishDirectory, "apply-manifest.json");
        if (File.Exists(manifestPath))
        {
            throw new InvalidOperationException($"Manifest already exists: '{manifestPath}'.");
        }

        var manifest = new
        {
            workflowInstanceId,
            patchPackageArtifactId,
            fixtureId = package.FixtureId,
            entryProject = package.EntryProject,
            targetFramework = package.TargetFramework,
            applySucceeded,
            buildSucceeded,
            failureMessage,
            buildOutput,
            functionalTest = "deferred-to-tester-agent",
            createdUtc = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(manifest, ManifestJsonOptions);
        await File.WriteAllTextAsync(manifestPath, json, cancellationToken).ConfigureAwait(false);
        return manifestPath;
    }
}
