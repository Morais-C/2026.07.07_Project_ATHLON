using Athlon.Spike.Artifacts;
using Athlon.Spike.Contracts;
using Athlon.Spike.Workflow;

namespace Athlon.Spike.Tests;

/// <summary>
/// Hand-written PatchPackage on rest-api-v1 pack baseline (mini-erp-v1) → Applier → proof gates.
/// Phase 2: Tests cover apply + build + OpenAPI consistency + contract tests.
/// </summary>
public class ApplierTests
{
    private static string FixtureId => SpikeTestPaths.RestApiV1Pack.Baseline.FixtureId;

    private static string FixtureRoot => SpikeTestPaths.RestApiV1Pack.Baseline.FixtureRoot;

    private const string EntryProject = "MiniErp/MiniErp.csproj";

    /// <summary>
    /// Minimal diff that adds a comment to Program.cs without breaking the API.
    /// </summary>
    private const string AddCommentDiff =
        """
        --- a/MiniErp/Program.cs
        +++ b/MiniErp/Program.cs
        @@ -1,4 +1,5 @@
        +// Phase 2 test: minimal change to verify proof gates
         var builder = WebApplication.CreateBuilder(args);
         var app = builder.Build();
         
         app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
        """;

    /// <summary>
    /// Diff with wrong context that won't match the fixture.
    /// </summary>
    private const string BadContextDiff =
        """
        --- a/MiniErp/Program.cs
        +++ b/MiniErp/Program.cs
        @@ -1,4 +1,5 @@
        +// This comment won't apply
         // wrong context that does not match the fixture
         var builder = WebApplication.CreateBuilder(args);
         var app = builder.Build();
        """;

    [Fact]
    public async Task Handwritten_PatchPackage_applies_with_all_proof_gates()
    {
        var artifactRoot = CreateTempDir("artifacts");
        var publishRoot = CreateTempDir("publish");
        var store = new FileArtifactStore(artifactRoot);
        var workflowId = Guid.NewGuid();

        try
        {
            var patch = PatchPackage.Create(
                new PatchPackagePayload(
                    FixtureId,
                    [
                        new PatchFileChange(
                            "MiniErp/Program.cs",
                            PatchPackage.OperationModify,
                            AddCommentDiff.Replace("\r\n", "\n"))
                    ],
                    EntryProject,
                    "net9.0",
                    "Add comment to Program.cs"),
                workflowId,
                producer: "HandWritten");
            await store.SaveAsync(patch);

            var applier = new Applier(store, publishRoot: publishRoot);
            var result = await applier.ApplyAsync(
                workflowId,
                patch.Id,
                expectedFixtureId: FixtureId,
                fixtureRoot: FixtureRoot);

            Assert.True(result.ApplySucceeded, result.FailureMessage);
            Assert.True(result.BuildSucceeded, result.BuildOutput);
            Assert.True(result.OpenapiConsistencySucceeded, result.OpenapiConsistencyOutput);
            Assert.True(result.ContractTestsSucceeded, result.ContractTestOutput);
            Assert.True(result.Succeeded, result.FailureMessage);
            Assert.True(File.Exists(result.ManifestPath));

            var publishedProgram = await File.ReadAllTextAsync(
                Path.Combine(result.PublishDirectory, "MiniErp", "Program.cs"));
            Assert.Contains("Phase 2 test", publishedProgram, StringComparison.Ordinal);

            var manifest = await File.ReadAllTextAsync(result.ManifestPath);
            Assert.Contains("\"buildSucceeded\": true", manifest, StringComparison.Ordinal);
            Assert.Contains("\"openapiConsistencySucceeded\": true", manifest, StringComparison.Ordinal);
            Assert.Contains("\"contractTestsSucceeded\": true", manifest, StringComparison.Ordinal);
        }
        finally
        {
            Cleanup(artifactRoot);
            Cleanup(publishRoot);
        }
    }

    [Fact]
    public async Task Baseline_copy_without_changes_passes_all_gates()
    {
        var artifactRoot = CreateTempDir("artifacts");
        var publishRoot = CreateTempDir("publish");
        var store = new FileArtifactStore(artifactRoot);
        var workflowId = Guid.NewGuid();

        try
        {
            var patch = PatchPackage.Create(
                new PatchPackagePayload(
                    FixtureId,
                    [
                        new PatchFileChange(
                            "MiniErp/Program.cs",
                            PatchPackage.OperationModify,
                            NoOpDiff())
                    ],
                    EntryProject,
                    "net9.0",
                    "No-op change (baseline verification)"),
                workflowId,
                producer: "HandWritten");
            await store.SaveAsync(patch);

            var applier = new Applier(store, publishRoot: publishRoot);
            var result = await applier.ApplyAsync(
                workflowId,
                patch.Id,
                expectedFixtureId: FixtureId,
                fixtureRoot: FixtureRoot);

            Assert.True(result.ApplySucceeded, result.FailureMessage);
            Assert.True(result.BuildSucceeded, result.BuildOutput);
            Assert.True(result.OpenapiConsistencySucceeded, result.OpenapiConsistencyOutput);
            Assert.True(result.ContractTestsSucceeded, result.ContractTestOutput);
            Assert.True(result.Succeeded, result.FailureMessage);

            Assert.Contains("GET /health", result.OpenapiConsistencyOutput, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("PASS", result.OpenapiConsistencyOutput, StringComparison.Ordinal);
        }
        finally
        {
            Cleanup(artifactRoot);
            Cleanup(publishRoot);
        }
    }

    [Fact]
    public async Task Fails_when_publish_directory_already_exists()
    {
        var artifactRoot = CreateTempDir("artifacts");
        var publishRoot = CreateTempDir("publish");
        var store = new FileArtifactStore(artifactRoot);
        var workflowId = Guid.NewGuid();

        try
        {
            var patch = await SeedValidPatchAsync(store, workflowId);
            Directory.CreateDirectory(Path.Combine(publishRoot, workflowId.ToString("D")));

            var applier = new Applier(store, publishRoot: publishRoot);
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                applier.ApplyAsync(
                    workflowId,
                    patch.Id,
                    expectedFixtureId: FixtureId,
                    fixtureRoot: FixtureRoot));

            Assert.Contains("already exists", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Cleanup(artifactRoot);
            Cleanup(publishRoot);
        }
    }

    [Fact]
    public async Task Apply_failure_keeps_tree_and_failed_manifest()
    {
        var artifactRoot = CreateTempDir("artifacts");
        var publishRoot = CreateTempDir("publish");
        var store = new FileArtifactStore(artifactRoot);
        var workflowId = Guid.NewGuid();

        try
        {
            var patch = PatchPackage.Create(
                new PatchPackagePayload(
                    FixtureId,
                    [
                        new PatchFileChange(
                            "MiniErp/Program.cs",
                            PatchPackage.OperationModify,
                            BadContextDiff.Replace("\r\n", "\n"))
                    ],
                    EntryProject,
                    "net9.0",
                    "Intentionally broken context"),
                workflowId,
                producer: "HandWritten");
            await store.SaveAsync(patch);

            var applier = new Applier(store, publishRoot: publishRoot);
            var result = await applier.ApplyAsync(
                workflowId,
                patch.Id,
                expectedFixtureId: FixtureId,
                fixtureRoot: FixtureRoot);

            Assert.False(result.ApplySucceeded);
            Assert.False(result.BuildSucceeded);
            Assert.False(result.Succeeded);
            Assert.Contains("mismatch", result.FailureMessage, StringComparison.OrdinalIgnoreCase);
            Assert.True(Directory.Exists(result.PublishDirectory));
            Assert.True(File.Exists(result.ManifestPath));

            var manifest = await File.ReadAllTextAsync(result.ManifestPath);
            Assert.Contains("\"applySucceeded\": false", manifest, StringComparison.Ordinal);
            Assert.Contains("\"buildSucceeded\": false", manifest, StringComparison.Ordinal);
        }
        finally
        {
            Cleanup(artifactRoot);
            Cleanup(publishRoot);
        }
    }

    [Fact]
    public async Task FixtureId_mismatch_throws_before_publish()
    {
        var artifactRoot = CreateTempDir("artifacts");
        var publishRoot = CreateTempDir("publish");
        var store = new FileArtifactStore(artifactRoot);
        var workflowId = Guid.NewGuid();

        try
        {
            var patch = await SeedValidPatchAsync(store, workflowId);
            var applier = new Applier(store, publishRoot: publishRoot);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                applier.ApplyAsync(
                    workflowId,
                    patch.Id,
                    expectedFixtureId: "other-fixture",
                    fixtureRoot: FixtureRoot));

            Assert.Contains("fixtureId", ex.Message, StringComparison.OrdinalIgnoreCase);
            Assert.False(Directory.Exists(Path.Combine(publishRoot, workflowId.ToString("D"))));
        }
        finally
        {
            Cleanup(artifactRoot);
            Cleanup(publishRoot);
        }
    }

    [Fact]
    public async Task Create_and_delete_operations_apply()
    {
        var artifactRoot = CreateTempDir("artifacts");
        var publishRoot = CreateTempDir("publish");
        var fixtureWithNote = CreateTempDir("fixture-with-note");
        var store = new FileArtifactStore(artifactRoot);
        var createWorkflowId = Guid.NewGuid();
        var deleteWorkflowId = Guid.NewGuid();

        try
        {
            const string createDiff =
                """
                --- /dev/null
                +++ b/MiniErp/Notes.txt
                @@ -0,0 +1,1 @@
                +temporary note
                """;

            const string deleteDiff =
                """
                --- a/MiniErp/Notes.txt
                +++ /dev/null
                @@ -1,1 +0,0 @@
                -temporary note
                """;

            var createPatch = PatchPackage.Create(
                new PatchPackagePayload(
                    FixtureId,
                    [
                        new PatchFileChange(
                            "MiniErp/Notes.txt",
                            PatchPackage.OperationCreate,
                            createDiff.Replace("\r\n", "\n"))
                    ],
                    EntryProject,
                    "net9.0",
                    "Add notes file"),
                createWorkflowId,
                producer: "HandWritten");
            await store.SaveAsync(createPatch);

            var applier = new Applier(store, publishRoot: publishRoot);
            var createResult = await applier.ApplyAsync(
                createWorkflowId,
                createPatch.Id,
                expectedFixtureId: FixtureId,
                fixtureRoot: FixtureRoot);

            Assert.True(createResult.ApplySucceeded, createResult.FailureMessage);
            Assert.True(createResult.BuildSucceeded, createResult.BuildOutput);
            Assert.True(File.Exists(Path.Combine(createResult.PublishDirectory, "MiniErp", "Notes.txt")));

            CopyDirectory(FixtureRoot, fixtureWithNote);
            Directory.CreateDirectory(Path.Combine(fixtureWithNote, "MiniErp"));
            await File.WriteAllTextAsync(
                Path.Combine(fixtureWithNote, "MiniErp", "Notes.txt"),
                "temporary note\n");

            var deletePatch = PatchPackage.Create(
                new PatchPackagePayload(
                    FixtureId,
                    [
                        new PatchFileChange(
                            "MiniErp/Notes.txt",
                            PatchPackage.OperationDelete,
                            deleteDiff.Replace("\r\n", "\n"))
                    ],
                    EntryProject,
                    "net9.0",
                    "Remove notes file"),
                deleteWorkflowId,
                producer: "HandWritten");
            await store.SaveAsync(deletePatch);

            var deleteResult = await applier.ApplyAsync(
                deleteWorkflowId,
                deletePatch.Id,
                expectedFixtureId: FixtureId,
                fixtureRoot: fixtureWithNote);

            Assert.True(deleteResult.ApplySucceeded, deleteResult.FailureMessage);
            Assert.True(deleteResult.BuildSucceeded, deleteResult.BuildOutput);
            Assert.False(File.Exists(Path.Combine(deleteResult.PublishDirectory, "MiniErp", "Notes.txt")));
        }
        finally
        {
            Cleanup(artifactRoot);
            Cleanup(publishRoot);
            Cleanup(fixtureWithNote);
        }
    }

    [Fact]
    public async Task OpenAPI_validation_fails_for_invalid_yaml()
    {
        var artifactRoot = CreateTempDir("artifacts");
        var publishRoot = CreateTempDir("publish");
        var store = new FileArtifactStore(artifactRoot);
        var workflowId = Guid.NewGuid();

        try
        {
            const string breakOpenApiDiff =
                """
                --- a/openapi.yaml
                +++ b/openapi.yaml
                @@ -1,5 +1,3 @@
                -openapi: 3.0.3
                -info:
                -  title: Mini ERP API
                -  version: 0.1.0
                -  description: Near-empty mini-ERP baseline for Athlon rest-api-v1 (Spike_06).
                +this is not valid yaml: [
                +  unclosed bracket
                +broken: : extra colon
                """;

            var patch = PatchPackage.Create(
                new PatchPackagePayload(
                    FixtureId,
                    [
                        new PatchFileChange(
                            "openapi.yaml",
                            PatchPackage.OperationModify,
                            breakOpenApiDiff.Replace("\r\n", "\n"))
                    ],
                    EntryProject,
                    "net9.0",
                    "Break OpenAPI file"),
                workflowId,
                producer: "HandWritten");
            await store.SaveAsync(patch);

            var applier = new Applier(store, publishRoot: publishRoot);
            var result = await applier.ApplyAsync(
                workflowId,
                patch.Id,
                expectedFixtureId: FixtureId,
                fixtureRoot: FixtureRoot);

            Assert.True(result.ApplySucceeded);
            Assert.True(result.BuildSucceeded);
            Assert.False(result.OpenapiConsistencySucceeded);
            Assert.False(result.Succeeded);
            Assert.Contains("OpenAPI", result.FailureMessage, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Cleanup(artifactRoot);
            Cleanup(publishRoot);
        }
    }

    [Fact]
    public async Task Contract_tests_fail_when_endpoint_breaks()
    {
        var artifactRoot = CreateTempDir("artifacts");
        var publishRoot = CreateTempDir("publish");
        var store = new FileArtifactStore(artifactRoot);
        var workflowId = Guid.NewGuid();

        try
        {
            const string breakHealthEndpointDiff =
                """
                --- a/MiniErp/Program.cs
                +++ b/MiniErp/Program.cs
                @@ -1,8 +1,8 @@
                 var builder = WebApplication.CreateBuilder(args);
                 var app = builder.Build();
                 
                -app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
                +app.MapGet("/health", () => Results.Ok(new { status = "broken" }));
                 
                 app.Run();
                 
                 public partial class Program;
                """;

            var patch = PatchPackage.Create(
                new PatchPackagePayload(
                    FixtureId,
                    [
                        new PatchFileChange(
                            "MiniErp/Program.cs",
                            PatchPackage.OperationModify,
                            breakHealthEndpointDiff.Replace("\r\n", "\n"))
                    ],
                    EntryProject,
                    "net9.0",
                    "Break health endpoint response"),
                workflowId,
                producer: "HandWritten");
            await store.SaveAsync(patch);

            var applier = new Applier(store, publishRoot: publishRoot);
            var result = await applier.ApplyAsync(
                workflowId,
                patch.Id,
                expectedFixtureId: FixtureId,
                fixtureRoot: FixtureRoot);

            Assert.True(result.ApplySucceeded);
            Assert.True(result.BuildSucceeded);
            Assert.True(result.OpenapiConsistencySucceeded);
            Assert.False(result.ContractTestsSucceeded);
            Assert.False(result.Succeeded);
            Assert.Contains("Contract tests failed", result.FailureMessage, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Cleanup(artifactRoot);
            Cleanup(publishRoot);
        }
    }

    private static void CopyDirectory(string sourceDir, string destDir)
    {
        foreach (var path in Directory.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            if (path.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar) ||
                path.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar))
            {
                continue;
            }

            var relative = Path.GetRelativePath(sourceDir, path);
            var dest = Path.Combine(destDir, relative);
            var destParent = Path.GetDirectoryName(dest);
            if (!string.IsNullOrEmpty(destParent))
            {
                Directory.CreateDirectory(destParent);
            }

            File.Copy(path, dest);
        }
    }

    private static async Task<Artifact> SeedValidPatchAsync(IArtifactStore store, Guid workflowId)
    {
        var patch = PatchPackage.Create(
            new PatchPackagePayload(
                FixtureId,
                [
                    new PatchFileChange(
                        "MiniErp/Program.cs",
                        PatchPackage.OperationModify,
                        AddCommentDiff.Replace("\r\n", "\n"))
                ],
                EntryProject,
                "net9.0",
                "Add comment"),
            workflowId,
            producer: "HandWritten");
        await store.SaveAsync(patch);
        return patch;
    }

    private static string NoOpDiff()
    {
        var fixtureContent = File.ReadAllText(Path.Combine(FixtureRoot, "MiniErp", "Program.cs"));
        var lines = fixtureContent.Replace("\r\n", "\n").TrimEnd('\n').Split('\n');
        var contextLines = string.Join("\n", lines.Select(l => " " + l));
        return $"""
            --- a/MiniErp/Program.cs
            +++ b/MiniErp/Program.cs
            @@ -1,{lines.Length} +1,{lines.Length} @@
            {contextLines}
            """.Replace("\r\n", "\n");
    }

    private static string CreateTempDir(string label) =>
        Path.Combine(Path.GetTempPath(), "athlon-spike-tests", label, Guid.NewGuid().ToString("D"));

    private static void Cleanup(string root)
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
