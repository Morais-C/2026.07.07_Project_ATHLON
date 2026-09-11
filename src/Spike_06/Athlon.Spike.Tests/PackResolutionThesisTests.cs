using System.Text.Json;
using Athlon.Spike.Contracts;

namespace Athlon.Spike.Tests;

/// <summary>
/// Thesis: agent prompt/schema paths are driven by the pack manifest,
/// not by hardcoded spike-root or agent-constructor constants.
/// </summary>
public class PackResolutionThesisTests
{
    [Fact]
    public void Agent_prompt_and_schema_paths_match_rest_api_v1_manifest_fields()
    {
        var pack = SpikeTestPaths.RestApiV1Pack;
        var manifestPath = Path.Combine(pack.PackRoot, "archetype.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var agents = doc.RootElement.GetProperty("agents");

        AssertAgentPathsMatchManifest(pack.PackRoot, agents.GetProperty("Analyst"), pack.Analyst);
        AssertAgentPathsMatchManifest(pack.PackRoot, agents.GetProperty("Planner"), pack.Planner);
        AssertAgentPathsMatchManifest(pack.PackRoot, agents.GetProperty("Coder"), pack.Coder);

        Assert.StartsWith(pack.PackRoot, pack.Analyst.PromptPath, StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith(pack.PackRoot, pack.Planner.PromptPath, StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith(pack.PackRoot, pack.Coder.PromptPath, StringComparison.OrdinalIgnoreCase);
        Assert.False(Directory.Exists(Path.Combine(SpikeTestPaths.Root, "prompts")));
        Assert.False(Directory.Exists(Path.Combine(SpikeTestPaths.Root, "schemas")));
    }

    [Fact]
    public void Switching_manifest_prompt_path_changes_resolved_prompt_file()
    {
        var tempRoot = CreateTempSpikeRoot();
        try
        {
            CopyRestApiV1Pack(tempRoot);
            var packRoot = Path.Combine(tempRoot, "archetypes", "rest-api-v1");
            const string altRelative = "prompts/analyst-alt-v1.txt";
            var altAbsolute = Path.GetFullPath(Path.Combine(packRoot, altRelative));

            File.Copy(
                Path.Combine(packRoot, "prompts", "analyst-v1.txt"),
                altAbsolute,
                overwrite: true);

            var manifestPath = Path.Combine(packRoot, "archetype.json");
            var json = File.ReadAllText(manifestPath)
                .Replace("prompts/analyst-v1.txt", altRelative, StringComparison.Ordinal);
            File.WriteAllText(manifestPath, json);

            var pack = ArchetypePackLoader.Load(tempRoot, "rest-api-v1");

            Assert.Equal(altAbsolute, pack.Analyst.PromptPath, ignoreCase: true);
            Assert.EndsWith("analyst-alt-v1.txt", pack.Analyst.PromptPath, StringComparison.OrdinalIgnoreCase);
            Assert.True(File.Exists(pack.Analyst.PromptPath));

            Assert.EndsWith("planner-v1.txt", pack.Planner.PromptPath, StringComparison.OrdinalIgnoreCase);
            Assert.EndsWith("coder-v1.txt", pack.Coder.PromptPath, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("analyst-v1.txt", pack.Analyst.PromptPath, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    private static void AssertAgentPathsMatchManifest(
        string packRoot,
        JsonElement agentElement,
        ArchetypeAgentPaths resolved)
    {
        var promptRelative = agentElement.GetProperty("prompt").GetString()
            ?? throw new InvalidOperationException("agents.*.prompt missing in manifest.");
        var schemaRelative = agentElement.GetProperty("outputSchema").GetString()
            ?? throw new InvalidOperationException("agents.*.outputSchema missing in manifest.");

        var expectedPrompt = Path.GetFullPath(Path.Combine(packRoot, promptRelative));
        var expectedSchema = Path.GetFullPath(Path.Combine(packRoot, schemaRelative));

        Assert.Equal(expectedPrompt, resolved.PromptPath, ignoreCase: true);
        Assert.Equal(expectedSchema, resolved.OutputSchemaPath, ignoreCase: true);
        Assert.True(File.Exists(resolved.PromptPath));
        Assert.True(File.Exists(resolved.OutputSchemaPath));
    }

    private static string CreateTempSpikeRoot()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "athlon-spike-pack-thesis-" + Guid.NewGuid().ToString("N"));
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
