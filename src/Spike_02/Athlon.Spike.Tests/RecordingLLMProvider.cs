using Athlon.Spike.Contracts;
using Athlon.Spike.Llm;

namespace Athlon.Spike.Tests;

/// <summary>
/// Mock LLM that records every CompleteAsync prompt (for thesis / handoff tests).
/// </summary>
internal sealed class RecordingLLMProvider : ILLMProvider
{
    private readonly MockLLMProvider _inner;

    public RecordingLLMProvider(params MockResponse[] responses)
    {
        _inner = new MockLLMProvider(responses);
    }

    public List<(string SystemPrompt, string UserPrompt)> Calls { get; } = [];

    public async Task<LlmCompletionResult> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        Calls.Add((systemPrompt, userPrompt));
        return await _inner.CompleteAsync(systemPrompt, userPrompt, cancellationToken)
            .ConfigureAwait(false);
    }
}
