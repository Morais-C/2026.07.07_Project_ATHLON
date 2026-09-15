using Athlon.Spike.Agents;
using Athlon.Spike.Artifacts;
using Athlon.Spike.Contracts;
using Athlon.Spike.Llm;
using Athlon.Spike.Workflow;

namespace Athlon.Spike.Tests;

public class CoderAgentTests
{
    [Fact]
    public async Task Publishes_valid_PatchPackage_from_mock_llm_response()
    {
        var root = CreateTempArtifactRoot();
        var store = new FileArtifactStore(root);
        var workflowId = Guid.NewGuid();

        try
        {
            var plan = await SeedImplementationPlanAsync(store, workflowId);

            var agent = CreateAgent(store, new MockLLMProvider(new MockResponse(Content: MiniErpTestFixtures.ValidPatchPackageJson())));
            var result = await agent.ExecuteAsync(new AgentExecutionContext(workflowId, plan.Id));

            Assert.Equal(ArtifactTypes.PatchPackage, result.OutputArtifact.Type);
            Assert.Equal(AgentName, result.OutputArtifact.Producer);

            var package = PatchPackage.Parse(result.OutputArtifact);
            Assert.Equal(MiniErpTestFixtures.FixtureId, package.FixtureId);
            Assert.Equal(MiniErpTestFixtures.EntryProject, package.EntryProject);
            Assert.Equal("net9.0", package.TargetFramework);
            Assert.Equal("Add comment to Program.cs", package.Summary);
            Assert.Single(package.Changes);
            Assert.Equal("modify", package.Changes[0].Operation);
            Assert.Equal(MiniErpTestFixtures.ProgramPath, package.Changes[0].Path);
            Assert.True(result.Telemetry.TotalTokens > 0);
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
            var plan = await SeedImplementationPlanAsync(store, workflowId);

            var agent = CreateAgent(store, new MockLLMProvider(
                new MockResponse(Content: "not valid json"),
                new MockResponse(Content: MiniErpTestFixtures.ValidPatchPackageJson())));

            var result = await agent.ExecuteAsync(new AgentExecutionContext(workflowId, plan.Id));

            Assert.Equal(ArtifactTypes.PatchPackage, result.OutputArtifact.Type);
            Assert.Equal(200, result.Telemetry.PromptTokens);
            Assert.Equal(100, result.Telemetry.CompletionTokens);
        }
        finally
        {
            Cleanup(root);
        }
    }

    [Fact]
    public async Task Fails_after_retry_without_publishing_PatchPackage()
    {
        var root = CreateTempArtifactRoot();
        var store = new FileArtifactStore(root);
        var workflowId = Guid.NewGuid();

        try
        {
            var plan = await SeedImplementationPlanAsync(store, workflowId);
            var seedCount = Directory.GetFiles(root, "*.json", SearchOption.AllDirectories).Length;

            var agent = CreateAgent(store, new MockLLMProvider(
                new MockResponse(Content: "still not json"),
                new MockResponse(Content: """{ "changes": [] }""")));

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => agent.ExecuteAsync(new AgentExecutionContext(workflowId, plan.Id)));

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
    public async Task Rejects_unsafe_paths_and_does_not_publish()
    {
        var root = CreateTempArtifactRoot();
        var store = new FileArtifactStore(root);
        var workflowId = Guid.NewGuid();

        const string unsafePackage = """
            {
              "fixtureId": "mini-erp-v1",
              "changes": [
                {
                  "path": "../evil/Program.cs",
                  "operation": "modify",
                  "unifiedDiff": "--- a/../evil/Program.cs\n+++ b/../evil/Program.cs\n@@ -1 +1 @@\n-x\n+y\n"
                }
              ],
              "entryProject": "../evil/Evil.csproj",
              "targetFramework": "net9.0",
              "summary": "nope"
            }
            """;

        try
        {
            var plan = await SeedImplementationPlanAsync(store, workflowId);
            var seedCount = Directory.GetFiles(root, "*.json", SearchOption.AllDirectories).Length;

            var agent = CreateAgent(store, new MockLLMProvider(
                new MockResponse(Content: unsafePackage),
                new MockResponse(Content: unsafePackage)));

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => agent.ExecuteAsync(new AgentExecutionContext(workflowId, plan.Id)));

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
    public async Task Rejects_ChangeBundle_input_type()
    {
        var root = CreateTempArtifactRoot();
        var store = new FileArtifactStore(root);
        var workflowId = Guid.NewGuid();

        try
        {
            var wrong = ChangeBundle.Create(
                new ChangeBundlePayload(Guid.NewGuid(), Guid.NewGuid()),
                workflowId);
            await store.SaveAsync(wrong);

            var agent = CreateAgent(store, new MockLLMProvider(new MockResponse(Content: MiniErpTestFixtures.ValidPatchPackageJson())));

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => agent.ExecuteAsync(new AgentExecutionContext(workflowId, wrong.Id)));

            Assert.Contains(ArtifactTypes.ImplementationPlan, exception.Message);
            Assert.Contains(ArtifactTypes.ChangeBundle, exception.Message);
        }
        finally
        {
            Cleanup(root);
        }
    }

    private static async Task<Artifact> SeedImplementationPlanAsync(FileArtifactStore store, Guid workflowId)
    {
        var builder = new CodeContextBuilder(store);
        var codeContext = await builder.BuildAndPublishAsync(
            workflowId,
            fixtureId: MiniErpTestFixtures.FixtureId,
            fixtureRoot: MiniErpTestFixtures.FixtureRoot,
            entryProject: MiniErpTestFixtures.EntryProject);

        var plan = ImplementationPlan.Create(
            new ImplementationPlanPayload(
                Title: "Add health comment",
                Summary: "Add comment to Program.cs",
                Tasks: [new ImplementationTask("T1", "Edit Program.cs", "15m")],
                AcceptanceCriteria: ["GET /health still returns status ok"],
                TechnicalNotes: "Minimal diff",
                IntendedPaths: [MiniErpTestFixtures.ProgramPath],
                FixtureId: MiniErpTestFixtures.FixtureId,
                CodeContextArtifactId: codeContext.Id.ToString("D"),
                EntryProject: MiniErpTestFixtures.EntryProject,
                TargetFramework: "net9.0"),
            workflowId,
            producer: PlannerAgent.AgentName);
        await store.SaveAsync(plan);
        return plan;
    }

    private static CoderAgent CreateAgent(FileArtifactStore store, MockLLMProvider llm) =>
        new(
            llm,
            store,
            SpikeTestPaths.CoderPromptTemplate,
            SpikeTestPaths.PatchPackageSchema);

    private static string CreateTempArtifactRoot() =>
        Path.Combine(Path.GetTempPath(), "athlon-spike-tests", Guid.NewGuid().ToString("D"));

    private static void Cleanup(string root)
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private const string AgentName = CoderAgent.AgentName;
}
