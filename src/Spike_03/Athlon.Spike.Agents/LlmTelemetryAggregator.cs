using Athlon.Spike.Contracts;

namespace Athlon.Spike.Agents;

internal static class LlmTelemetryAggregator
{
    public static LlmCompletionResult Aggregate(IReadOnlyList<LlmCompletionResult> completions)
    {
        ArgumentNullException.ThrowIfNull(completions);

        if (completions.Count == 0)
        {
            throw new ArgumentException("At least one completion is required.", nameof(completions));
        }

        if (completions.Count == 1)
        {
            return completions[0];
        }

        var last = completions[^1];
        var totalDuration = TimeSpan.FromMilliseconds(
            completions.Sum(completion => completion.Duration.TotalMilliseconds));

        decimal? totalCost = null;
        if (completions.Any(completion => completion.EstimatedCostUsd.HasValue))
        {
            totalCost = completions.Sum(completion => completion.EstimatedCostUsd ?? 0m);
        }

        return new LlmCompletionResult(
            Content: last.Content,
            Model: last.Model,
            PromptTokens: completions.Sum(completion => completion.PromptTokens),
            CompletionTokens: completions.Sum(completion => completion.CompletionTokens),
            TotalTokens: completions.Sum(completion => completion.TotalTokens),
            Duration: totalDuration,
            EstimatedCostUsd: totalCost);
    }
}
