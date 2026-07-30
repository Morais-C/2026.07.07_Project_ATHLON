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
    private const string ValidStructuredChangeJson = """
        {
          "kind": "feature",
          "title": "Uppercase echo",
          "summary": "Echo the typed line in uppercase",
          "acceptanceCriteria": ["Prints the input transformed to UPPERCASE"],
          "constraints": ["Single .NET 9 console", "Keep read → process → print"],
          "priority": "Medium",
          "suspectedPaths": ["Echo/Program.cs"]
        }
        """;

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
    public async Task Chain_runs_Planner_with_ChangeBundle_id_only()
    {
        var root = CreateTempArtifactRoot();
        var store = new FileArtifactStore(root);
        var runner = new WorkflowRunner(store, artifactRoot: root);
        var builder = new CodeContextBuilder(store);

        var llm = new MockLLMProvider(
            new MockResponse(Content: ValidStructuredChangeJson),
            new MockResponse(Content: ValidImplementationPlanJson));

        var analyst = new AnalystAgent(
            llm, store, SpikeTestPaths.AnalystPromptTemplate, SpikeTestPaths.StructuredChangeSchema);
        var planner = new PlannerAgent(
            llm, store, SpikeTestPaths.PlannerPromptTemplate, SpikeTestPaths.ImplementationPlanSchema);

        try
        {
            var (instance, inputArtifact) = await runner.StartAndSaveChangeRequestAsync(
                "ChangeRequestToPlan",
                new ChangeRequestPayload(
                    Kind: ChangeRequest.KindFeature,
                    Title: "Uppercase echo",
                    Description: "Print input in UPPERCASE",
                    SuspectedPaths: ["Echo/Program.cs"]),
                CancellationToken.None);

            var analystResult = await analyst.ExecuteAsync(new AgentExecutionContext(instance.Id, inputArtifact.Id));
            Assert.Equal(ArtifactTypes.StructuredChange, analystResult.OutputArtifact.Type);

            var codeContext = await builder.BuildAndPublishAsync(
                instance.Id,
                fixtureId: "echo-v1",
                fixtureRoot: SpikeTestPaths.EchoV1FixtureRoot,
                entryProject: "Echo/Echo.csproj");

            var bundle = await runner.SaveChangeBundleAsync(
                instance.Id,
                analystResult.OutputArtifact.Id,
                codeContext.Id,
                CancellationToken.None);

            var plannerResult = await planner.ExecuteAsync(
                new AgentExecutionContext(instance.Id, bundle.Id));

            Assert.Equal(ArtifactTypes.ImplementationPlan, plannerResult.OutputArtifact.Type);
            Assert.Equal("Uppercase echo", ImplementationPlan.Parse(plannerResult.OutputArtifact).Title);

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
