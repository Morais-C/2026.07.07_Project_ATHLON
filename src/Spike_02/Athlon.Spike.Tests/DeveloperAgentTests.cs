using Athlon.Spike.Agents;
using Athlon.Spike.Artifacts;
using Athlon.Spike.Contracts;
using Athlon.Spike.Llm;

namespace Athlon.Spike.Tests;

public class DeveloperAgentTests
{
    private const string ValidImplementationJson = """
        {
          "title": "Meal Allowance",
          "summary": "Daily meal subsidy for employees",
          "tasks": [
            {
              "id": "T1",
              "description": "Add allowance field to payroll",
              "estimate": "2d"
            }
          ],
          "acceptanceCriteria": [
            "Employees receive a daily meal allowance"
          ],
          "technicalNotes": "Extend payroll module"
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

            var agent = CreateAgent(store, new MockLLMProvider(new MockResponse(Content: ValidImplementationJson)));
            var result = await agent.ExecuteAsync(new AgentExecutionContext(workflowId, input.Id));

            Assert.Equal(ArtifactTypes.Implementation, result.OutputArtifact.Type);
            Assert.Equal(AgentName, result.OutputArtifact.Producer);
            Assert.Equal("Meal Allowance", ImplementationArtifact.Parse(result.OutputArtifact).Title);
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
                new MockResponse(Content: "not valid json"),
                new MockResponse(Content: ValidImplementationJson)));

            var result = await agent.ExecuteAsync(new AgentExecutionContext(workflowId, input.Id));

            Assert.Equal(ArtifactTypes.Implementation, result.OutputArtifact.Type);
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
                new MockResponse(Content: "{ \"title\": \"missing required fields\" }")));

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => agent.ExecuteAsync(new AgentExecutionContext(workflowId, input.Id)));

            Assert.Contains("after one retry", exception.Message, StringComparison.OrdinalIgnoreCase);

            var files = Directory.GetFiles(root, "*.json", SearchOption.AllDirectories);
            Assert.Single(files);
        }
        finally
        {
            Cleanup(root);
        }
    }

    [Fact]
    public void JsonSchemaValidator_strips_json_fences()
    {
        var fenced = """
            ```json
            {
              "title": "Meal Allowance",
              "summary": "Daily meal subsidy for employees",
              "tasks": [
                { "id": "T1", "description": "Add field", "estimate": "2d" }
              ],
              "acceptanceCriteria": ["Employees receive allowance"],
              "technicalNotes": "Payroll"
            }
            ```
            """;

        var validator = new JsonSchemaValidator(SpikeTestPaths.ImplementationSchema);
        var outcome = validator.Validate(fenced);

        Assert.True(outcome.IsValid);
    }

    private static DeveloperAgent CreateAgent(FileArtifactStore store, MockLLMProvider llm) =>
        new(
            llm,
            store,
            SpikeTestPaths.PromptTemplate,
            SpikeTestPaths.ImplementationSchema);

    private static string CreateTempArtifactRoot() =>
        Path.Combine(Path.GetTempPath(), "athlon-spike-tests", Guid.NewGuid().ToString("D"));

    private static void Cleanup(string root)
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private const string AgentName = DeveloperAgent.AgentName;
}
