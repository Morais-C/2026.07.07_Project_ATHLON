namespace Athlon.Spike.Contracts;

public record LlmCompletionResult(
    string Content,
    string Model,
    int PromptTokens,
    int CompletionTokens,
    int TotalTokens,
    TimeSpan Duration,
    decimal? EstimatedCostUsd = null);
