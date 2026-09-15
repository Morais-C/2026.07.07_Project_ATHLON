using Athlon.Spike.Contracts;
using Microsoft.Extensions.Configuration;

namespace Athlon.Spike.ConsoleHost;

/// <summary>
/// Loads Athlon host settings (archetype id, optional spike root) from appsettings.
/// Missing/blank archetype id fails fast via <see cref="ArchetypePackException"/>.
/// </summary>
internal static class ArchetypeConfig
{
    public static ArchetypeHostConfig Load()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Local.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var archetypeId = ArchetypePackLoader.RequireArchetypeId(config["Athlon:ArchetypeId"]);

        var spikeRoot = config["Athlon:SpikeRoot"];
        if (string.IsNullOrWhiteSpace(spikeRoot))
        {
            spikeRoot = Directory.GetCurrentDirectory();
        }

        var demoId = config["Athlon:DemoId"];
        if (string.IsNullOrWhiteSpace(demoId))
        {
            demoId = Environment.GetEnvironmentVariable("ATHLON_DEMO_ID");
        }

        return new ArchetypeHostConfig(
            archetypeId,
            Path.GetFullPath(spikeRoot),
            string.IsNullOrWhiteSpace(demoId) ? null : demoId.Trim());
    }
}

internal sealed record ArchetypeHostConfig(string ArchetypeId, string SpikeRoot, string? DemoId);
