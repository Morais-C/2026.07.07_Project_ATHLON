using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Athlon.Spike.Contracts;

namespace Athlon.Spike.Workflow;

/// <summary>
/// Deterministic publisher: CodePackage (by id) → Publish/{workflowId}/ + dotnet build.
/// Functional run/output checks are deferred to a future Tester agent (not LLM here).
/// </summary>
public sealed class Publisher
{
    private const int MaxCapturedOutputChars = 64 * 1024;

    private static readonly JsonSerializerOptions ManifestJsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IArtifactStore _artifactStore;
    private readonly string _publishRoot;
    private readonly TimeSpan _buildTimeout;

    public Publisher(
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

    public async Task<PublishResult> PublishAsync(
        Guid workflowInstanceId,
        Guid codePackageArtifactId,
        CancellationToken cancellationToken = default)
    {
        var artifact = await _artifactStore.LoadAsync(codePackageArtifactId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException(
                $"CodePackage artifact '{codePackageArtifactId}' was not found.");

        if (!string.Equals(artifact.Type, ArtifactTypes.CodePackage, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Publisher expects a CodePackage artifact, got '{artifact.Type}'.");
        }

        if (artifact.WorkflowInstanceId != workflowInstanceId)
        {
            throw new InvalidOperationException(
                $"CodePackage workflow id '{artifact.WorkflowInstanceId}' does not match '{workflowInstanceId}'.");
        }

        var package = CodePackage.Parse(artifact);
        var publishDirectory = Path.Combine(_publishRoot, workflowInstanceId.ToString("D"));

        if (Directory.Exists(publishDirectory))
        {
            throw new InvalidOperationException(
                $"Publish directory already exists (immutable): '{publishDirectory}'.");
        }

        var publishDirectoryFull = EnsureTrailingSeparator(Path.GetFullPath(publishDirectory));
        Directory.CreateDirectory(publishDirectory);

        MaterializeFiles(publishDirectoryFull, package);

        var entryProjectPath = Path.GetFullPath(Path.Combine(publishDirectoryFull, package.EntryProject));
        if (!entryProjectPath.StartsWith(publishDirectoryFull, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"entryProject resolved outside publish directory: '{package.EntryProject}'.");
        }

        if (!File.Exists(entryProjectPath))
        {
            throw new InvalidOperationException(
                $"entryProject file was not materialized: '{package.EntryProject}'.");
        }

        Console.WriteLine($"Publishing to      : {publishDirectoryFull}");
        Console.WriteLine($"Building            : {package.EntryProject}");

        var (buildSucceeded, buildOutput) = await RunDotnetBuildAsync(
            $"build \"{entryProjectPath}\" --nologo",
            publishDirectoryFull,
            _buildTimeout,
            cancellationToken).ConfigureAwait(false);

        string? failure = buildSucceeded ? null : "dotnet build failed.";
        if (buildSucceeded)
        {
            Console.WriteLine("Build succeeded. (Functional run checks deferred to Tester agent.)");
        }

        var manifestPath = await WriteManifestAsync(
            publishDirectoryFull,
            workflowInstanceId,
            codePackageArtifactId,
            package,
            buildSucceeded,
            Truncate(buildOutput),
            failure,
            cancellationToken).ConfigureAwait(false);

        return new PublishResult(
            workflowInstanceId,
            codePackageArtifactId,
            publishDirectoryFull,
            BuildSucceeded: buildSucceeded,
            BuildOutput: Truncate(buildOutput),
            ManifestPath: manifestPath,
            FailureMessage: failure);
    }

    private static string EnsureTrailingSeparator(string path) =>
        path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar)
            ? path
            : path + Path.DirectorySeparatorChar;

    private static void MaterializeFiles(string publishDirectory, CodePackagePayload package)
    {
        foreach (var file in package.Files)
        {
            if (!CodePackage.IsSafeRelativePath(file.Path))
            {
                throw new InvalidOperationException($"Refusing unsafe file path: '{file.Path}'.");
            }

            var fullPath = Path.GetFullPath(Path.Combine(publishDirectory, file.Path));
            if (!fullPath.StartsWith(publishDirectory, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Refusing path escape: '{file.Path}'.");
            }

            if (File.Exists(fullPath))
            {
                throw new InvalidOperationException($"Refusing overwrite of existing file: '{file.Path}'.");
            }

            var dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllText(fullPath, file.Content, Encoding.UTF8);
        }
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
        Guid codePackageArtifactId,
        CodePackagePayload package,
        bool buildSucceeded,
        string buildOutput,
        string? failureMessage,
        CancellationToken cancellationToken)
    {
        var manifestPath = Path.Combine(publishDirectory, "publish-manifest.json");
        if (File.Exists(manifestPath))
        {
            throw new InvalidOperationException($"Manifest already exists: '{manifestPath}'.");
        }

        var manifest = new
        {
            workflowInstanceId,
            codePackageArtifactId,
            entryProject = package.EntryProject,
            targetFramework = package.TargetFramework,
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
