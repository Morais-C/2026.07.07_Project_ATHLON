using Microsoft.Extensions.Configuration;

namespace Athlon.Spike.ConsoleHost;

/// <summary>
/// Loads OpenRouter settings from appsettings.json + optional appsettings.Local.json.
/// </summary>
internal static class OpenRouterConfig
{
    public static (string ApiKey, string Model) Load()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Local.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var apiKey = config["OpenRouter:ApiKey"];
        var model = config["OpenRouter:Model"];
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(model))
        {
            throw new InvalidOperationException(
                "Set OpenRouter:ApiKey and OpenRouter:Model in appsettings.Local.json " +
                "(copy from appsettings.Local.json.example).");
        }

        return (apiKey, model);
    }
}
