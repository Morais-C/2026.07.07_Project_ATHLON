using Athlon.Spike.Agents;
using Athlon.Spike.Artifacts;
using Athlon.Spike.Contracts;
using Athlon.Spike.Llm;
using Athlon.Spike.Workflow;

namespace Athlon.Spike.Tests;

public class PlannerAgentTests
{
    [Fact]
    public async Task Publishes_valid_artifact_from_mock_llm_response()
    {
        var root = CreateTempArtifactRoot();
        var store = new FileArtifactStore(root);
        var workflowId = Guid.NewGuid();

        try
        {
            var bundle = await SeedChangeBundleAsync(store, workflowId);

            var agent = CreateAgent(store, new MockLLMProvider(new MockResponse(Content: MiniErpTestFixtures.ValidImplementationPlanJson)));
            var result = await agent.ExecuteAsync(new AgentExecutionContext(workflowId, bundle.Id));

            Assert.Equal(ArtifactTypes.ImplementationPlan, result.OutputArtifact.Type);
            Assert.Equal(AgentName, result.OutputArtifact.Producer);

            var plan = ImplementationPlan.Parse(result.OutputArtifact);
            Assert.Equal("Add health comment", plan.Title);
            Assert.Equal([MiniErpTestFixtures.ProgramPath], plan.IntendedPaths);
            Assert.Equal(MiniErpTestFixtures.FixtureId, plan.FixtureId);
            Assert.Equal(MiniErpTestFixtures.EntryProject, plan.EntryProject);
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
                new MockResponse(Content: MiniErpTestFixtures.ValidImplementationPlanJson)));

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
                    Title: "Add health comment",
                    Summary: "Add comment",
                    AcceptanceCriteria: ["GET /health still returns status ok"],
                    Constraints: ["Minimal API only"],
                    Priority: "Medium"),
                workflowId,
                producer: AnalystAgent.AgentName);
            await store.SaveAsync(wrong);

            var agent = CreateAgent(store, new MockLLMProvider(new MockResponse(Content: MiniErpTestFixtures.ValidImplementationPlanJson)));

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
              "title": "Add health comment",
              "summary": "Add comment",
              "tasks": [
                { "id": "T1", "description": "Edit Program.cs", "estimate": "15m" }
              ],
              "acceptanceCriteria": ["GET /health still returns status ok"],
              "technicalNotes": "Minimal",
              "intendedPaths": ["MiniErp/Program.cs"]
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
                Title: "Add health comment",
                Summary: "Add comment to Program.cs",
                AcceptanceCriteria: ["GET /health still returns status ok"],
                Constraints: ["Minimal API only"],
                Priority: "Medium",
                SuspectedPaths: [MiniErpTestFixtures.ProgramPath]),
            workflowId,
            producer: AnalystAgent.AgentName);
        await store.SaveAsync(structured);

        var builder = new CodeContextBuilder(store);
        var codeContext = await builder.BuildAndPublishAsync(
            workflowId,
            fixtureId: MiniErpTestFixtures.FixtureId,
            fixtureRoot: MiniErpTestFixtures.FixtureRoot,
            entryProject: MiniErpTestFixtures.EntryProject);

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
