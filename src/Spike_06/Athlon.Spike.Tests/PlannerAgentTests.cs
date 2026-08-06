using Athlon.Spike.Agents;
using Athlon.Spike.Artifacts;
using Athlon.Spike.Contracts;
using Athlon.Spike.Llm;
using Athlon.Spike.Workflow;

namespace Athlon.Spike.Tests;

public class PlannerAgentTests
{
    private const string ValidImplementationPlanJson = """
        {
          "title": "Uppercase echo",
          "summary": "Echo typed line in UPPERCASE",
          "tasks": [
            {
              "id": "T1",
              "description": "Change Program.cs to uppercase the input before printing",
              "estimate": "30m"
            }
          ],
          "acceptanceCriteria": [
            "Prints the input transformed to UPPERCASE"
          ],
          "technicalNotes": "Minimal edit to Echo/Program.cs",
          "intendedPaths": ["Echo/Program.cs"]
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
            var bundle = await SeedChangeBundleAsync(store, workflowId);

            var agent = CreateAgent(store, new MockLLMProvider(new MockResponse(Content: ValidImplementationPlanJson)));
            var result = await agent.ExecuteAsync(new AgentExecutionContext(workflowId, bundle.Id));

            Assert.Equal(ArtifactTypes.ImplementationPlan, result.OutputArtifact.Type);
            Assert.Equal(AgentName, result.OutputArtifact.Producer);

            var plan = ImplementationPlan.Parse(result.OutputArtifact);
            Assert.Equal("Uppercase echo", plan.Title);
            Assert.Equal(["Echo/Program.cs"], plan.IntendedPaths);
            Assert.Equal("echo-v1", plan.FixtureId);
            Assert.Equal("Echo/Echo.csproj", plan.EntryProject);
            Assert.Equal("net9.0", plan.TargetFramework);
            Assert.True(Guid.TryParse(plan.CodeContextArtifactId, out _));
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
            var bundle = await SeedChangeBundleAsync(store, workflowId);

            var agent = CreateAgent(store, new MockLLMProvider(
                new MockResponse(Content: "not valid json"),
                new MockResponse(Content: ValidImplementationPlanJson)));

            var result = await agent.ExecuteAsync(new AgentExecutionContext(workflowId, bundle.Id));

            Assert.Equal(ArtifactTypes.ImplementationPlan, result.OutputArtifact.Type);
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
            var bundle = await SeedChangeBundleAsync(store, workflowId);
            var seedCount = Directory.GetFiles(root, "*.json", SearchOption.AllDirectories).Length;

            var agent = CreateAgent(store, new MockLLMProvider(
                new MockResponse(Content: "still not json"),
                new MockResponse(Content: "{ \"title\": \"missing required fields\" }")));

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => agent.ExecuteAsync(new AgentExecutionContext(workflowId, bundle.Id)));

            Assert.Contains("after one retry", exception.Message, StringComparison.OrdinalIgnoreCase);

            var files = Directory.GetFiles(root, "*.json", SearchOption.AllDirectories);
            Assert.Equal(seedCount, files.Length);
        }
        finally
        {
            Cleanup(root);
        }
    }

    [Fact]
    public async Task Rejects_StructuredChange_input_type_without_ChangeBundle()
    {
        var root = CreateTempArtifactRoot();
        var store = new FileArtifactStore(root);
        var workflowId = Guid.NewGuid();

        try
        {
            var wrong = StructuredChange.Create(
                new StructuredChangePayload(
                    Kind: ChangeRequest.KindFeature,
                    Title: "Uppercase echo",
                    Summary: "Echo UPPERCASE",
                    AcceptanceCriteria: ["Prints UPPERCASE"],
                    Constraints: ["Console only"],
                    Priority: "Medium"),
                workflowId,
                producer: AnalystAgent.AgentName);
            await store.SaveAsync(wrong);

            var agent = CreateAgent(store, new MockLLMProvider(new MockResponse(Content: ValidImplementationPlanJson)));

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => agent.ExecuteAsync(new AgentExecutionContext(workflowId, wrong.Id)));

            Assert.Contains(ArtifactTypes.ChangeBundle, exception.Message);
            Assert.Contains(ArtifactTypes.StructuredChange, exception.Message);
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
              "title": "Uppercase echo",
              "summary": "Echo UPPERCASE",
              "tasks": [
                { "id": "T1", "description": "Edit Program.cs", "estimate": "30m" }
              ],
              "acceptanceCriteria": ["Prints UPPERCASE"],
              "technicalNotes": "Minimal",
              "intendedPaths": ["Echo/Program.cs"]
            }
            ```
            """;

        var validator = new JsonSchemaValidator(SpikeTestPaths.ImplementationPlanSchema);
        var outcome = validator.Validate(fenced);

        Assert.True(outcome.IsValid);
    }

    private static async Task<Artifact> SeedChangeBundleAsync(FileArtifactStore store, Guid workflowId)
    {
        var structured = StructuredChange.Create(
            new StructuredChangePayload(
                Kind: ChangeRequest.KindFeature,
                Title: "Uppercase echo",
                Summary: "Echo typed line in UPPERCASE",
                AcceptanceCriteria: ["Prints the input transformed to UPPERCASE"],
                Constraints: ["Single .NET 9 console"],
                Priority: "Medium",
                SuspectedPaths: ["Echo/Program.cs"]),
            workflowId,
            producer: AnalystAgent.AgentName);
        await store.SaveAsync(structured);

        var builder = new CodeContextBuilder(store);
        var codeContext = await builder.BuildAndPublishAsync(
            workflowId,
            fixtureId: "echo-v1",
            fixtureRoot: SpikeTestPaths.EchoV1FixtureRoot,
            entryProject: "Echo/Echo.csproj");

        var runner = new WorkflowRunner(store);
        return await runner.SaveChangeBundleAsync(workflowId, structured.Id, codeContext.Id, CancellationToken.None);
    }

    private static PlannerAgent CreateAgent(FileArtifactStore store, MockLLMProvider llm) =>
        new(
            llm,
            store,
            SpikeTestPaths.PlannerPromptTemplate,
            SpikeTestPaths.ImplementationPlanSchema);

    private static string CreateTempArtifactRoot() =>
        Path.Combine(Path.GetTempPath(), "athlon-spike-tests", Guid.NewGuid().ToString("D"));

    private static void Cleanup(string root)
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private const string AgentName = PlannerAgent.AgentName;
}
