using Athlon.Spike.Agents;
using Athlon.Spike.Artifacts;
using Athlon.Spike.Contracts;
using Athlon.Spike.Llm;
using Athlon.Spike.Workflow;

namespace Athlon.Spike.Tests;

/// <summary>
/// Phase 2 exit: ChangeBundle → Planner → Coder publishes PatchPackage; invalid → retry; two failures → no publish.
/// </summary>
public class Phase2ChangeChainTests
{
    [Fact]
    public async Task ChangeBundle_yields_ImplementationPlan_and_PatchPackage()
    {
        var root = CreateTempArtifactRoot();
        var store = new FileArtifactStore(root);
        var runner = new WorkflowRunner(store, artifactRoot: root);
        var builder = new CodeContextBuilder(store);

        var llm = new MockLLMProvider(
            new MockResponse(Content: MiniErpTestFixtures.ValidImplementationPlanJson),
            new MockResponse(Content: MiniErpTestFixtures.ValidPatchPackageJson()));

        var planner = new PlannerAgent(
            llm, store, SpikeTestPaths.PlannerPromptTemplate, SpikeTestPaths.ImplementationPlanSchema);
        var coder = new CoderAgent(
            llm, store, SpikeTestPaths.CoderPromptTemplate, SpikeTestPaths.PatchPackageSchema);

        try
        {
            var workflowId = Guid.NewGuid();

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

            var codeContext = await builder.BuildAndPublishAsync(
                workflowId,
                fixtureId: MiniErpTestFixtures.FixtureId,
                fixtureRoot: MiniErpTestFixtures.FixtureRoot,
                entryProject: MiniErpTestFixtures.EntryProject);

            var bundle = await runner.SaveChangeBundleAsync(
                workflowId, structured.Id, codeContext.Id, CancellationToken.None);

            var plan = (await planner.ExecuteAsync(new AgentExecutionContext(workflowId, bundle.Id))).OutputArtifact;
            var patch = (await coder.ExecuteAsync(new AgentExecutionContext(workflowId, plan.Id))).OutputArtifact;

            Assert.Equal(ArtifactTypes.ChangeBundle, bundle.Type);
            Assert.Equal(ArtifactTypes.ImplementationPlan, plan.Type);
            Assert.Equal(ArtifactTypes.PatchPackage, patch.Type);

            var planPayload = ImplementationPlan.Parse(plan);
            Assert.Equal(codeContext.Id.ToString("D"), planPayload.CodeContextArtifactId);
            Assert.Equal(MiniErpTestFixtures.FixtureId, planPayload.FixtureId);

            var package = PatchPackage.Parse(patch);
            Assert.Equal(MiniErpTestFixtures.FixtureId, package.FixtureId);
            Assert.Single(package.Changes);
            Assert.Equal(PatchPackage.OperationModify, package.Changes[0].Operation);
        }
        finally
        {
            Cleanup(root);
        }
    }

    [Fact]
    public async Task Invalid_Coder_response_twice_does_not_publish_PatchPackage()
    {
        var root = CreateTempArtifactRoot();
        var store = new FileArtifactStore(root);
        var workflowId = Guid.NewGuid();

        try
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
                    TechnicalNotes: "Minimal",
                    IntendedPaths: [MiniErpTestFixtures.ProgramPath],
                    FixtureId: MiniErpTestFixtures.FixtureId,
                    CodeContextArtifactId: codeContext.Id.ToString("D"),
                    EntryProject: MiniErpTestFixtures.EntryProject,
                    TargetFramework: "net9.0"),
                workflowId,
                producer: PlannerAgent.AgentName);
            await store.SaveAsync(plan);

            var seedCount = Directory.GetFiles(root, "*.json", SearchOption.AllDirectories).Length;

            var coder = new CoderAgent(
                new MockLLMProvider(
                    new MockResponse(Content: "not json"),
                    new MockResponse(Content: """{ "fixtureId": "mini-erp-v1" }""")),
                store,
                SpikeTestPaths.CoderPromptTemplate,
                SpikeTestPaths.PatchPackageSchema);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => coder.ExecuteAsync(new AgentExecutionContext(workflowId, plan.Id)));

            Assert.Contains("after one retry", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(seedCount, Directory.GetFiles(root, "*.json", SearchOption.AllDirectories).Length);
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
