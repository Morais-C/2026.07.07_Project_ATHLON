using Athlon.Spike.Contracts;

namespace Athlon.Spike.Tests;

/// <summary>
/// Host fail-fast: wrong/missing archetypeId or demo id fails before the chain starts.
/// </summary>
public class HostFailFastTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Missing_or_blank_archetype_id_throws_ArchetypePackException(string? archetypeId)
    {
        var ex = Assert.Throws<ArchetypePackException>(() =>
            ArchetypePackLoader.RequireArchetypeId(archetypeId));

        Assert.Contains("Athlon:ArchetypeId", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Unknown_archetype_id_throws_ArchetypePackException()
    {
        var ex = Assert.Throws<ArchetypePackException>(() =>
            ArchetypePackLoader.Load(SpikeTestPaths.Root, "does-not-exist"));

        Assert.Contains("pack directory not found", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Blank_archetype_id_on_Load_throws_ArchetypePackException()
    {
        var ex = Assert.Throws<ArchetypePackException>(() =>
            ArchetypePackLoader.Load(SpikeTestPaths.Root, "   "));

        Assert.Contains("Athlon:ArchetypeId", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Prompt_and_schema_paths_resolve_under_pack_not_spike_root()
    {
        var pack = SpikeTestPaths.RestApiV1Pack;
        var packPrompts = Path.Combine("archetypes", "rest-api-v1", "prompts");
        var packSchemas = Path.Combine("archetypes", "rest-api-v1", "schemas");

        Assert.Contains(packPrompts, pack.Analyst.PromptPath, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(packPrompts, pack.Planner.PromptPath, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(packPrompts, pack.Coder.PromptPath, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(packSchemas, pack.Analyst.OutputSchemaPath, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(packSchemas, pack.Planner.OutputSchemaPath, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(packSchemas, pack.Coder.OutputSchemaPath, StringComparison.OrdinalIgnoreCase);

        Assert.False(Directory.Exists(Path.Combine(SpikeTestPaths.Root, "prompts")));
        Assert.False(Directory.Exists(Path.Combine(SpikeTestPaths.Root, "schemas")));
    }

    [Fact]
    public void Demo_catalog_loads_from_rest_api_v1_pack()
    {
        var pack = SpikeTestPaths.RestApiV1Pack;
        var demos = ArchetypeDemoCatalog.Load(pack.Paths.Demos);

        Assert.True(demos.Count >= 2);
        Assert.Contains(demos, d => d.Id == "add-product-resource");
        Assert.Contains(demos, d => d.Id == "add-customer-resource");
    }

    [Fact]
    public void Unknown_demo_id_throws_ArchetypePackException()
    {
        var pack = SpikeTestPaths.RestApiV1Pack;
        var demos = ArchetypeDemoCatalog.Load(pack.Paths.Demos);

        var ex = Assert.Throws<ArchetypePackException>(() =>
            ArchetypeDemoCatalog.ResolveActive(demos, "does-not-exist"));

        Assert.Contains("Unknown demo id", ex.Message, StringComparison.Ordinal);
    }
}
