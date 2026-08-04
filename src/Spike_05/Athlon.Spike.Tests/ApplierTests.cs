using Athlon.Spike.Artifacts;
using Athlon.Spike.Contracts;
using Athlon.Spike.Workflow;

namespace Athlon.Spike.Tests;

/// <summary>
/// Hand-written PatchPackage on console-v1 pack baseline (echo-v1) → Applier → build.
/// </summary>
public class ApplierTests
{
    private static string FixtureId => SpikeTestPaths.ConsoleV1Pack.Baseline.FixtureId;

    private static string FixtureRoot => SpikeTestPaths.ConsoleV1Pack.Baseline.FixtureRoot;

    private const string EntryProject = "Echo/Echo.csproj";

    /// <summary>
    /// Exact unified diff against checked-in fixtures/echo-v1/Echo/Program.cs.
    /// </summary>
    private const string UppercaseEchoDiff =
        """
        --- a/Echo/Program.cs
        +++ b/Echo/Program.cs
        @@ -1,4 +1,4 @@
         // Spike_04 fixture baseline: read a line, echo it back (read → process → print).
         Console.Write("Enter text: ");
         var input = Console.ReadLine() ?? string.Empty;
        -Console.WriteLine(input);
        +Console.WriteLine(input.ToUpperInvariant());
        """;

    private const string BadContextDiff =
        """
        --- a/Echo/Program.cs
        +++ b/Echo/Program.cs
        @@ -1,4 +1,4 @@
         // wrong context that does not match the fixture
         Console.Write("Enter text: ");
         var input = Console.ReadLine() ?? string.Empty;
        -Console.WriteLine(input);
        +Console.WriteLine(input.ToUpperInvariant());
        """;

    [Fact]
    public async Task Handwritten_PatchPackage_applies_to_fixture_and_builds()
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
                            "Echo/Program.cs",
                            PatchPackage.OperationModify,
                            UppercaseEchoDiff.Replace("\r\n", "\n"))
                    ],
                    EntryProject,
                    "net9.0",
                    "Uppercase echoed line"),
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
            Assert.True(result.Succeeded, result.FailureMessage);
            Assert.True(File.Exists(result.ManifestPath));
            Assert.True(File.Exists(Path.Combine(result.PublishDirectory, "Echo", "Program.cs")));
            Assert.True(File.Exists(Path.Combine(result.PublishDirectory, "Echo", "Echo.csproj")));

            var publishedProgram = await File.ReadAllTextAsync(
                Path.Combine(result.PublishDirectory, "Echo", "Program.cs"));
            Assert.Contains("ToUpperInvariant()", publishedProgram, StringComparison.Ordinal);
            Assert.DoesNotContain(
                "Console.WriteLine(input);",
                publishedProgram.Replace("\r\n", "\n"),
                StringComparison.Ordinal);

            // Fixture baseline must remain untouched
            var fixtureProgram = await File.ReadAllTextAsync(
                Path.Combine(FixtureRoot, "Echo", "Program.cs"));
            Assert.DoesNotContain("ToUpperInvariant()", fixtureProgram, StringComparison.Ordinal);

            var manifest = await File.ReadAllTextAsync(result.ManifestPath);
            Assert.Contains("\"buildSucceeded\": true", manifest, StringComparison.Ordinal);
            Assert.Contains("deferred-to-tester-agent", manifest, StringComparison.Ordinal);
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
                            "Echo/Program.cs",
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
                +++ b/Echo/Notes.txt
                @@ -0,0 +1,1 @@
                +temporary note
                """;

            const string deleteDiff =
                """
                --- a/Echo/Notes.txt
                +++ /dev/null
                @@ -1,1 +0,0 @@
                -temporary note
                """;

            var createPatch = PatchPackage.Create(
                new PatchPackagePayload(
                    FixtureId,
                    [
                        new PatchFileChange(
                            "Echo/Notes.txt",
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

            Assert.True(createResult.Succeeded, createResult.FailureMessage ?? createResult.BuildOutput);
            Assert.True(File.Exists(Path.Combine(createResult.PublishDirectory, "Echo", "Notes.txt")));

            // Temp fixture = pack baseline + Notes.txt (no prior apply-manifest).
            CopyDirectory(FixtureRoot, fixtureWithNote);
            Directory.CreateDirectory(Path.Combine(fixtureWithNote, "Echo"));
            await File.WriteAllTextAsync(
                Path.Combine(fixtureWithNote, "Echo", "Notes.txt"),
                "temporary note\n");

            var deletePatch = PatchPackage.Create(
                new PatchPackagePayload(
                    FixtureId,
                    [
                        new PatchFileChange(
                            "Echo/Notes.txt",
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
            Assert.False(File.Exists(Path.Combine(deleteResult.PublishDirectory, "Echo", "Notes.txt")));
        }
        finally
        {
            Cleanup(artifactRoot);
            Cleanup(publishRoot);
            Cleanup(fixtureWithNote);
        }
    }

    private static void CopyDirectory(string sourceDir, string destDir)
    {
        foreach (var path in Directory.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
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
                        "Echo/Program.cs",
                        PatchPackage.OperationModify,
                        UppercaseEchoDiff.Replace("\r\n", "\n"))
                ],
                EntryProject,
                "net9.0",
                "Uppercase echoed line"),
            workflowId,
            producer: "HandWritten");
        await store.SaveAsync(patch);
        return patch;
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
