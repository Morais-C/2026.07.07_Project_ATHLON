using Athlon.Spike.Contracts;

namespace Athlon.Spike.Tests;

public class ArchetypePackLoaderTests
{
    [Fact]
    public void Load_rest_api_v1_pack_from_spike_root()
    {
        var pack = ArchetypePackLoader.Load(SpikeTestPaths.Root, "rest-api-v1");

        Assert.Equal("rest-api-v1", pack.ArchetypeId);
        Assert.Equal("1.0.0", pack.Version);
        Assert.Equal("training", pack.Status);
        Assert.Equal("REST API v1", pack.DisplayName);
        Assert.True(Directory.Exists(pack.PackRoot));
        Assert.Equal(SpikeTestPaths.RestApiV1PackRoot, pack.PackRoot);

        Assert.True(File.Exists(pack.Paths.Bounds));
        Assert.True(File.Exists(pack.Paths.CodeContext));
        Assert.True(File.Exists(pack.Paths.ChangeRequest));
        Assert.True(Directory.Exists(pack.Paths.SchemasDirectory));
        Assert.True(Directory.Exists(pack.Paths.PromptsDirectory));
        Assert.True(File.Exists(pack.Paths.PatchApply));
        Assert.True(File.Exists(pack.Paths.ProofPipeline));
        Assert.True(File.Exists(pack.Paths.Demos));

        Assert.Equal("mini-erp-v1", pack.Baseline.FixtureId);
        Assert.Equal(SpikeTestPaths.MiniErpV1FixtureRoot, pack.Baseline.FixtureRoot);

        Assert.Equal(16, pack.CodeContext.MaxFilesAllowed);
        Assert.Equal(64000, pack.CodeContext.MaxCharsAllowed);
        Assert.Contains(".cs", pack.CodeContext.IncludeExtensions);
        Assert.Contains(".csproj", pack.CodeContext.IncludeExtensions);
        Assert.Contains(".yaml", pack.CodeContext.IncludeExtensions);

        Assert.Equal(["feature", "bugfix"], pack.ChangeRequestKinds);
        Assert.Equal(6, pack.ArtifactChain.Count);

        Assert.True(File.Exists(pack.Analyst.PromptPath));
        Assert.True(File.Exists(pack.Analyst.OutputSchemaPath));
        Assert.True(File.Exists(pack.Planner.PromptPath));
        Assert.True(File.Exists(pack.Planner.OutputSchemaPath));
        Assert.True(File.Exists(pack.Coder.PromptPath));
        Assert.True(File.Exists(pack.Coder.OutputSchemaPath));

        Assert.EndsWith("analyst-v1.txt", pack.Analyst.PromptPath);
        Assert.EndsWith("structured-change.schema.json", pack.Analyst.OutputSchemaPath);
    }

    [Fact]
    public void Missing_pack_directory_throws()
    {
        var ex = Assert.Throws<ArchetypePackException>(() =>
            ArchetypePackLoader.Load(SpikeTestPaths.Root, "does-not-exist"));

        Assert.Contains("pack directory not found", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Id_mismatch_in_manifest_throws()
    {
        var tempRoot = CreateTempSpikeRoot();
        try
        {
            CopyRestApiV1Pack(tempRoot);
            var manifestPath = Path.Combine(tempRoot, "archetypes", "rest-api-v1", "archetype.json");
            var json = File.ReadAllText(manifestPath).Replace("\"rest-api-v1\"", "\"other-id\"");
            File.WriteAllText(manifestPath, json);

            var ex = Assert.Throws<ArchetypePackException>(() =>
                ArchetypePackLoader.Load(tempRoot, "rest-api-v1"));

            Assert.Contains("id mismatch", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public void Missing_required_path_throws()
    {
        var tempRoot = CreateTempSpikeRoot();
        try
        {
            CopyRestApiV1Pack(tempRoot);
            File.Delete(Path.Combine(tempRoot, "archetypes", "rest-api-v1", "bounds.md"));

            var ex = Assert.Throws<ArchetypePackException>(() =>
                ArchetypePackLoader.Load(tempRoot, "rest-api-v1"));

            Assert.Contains("bounds", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public void Invalid_manifest_json_throws()
    {
        var tempRoot = CreateTempSpikeRoot();
        try
        {
            CopyRestApiV1Pack(tempRoot);
            var manifestPath = Path.Combine(tempRoot, "archetypes", "rest-api-v1", "archetype.json");
            File.WriteAllText(manifestPath, "{ not valid json");

            var ex = Assert.Throws<ArchetypePackException>(() =>
                ArchetypePackLoader.Load(tempRoot, "rest-api-v1"));

            Assert.Contains("invalid", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public void Missing_baseline_fixture_throws()
    {
        var tempRoot = CreateTempSpikeRoot();
        try
        {
            CopyRestApiV1Pack(tempRoot);
            Directory.Delete(Path.Combine(tempRoot, "fixtures", "mini-erp-v1"), recursive: true);

            var ex = Assert.Throws<ArchetypePackException>(() =>
                ArchetypePackLoader.Load(tempRoot, "rest-api-v1"));

            Assert.Contains("fixture", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    private static string CreateTempSpikeRoot()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "athlon-spike-pack-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        return tempRoot;
    }

    private static void CopyRestApiV1Pack(string tempRoot)
    {
        CopyDirectory(SpikeTestPaths.RestApiV1PackRoot, Path.Combine(tempRoot, "archetypes", "rest-api-v1"));
        CopyDirectory(SpikeTestPaths.MiniErpV1FixtureRoot, Path.Combine(tempRoot, "fixtures", "mini-erp-v1"));
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);

        foreach (var file in Directory.GetFiles(source))
        {
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), overwrite: true);
        }

        foreach (var directory in Directory.GetDirectories(source))
        {
            CopyDirectory(directory, Path.Combine(destination, Path.GetFileName(directory)));
        }
    }
}
