using Athlon.Spike.Agents;
using Athlon.Spike.Artifacts;
using Athlon.Spike.Contracts;
using Athlon.Spike.Llm;
using Athlon.Spike.Workflow;

namespace Athlon.Spike.Tests;

/// <summary>
/// Analyst → StructuredChange → CodeContext → ChangeBundle → Planner (by id), with mock LLM.
/// </summary>
public class AnalystToPlannerChainTests
{
    [Fact]
    public async Task Chain_runs_Planner_with_ChangeBundle_id_only()
    {
        var root = CreateTempArtifactRoot();
        var store = new FileArtifactStore(root);
        var runner = new WorkflowRunner(store, artifactRoot: root);
        var builder = new CodeContextBuilder(store);

        var llm = new MockLLMProvider(
            new MockResponse(Content: MiniErpTestFixtures.ValidStructuredChangeJson),
            new MockResponse(Content: MiniErpTestFixtures.ValidImplementationPlanJson));

        var analyst = new AnalystAgent(
            llm, store, SpikeTestPaths.AnalystPromptTemplate, SpikeTestPaths.StructuredChangeSchema);
        var planner = new PlannerAgent(
            llm, store, SpikeTestPaths.PlannerPromptTemplate, SpikeTestPaths.ImplementationPlanSchema);

        try
        {
            var (instance, inputArtifact) = await runner.StartAndSaveChangeRequestAsync(
                "ChangeRequestToPlan",
                MiniErpTestFixtures.SampleChangeRequest(),
                CancellationToken.None);

            var analystResult = await analyst.ExecuteAsync(new AgentExecutionContext(instance.Id, inputArtifact.Id));
            Assert.Equal(ArtifactTypes.StructuredChange, analystResult.OutputArtifact.Type);

            var codeContext = await builder.BuildAndPublishAsync(
                instance.Id,
                fixtureId: MiniErpTestFixtures.FixtureId,
                fixtureRoot: MiniErpTestFixtures.FixtureRoot,
                entryProject: MiniErpTestFixtures.EntryProject);

            var bundle = await runner.SaveChangeBundleAsync(
                instance.Id,
                analystResult.OutputArtifact.Id,
                codeContext.Id,
                CancellationToken.None);

            var plannerResult = await planner.ExecuteAsync(
                new AgentExecutionContext(instance.Id, bundle.Id));

            Assert.Equal(ArtifactTypes.ImplementationPlan, plannerResult.OutputArtifact.Type);
            Assert.Equal("Add health comment", ImplementationPlan.Parse(plannerResult.OutputArtifact).Title);

            Assert.NotNull(await store.LoadAsync(analystResult.OutputArtifact.Id));
            Assert.NotNull(await store.LoadAsync(codeContext.Id));
            Assert.NotNull(await store.LoadAsync(bundle.Id));
            Assert.NotNull(await store.LoadAsync(plannerResult.OutputArtifact.Id));
        }
        finally
        {
            Cleanup(root);
        }
    }

    private static string CreateTempArtifactRoot() =>
        Path.Combine(Path.GetTempPath(), "athlon-spike-tests", Guid.NewGuid().ToString("D"));

    private static void Cleanup(string root)
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
