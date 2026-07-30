using Athlon.Spike.Contracts;

namespace Athlon.Spike.Llm;

public sealed class MockLLMProvider : ILLMProvider
{
    private readonly Queue<MockResponse> _responses = new();

    public MockLLMProvider(params MockResponse[] responses)
    {
        foreach (var response in responses)
        {
            _responses.Enqueue(response);
        }
    }

    public void Enqueue(MockResponse response) => _responses.Enqueue(response);

    public Task<LlmCompletionResult> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        if (_responses.Count == 0)
        {
            throw new InvalidOperationException("MockLLMProvider has no queued responses.");
        }

        var mock = _responses.Dequeue();

        var result = new LlmCompletionResult(
            Content: mock.Content,
            Model: mock.Model,
            PromptTokens: mock.PromptTokens,
            CompletionTokens: mock.CompletionTokens,
            TotalTokens: mock.TotalTokens ?? mock.PromptTokens + mock.CompletionTokens,
            Duration: mock.Duration,
            EstimatedCostUsd: mock.EstimatedCostUsd);

        return Task.FromResult(result);
    }
}

public sealed record MockResponse(
    string Content,
    string Model = "mock/model",
    int PromptTokens = 100,
    int CompletionTokens = 50,
    int? TotalTokens = null,
    decimal? EstimatedCostUsd = 0.001m)
{
    public TimeSpan Duration { get; init; } = TimeSpan.FromMilliseconds(25);
}
