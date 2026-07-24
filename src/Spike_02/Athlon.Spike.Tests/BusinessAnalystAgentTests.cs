using Athlon.Spike.Agents;
using Athlon.Spike.Artifacts;
using Athlon.Spike.Contracts;
using Athlon.Spike.Llm;

namespace Athlon.Spike.Tests;

public class BusinessAnalystAgentTests
{
    private const string ValidStructuredRequirementJson = """
        {
          "title": "Employee Daily Meal Allowance",
          "actors": ["Employee", "Payroll", "Manager"],
          "goal": "Provide a fixed daily meal allowance on working days",
          "acceptanceCriteriaDraft": [
            "Eligible employees receive a fixed daily allowance on working days",
            "Payroll shows the allowance as a separate line item"
          ],
          "constraints": ["On-site working days only", "Configurable rate"],
          "priority": "Medium"
        }
        """;

    [Fact]
    public async Task Publishes_valid_artifact_from_mock_llm_response()
    {
        var root = CreateTempArtifactRoot();
        var store = new FileArtifactStore(root);
        var workflowId = Guid.NewGuid();

        try
        {
            var input = BusinessRequirement.FromText("As an employee I want meal allowance", workflowId);
            await store.SaveAsync(input);

            var agent = CreateAgent(store, new MockLLMProvider(new MockResponse(Content: ValidStructuredRequirementJson)));
            var result = await agent.ExecuteAsync(new AgentExecutionContext(workflowId, input.Id));

            Assert.Equal(ArtifactTypes.StructuredRequirement, result.OutputArtifact.Type);
            Assert.Equal(AgentName, result.OutputArtifact.Producer);
            Assert.Equal(
                "Employee Daily Meal Allowance",
                StructuredRequirement.Parse(result.OutputArtifact).Title);
            Assert.True(result.Telemetry.TotalTokens > 0);

            var loaded = await store.LoadAsync(result.OutputArtifact.Id);
            Assert.NotNull(loaded);
        }
        finally
        {
            Cleanup(root);
        }
    }

    [Fact]
    public async Task Retries_once_on_invalid_json_then_publishes()
    {
        var root = CreateTempArtifactRoot();
        var store = new FileArtifactStore(root);
        var workflowId = Guid.NewGuid();

        try
        {
            var input = BusinessRequirement.FromText("Meal allowance requirement", workflowId);
            await store.SaveAsync(input);

            var agent = CreateAgent(store, new MockLLMProvider(
                new MockResponse(Content: """{ "text": "echo only — not structured" }"""),
                new MockResponse(Content: ValidStructuredRequirementJson)));

            var result = await agent.ExecuteAsync(new AgentExecutionContext(workflowId, input.Id));

            Assert.Equal(ArtifactTypes.StructuredRequirement, result.OutputArtifact.Type);
            Assert.Equal(200, result.Telemetry.PromptTokens);
            Assert.Equal(100, result.Telemetry.CompletionTokens);
        }
        finally
        {
            Cleanup(root);
        }
    }

    [Fact]
    public async Task Fails_after_retry_without_publishing_output_artifact()
    {
        var root = CreateTempArtifactRoot();
        var store = new FileArtifactStore(root);
        var workflowId = Guid.NewGuid();

        try
        {
            var input = BusinessRequirement.FromText("Meal allowance requirement", workflowId);
            await store.SaveAsync(input);

            var agent = CreateAgent(store, new MockLLMProvider(
                new MockResponse(Content: "still not json"),
                new MockResponse(Content: """{ "title": "missing required fields" }""")));

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => agent.ExecuteAsync(new AgentExecutionContext(workflowId, input.Id)));

            Assert.Contains("after one retry", exception.Message, StringComparison.OrdinalIgnoreCase);

            // Only the input artifact should exist — no StructuredRequirement published
            var files = Directory.GetFiles(root, "*.json", SearchOption.AllDirectories);
            Assert.Single(files);
        }
        finally
        {
            Cleanup(root);
        }
    }

    private static BusinessAnalystAgent CreateAgent(FileArtifactStore store, MockLLMProvider llm) =>
        new(
            llm,
            store,
            SpikeTestPaths.BaPromptTemplate,
            SpikeTestPaths.StructuredRequirementSchema);

    private static string CreateTempArtifactRoot() =>
        Path.Combine(Path.GetTempPath(), "athlon-spike-tests", Guid.NewGuid().ToString("D"));

    private static void Cleanup(string root)
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private const string AgentName = BusinessAnalystAgent.AgentName;
}
