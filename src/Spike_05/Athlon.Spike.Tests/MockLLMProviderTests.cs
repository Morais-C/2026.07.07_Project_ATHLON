using Athlon.Spike.Llm;

namespace Athlon.Spike.Tests;

public class MockLLMProviderTests
{
    [Fact]
    public async Task Returns_fixed_usage_from_queued_response()
    {
        var provider = new MockLLMProvider(new MockResponse(
            Content: "{\"title\":\"Test\"}",
            Model: "mock/test-model",
            PromptTokens: 842,
            CompletionTokens: 312,
            TotalTokens: 1154,
            EstimatedCostUsd: 0.0038m)
        {
            Duration = TimeSpan.FromSeconds(4.2)
        });

        var result = await provider.CompleteAsync("system", "user");

        Assert.Equal("{\"title\":\"Test\"}", result.Content);
        Assert.Equal("mock/test-model", result.Model);
        Assert.Equal(842, result.PromptTokens);
        Assert.Equal(312, result.CompletionTokens);
        Assert.Equal(1154, result.TotalTokens);
        Assert.Equal(0.0038m, result.EstimatedCostUsd);
    }

    [Fact]
    public async Task Dequeues_multiple_responses_for_retry_scenarios()
    {
        var provider = new MockLLMProvider(
            new MockResponse(Content: "invalid json"),
            new MockResponse(Content: "{\"title\":\"Recovered\"}"));

        var first = await provider.CompleteAsync("system", "user");
        var second = await provider.CompleteAsync("system", "user");

        Assert.Equal("invalid json", first.Content);
        Assert.Equal("{\"title\":\"Recovered\"}", second.Content);
    }
}
