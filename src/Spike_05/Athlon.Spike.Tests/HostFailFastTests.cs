using Athlon.Spike.Contracts;

namespace Athlon.Spike.Tests;

/// <summary>
/// Phase 4: wrong/missing archetypeId fails before the chain starts.
/// Host maps <see cref="ArchetypePackException"/> to non-zero exit (see Program.cs).
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
        var pack = SpikeTestPaths.ConsoleV1Pack;
        var packPrompts = Path.Combine("archetypes", "console-v1", "prompts");
        var packSchemas = Path.Combine("archetypes", "console-v1", "schemas");

        Assert.Contains(packPrompts, pack.Analyst.PromptPath, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(packPrompts, pack.Planner.PromptPath, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(packPrompts, pack.Coder.PromptPath, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(packSchemas, pack.Analyst.OutputSchemaPath, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(packSchemas, pack.Planner.OutputSchemaPath, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(packSchemas, pack.Coder.OutputSchemaPath, StringComparison.OrdinalIgnoreCase);

        Assert.False(Directory.Exists(Path.Combine(SpikeTestPaths.Root, "prompts")));
        Assert.False(Directory.Exists(Path.Combine(SpikeTestPaths.Root, "schemas")));
    }
}
