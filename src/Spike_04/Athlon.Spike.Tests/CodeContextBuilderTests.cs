using Athlon.Spike.Artifacts;
using Athlon.Spike.Contracts;
using Athlon.Spike.Workflow;

namespace Athlon.Spike.Tests;

public class CodeContextBuilderTests
{
    [Fact]
    public async Task Publishes_CodeContext_from_echo_v1_fixture()
    {
        var root = CreateTempArtifactRoot();
        var store = new FileArtifactStore(root);
        var workflowId = Guid.NewGuid();
        var builder = new CodeContextBuilder(store);

        try
        {
            var artifact = await builder.BuildAndPublishAsync(
                workflowId,
                fixtureId: "echo-v1",
                fixtureRoot: SpikeTestPaths.EchoV1FixtureRoot,
                entryProject: "Echo/Echo.csproj");

            Assert.Equal(ArtifactTypes.CodeContext, artifact.Type);
            Assert.Equal(CodeContextBuilder.ProducerName, artifact.Producer);

            var payload = CodeContext.Parse(artifact);
            Assert.Equal("echo-v1", payload.FixtureId);
            Assert.Equal("Echo/Echo.csproj", payload.EntryProject);
            Assert.Equal("net9.0", payload.TargetFramework);
            Assert.Equal(2, payload.Files.Count);
            Assert.Contains(payload.Files, f => f.Path == "Echo/Echo.csproj");
            Assert.Contains(payload.Files, f => f.Path == "Echo/Program.cs");
            Assert.True(payload.TotalChars > 0);
            Assert.Equal(CodeContext.DefaultMaxFilesAllowed, payload.MaxFilesAllowed);
            Assert.Equal(CodeContext.DefaultMaxCharsAllowed, payload.MaxCharsAllowed);

            foreach (var file in payload.Files)
            {
                Assert.Equal(CodeContext.ComputeSha256(file.Content), file.ContentSha256);
            }
        }
        finally
        {
            Cleanup(root);
        }
    }

    [Fact]
    public async Task Over_file_cap_fails_without_publishing()
    {
        var root = CreateTempArtifactRoot();
        var store = new FileArtifactStore(root);
        var workflowId = Guid.NewGuid();
        // echo-v1 has 2 source files — cap at 1 to force abort
        var builder = new CodeContextBuilder(store, maxFilesAllowed: 1);

        try
        {
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                builder.BuildAndPublishAsync(
                    workflowId,
                    fixtureId: "echo-v1",
                    fixtureRoot: SpikeTestPaths.EchoV1FixtureRoot,
                    entryProject: "Echo/Echo.csproj"));

            Assert.Contains("file cap", ex.Message, StringComparison.OrdinalIgnoreCase);
            AssertNoPublishedArtifacts(root);
        }
        finally
        {
            Cleanup(root);
        }
    }

    [Fact]
    public async Task Over_char_cap_fails_without_publishing()
    {
        var root = CreateTempArtifactRoot();
        var store = new FileArtifactStore(root);
        var workflowId = Guid.NewGuid();
        var builder = new CodeContextBuilder(store, maxCharsAllowed: 10);

        try
        {
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                builder.BuildAndPublishAsync(
                    workflowId,
                    fixtureId: "echo-v1",
                    fixtureRoot: SpikeTestPaths.EchoV1FixtureRoot,
                    entryProject: "Echo/Echo.csproj"));

            Assert.Contains("char cap", ex.Message, StringComparison.OrdinalIgnoreCase);
            AssertNoPublishedArtifacts(root);
        }
        finally
        {
            Cleanup(root);
        }
    }

    private static void AssertNoPublishedArtifacts(string root)
    {
        if (!Directory.Exists(root))
        {
            return;
        }

        var files = Directory.GetFiles(root, "*.json", SearchOption.AllDirectories);
        Assert.Empty(files);
    }

    private static string CreateTempArtifactRoot() =>
        Path.Combine(Path.GetTempPath(), "athlon-spike-tests", Guid.NewGuid().ToString("D"));

    private static void Cleanup(string root)
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
