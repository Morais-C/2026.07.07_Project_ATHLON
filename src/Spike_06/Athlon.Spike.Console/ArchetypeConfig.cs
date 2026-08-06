using Athlon.Spike.Contracts;
using Microsoft.Extensions.Configuration;

namespace Athlon.Spike.ConsoleHost;

/// <summary>
/// Loads Athlon host settings (archetype id, optional spike root) from appsettings.
/// Missing/blank archetype id fails fast via <see cref="ArchetypePackException"/>.
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

        var archetypeId = ArchetypePackLoader.RequireArchetypeId(config["Athlon:ArchetypeId"]);

        var spikeRoot = config["Athlon:SpikeRoot"];
        if (string.IsNullOrWhiteSpace(spikeRoot))
        {
            spikeRoot = Directory.GetCurrentDirectory();
        }

        return (archetypeId, Path.GetFullPath(spikeRoot));
    }
}
