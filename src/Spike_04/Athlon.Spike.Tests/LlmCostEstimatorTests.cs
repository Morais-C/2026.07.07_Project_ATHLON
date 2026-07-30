using Athlon.Spike.Llm;

namespace Athlon.Spike.Tests;

public class LlmCostEstimatorTests
{
    [Fact]
    public void Estimates_cost_for_known_model()
    {
        var cost = LlmCostEstimator.Estimate("anthropic/claude-3.5-sonnet", promptTokens: 1_000_000, completionTokens: 0);
        Assert.Equal(3.0m, cost);
    }
}
