using Athlon.Spike.Agents;
using Athlon.Spike.Artifacts;
using Athlon.Spike.Contracts;
using Athlon.Spike.Llm;
using Athlon.Spike.Workflow;

namespace Athlon.Spike.Tests;

/// <summary>
/// Thesis: Planner and Coder prompts are built from LoadAsync(id) only —
/// not from prior chat text. Pack path wiring is covered by <see cref="PackResolutionThesisTests"/>.
/// </summary>
public class ArtifactHandoffThesisTests
{
    private const string AnalystRawCompletionMarker = "ANALYST_RAW_COMPLETION_MARKER_do_not_forward";
    private const string PlannerRawCompletionMarker = "PLANNER_RAW_COMPLETION_MARKER_do_not_forward";
    private const string StoreUniqueSummary = "STORE_UNIQUE_SUMMARY_artifact_handoff_7f3a";
    private const string StoreUniquePlanNote = "STORE_UNIQUE_PLAN_NOTE_artifact_handoff_2c91";

    private static string ValidStructuredChangeJson =>
        $$"""
        {
          "kind": "feature",
          "title": "Add health comment",
          "summary": "{{StoreUniqueSummary}}",
          "acceptanceCriteria": ["GET /health still returns status ok"],
          "constraints": ["Minimal API only", "In-memory only"],
          "priority": "Medium",
          "suspectedPaths": ["MiniErp/Program.cs"]
        }
        """;

    private static string ValidImplementationPlanJson =>
        $$"""
        {
          "title": "Add health comment",
          "summary": "Add comment to Program.cs",
          "tasks": [
            {
              "id": "T1",
              "description": "Add comment line to MiniErp/Program.cs",
              "estimate": "15m"
            }
          ],
          "acceptanceCriteria": [
            "GET /health still returns status ok"
          ],
          "technicalNotes": "{{StoreUniquePlanNote}}",
          "intendedPaths": ["MiniErp/Program.cs"]
        }
        """;

    [Fact]
    public async Task Planner_prompt_comes_from_store_not_Analyst_raw_completion()
    {
        var pack = MiniErpTestFixtures.Pack;
        var root = CreateTempArtifactRoot();
        var store = new FileArtifactStore(root);
        var runner = new WorkflowRunner(store, artifactRoot: root);
        var builder = CodeContextBuilder.FromArchetypePack(store, pack);

        var analystRawCompletion =
            $"""
            {AnalystRawCompletionMarker}
            {ValidStructuredChangeJson}
            """;

        var llm = new RecordingLLMProvider(
            new MockResponse(Content: analystRawCompletion),
            new MockResponse(Content: ValidImplementationPlanJson));

        var analyst = new AnalystAgent(
            llm, store, pack.Analyst.PromptPath, pack.Analyst.OutputSchemaPath);
        var planner = new PlannerAgent(
            llm, store, pack.Planner.PromptPath, pack.Planner.OutputSchemaPath);

        try
        {
            var (instance, inputArtifact) = await runner.StartAndSaveChangeRequestAsync(
                "ChangeRequestToPlan",
                MiniErpTestFixtures.SampleChangeRequest(),
                CancellationToken.None);

            var analystResult = await analyst.ExecuteAsync(
                new AgentExecutionContext(instance.Id, inputArtifact.Id));
            var structured = analystResult.OutputArtifact;

            Assert.DoesNotContain(AnalystRawCompletionMarker, structured.PayloadJson);
            Assert.Contains(StoreUniqueSummary, structured.PayloadJson);

            var codeContext = await builder.BuildAndPublishAsync(
                instance.Id,
                fixtureId: pack.Baseline.FixtureId,
                fixtureRoot: pack.Baseline.FixtureRoot,
                entryProject: MiniErpTestFixtures.EntryProject);

            var bundle = await runner.SaveChangeBundleAsync(
                instance.Id, structured.Id, codeContext.Id, CancellationToken.None);

            await planner.ExecuteAsync(new AgentExecutionContext(instance.Id, bundle.Id));

            Assert.Equal(2, llm.Calls.Count);
            var plannerUserPrompt = llm.Calls[1].UserPrompt;

            var loadedStructured = await store.LoadAsync(structured.Id);
            var loadedCodeContext = await store.LoadAsync(codeContext.Id);
            Assert.NotNull(loadedStructured);
            Assert.NotNull(loadedCodeContext);
            Assert.Contains(StoreUniqueSummary, plannerUserPrompt);
            Assert.Contains(ArtifactJson.Serialize(loadedStructured), plannerUserPrompt);
            Assert.Contains(ArtifactJson.Serialize(loadedCodeContext), plannerUserPrompt);
            Assert.DoesNotContain(AnalystRawCompletionMarker, plannerUserPrompt);
        }
        finally
        {
            Cleanup(root);
        }
    }

    [Fact]
    public async Task Coder_prompt_comes_from_store_not_Planner_raw_completion()
    {
        var pack = MiniErpTestFixtures.Pack;
        var root = CreateTempArtifactRoot();
        var store = new FileArtifactStore(root);
        var runner = new WorkflowRunner(store, artifactRoot: root);
        var builder = CodeContextBuilder.FromArchetypePack(store, pack);

        var analystRawCompletion =
            $"""
            {AnalystRawCompletionMarker}
            {ValidStructuredChangeJson}
            """;

        var plannerRawCompletion =
            $"""
            {PlannerRawCompletionMarker}
            {ValidImplementationPlanJson}
            """;

        var llm = new RecordingLLMProvider(
            new MockResponse(Content: analystRawCompletion),
            new MockResponse(Content: plannerRawCompletion),
            new MockResponse(Content: MiniErpTestFixtures.ValidPatchPackageJson()));

        var analyst = new AnalystAgent(
            llm, store, pack.Analyst.PromptPath, pack.Analyst.OutputSchemaPath);
        var planner = new PlannerAgent(
            llm, store, pack.Planner.PromptPath, pack.Planner.OutputSchemaPath);
        var coder = new CoderAgent(
            llm, store, pack.Coder.PromptPath, pack.Coder.OutputSchemaPath);

        try
        {
            var (instance, inputArtifact) = await runner.StartAndSaveChangeRequestAsync(
                "ChangeRequestToPatch",
                MiniErpTestFixtures.SampleChangeRequest(),
                CancellationToken.None);

            var analystResult = await analyst.ExecuteAsync(
                new AgentExecutionContext(instance.Id, inputArtifact.Id));
            var codeContext = await builder.BuildAndPublishAsync(
                instance.Id,
                fixtureId: pack.Baseline.FixtureId,
                fixtureRoot: pack.Baseline.FixtureRoot,
                entryProject: MiniErpTestFixtures.EntryProject);
            var bundle = await runner.SaveChangeBundleAsync(
                instance.Id,
                analystResult.OutputArtifact.Id,
                codeContext.Id,
                CancellationToken.None);

            var plannerResult = await planner.ExecuteAsync(
                new AgentExecutionContext(instance.Id, bundle.Id));
            var plan = plannerResult.OutputArtifact;

            Assert.DoesNotContain(PlannerRawCompletionMarker, plan.PayloadJson);
            Assert.Contains(StoreUniquePlanNote, plan.PayloadJson);

            await coder.ExecuteAsync(new AgentExecutionContext(instance.Id, plan.Id));

            Assert.Equal(3, llm.Calls.Count);
            var coderUserPrompt = llm.Calls[2].UserPrompt;

            var loadedPlan = await store.LoadAsync(plan.Id);
            var loadedCodeContext = await store.LoadAsync(codeContext.Id);
            Assert.NotNull(loadedPlan);
            Assert.NotNull(loadedCodeContext);
            Assert.Contains(StoreUniquePlanNote, coderUserPrompt);
            Assert.Contains(ArtifactJson.Serialize(loadedPlan), coderUserPrompt);
            Assert.Contains(ArtifactJson.Serialize(loadedCodeContext), coderUserPrompt);
            Assert.DoesNotContain(PlannerRawCompletionMarker, coderUserPrompt);
            Assert.DoesNotContain(AnalystRawCompletionMarker, coderUserPrompt);
            Assert.DoesNotContain(StoreUniqueSummary, coderUserPrompt);
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
