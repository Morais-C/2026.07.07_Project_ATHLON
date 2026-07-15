namespace Athlon.Spike.Contracts;

public interface ILLMProvider
{
    Task<LlmCompletionResult> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default);
}
