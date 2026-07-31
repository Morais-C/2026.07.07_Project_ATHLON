using Microsoft.Extensions.Configuration;

namespace Athlon.Spike.ConsoleHost;

/// <summary>
/// Loads Athlon host settings (archetype id, optional spike root) from appsettings.
/// </summary>
internal static class ArchetypeConfig
{
    public static (string ArchetypeId, string SpikeRoot) Load()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Local.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var archetypeId = config["Athlon:ArchetypeId"];
        if (string.IsNullOrWhiteSpace(archetypeId))
        {
            throw new InvalidOperationException(
                "Set Athlon:ArchetypeId in appsettings.json (e.g. \"console-v1\").");
        }

        var spikeRoot = config["Athlon:SpikeRoot"];
        if (string.IsNullOrWhiteSpace(spikeRoot))
        {
            spikeRoot = Directory.GetCurrentDirectory();
        }

        return (archetypeId.Trim(), Path.GetFullPath(spikeRoot));
    }
}
