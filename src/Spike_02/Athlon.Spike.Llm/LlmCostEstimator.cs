namespace Athlon.Spike.Llm;

internal static class LlmCostEstimator
{
    // Rough OpenRouter-style rates (USD per 1M tokens). Spike estimates only.
    private static readonly Dictionary<string, (decimal PromptPerMillion, decimal CompletionPerMillion)> ModelRates =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["anthropic/claude-3.5-sonnet"] = (3.0m, 15.0m),
            ["openai/gpt-4o-mini"] = (0.15m, 0.60m),
            ["google/gemini-flash-1.5"] = (0.075m, 0.30m),
        };

    public static decimal Estimate(string model, int promptTokens, int completionTokens)
    {
        var rates = ResolveRates(model);
        var promptCost = promptTokens / 1_000_000m * rates.PromptPerMillion;
        var completionCost = completionTokens / 1_000_000m * rates.CompletionPerMillion;
        return Math.Round(promptCost + completionCost, 6);
    }

    private static (decimal PromptPerMillion, decimal CompletionPerMillion) ResolveRates(string model)
    {
        if (ModelRates.TryGetValue(model, out var exact))
        {
            return exact;
        }

        foreach (var (key, rates) in ModelRates)
        {
            if (model.Contains(key, StringComparison.OrdinalIgnoreCase))
            {
                return rates;
            }
        }

        return (1.0m, 3.0m);
    }
}
