using Athlon.Spike.Artifacts;
using Athlon.Spike.Contracts;
using Athlon.Spike.Workflow;

namespace Athlon.Spike.Tests;

public class CodeContextBuilderTests
{
    [Fact]
    public async Task Publishes_CodeContext_from_mini_erp_v1_fixture()
    {
        var pack = MiniErpTestFixtures.Pack;
        var root = CreateTempArtifactRoot();
        var store = new FileArtifactStore(root);
        var workflowId = Guid.NewGuid();
        var builder = CodeContextBuilder.FromArchetypePack(store, pack);

        try
        {
            var artifact = await builder.BuildAndPublishAsync(
                workflowId,
                fixtureId: pack.Baseline.FixtureId,
                fixtureRoot: pack.Baseline.FixtureRoot,
                entryProject: MiniErpTestFixtures.EntryProject);

            Assert.Equal(ArtifactTypes.CodeContext, artifact.Type);
            Assert.Equal(CodeContextBuilder.ProducerName, artifact.Producer);

            var payload = CodeContext.Parse(artifact);
            Assert.Equal(pack.Baseline.FixtureId, payload.FixtureId);
            Assert.Equal(MiniErpTestFixtures.EntryProject, payload.EntryProject);
            Assert.Equal("net9.0", payload.TargetFramework);
            Assert.Equal(5, payload.Files.Count);
            Assert.Contains(payload.Files, f => f.Path == MiniErpTestFixtures.EntryProject);
            Assert.Contains(payload.Files, f => f.Path == MiniErpTestFixtures.ProgramPath);
            Assert.Contains(payload.Files, f => f.Path == MiniErpTestFixtures.OpenApiPath);
            Assert.True(payload.TotalChars > 0);
            Assert.Equal(pack.CodeContext.MaxFilesAllowed, payload.MaxFilesAllowed);
            Assert.Equal(pack.CodeContext.MaxCharsAllowed, payload.MaxCharsAllowed);

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
        // mini-erp-v1 loads 4 files with default extensions — cap at 3 to force abort
        var builder = new CodeContextBuilder(store, maxFilesAllowed: 3);

        try
        {
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                builder.BuildAndPublishAsync(
                    workflowId,
                    fixtureId: MiniErpTestFixtures.FixtureId,
                    fixtureRoot: MiniErpTestFixtures.FixtureRoot,
                    entryProject: MiniErpTestFixtures.EntryProject));

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
                    fixtureId: MiniErpTestFixtures.FixtureId,
                    fixtureRoot: MiniErpTestFixtures.FixtureRoot,
                    entryProject: MiniErpTestFixtures.EntryProject));

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
