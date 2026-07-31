using Athlon.Spike.Agents;
using Athlon.Spike.Artifacts;
using Athlon.Spike.Contracts;
using Athlon.Spike.Llm;
using Athlon.Spike.Workflow;

namespace Athlon.Spike.Tests;

/// <summary>
/// Thesis: Planner and Coder prompts are built from LoadAsync(id) only — not from prior chat text.
/// </summary>
public class ArtifactHandoffThesisTests
{
    // Present only in the Analyst mock LLM completion — must NOT appear in Planner prompt
    private const string AnalystRawCompletionMarker = "ANALYST_RAW_COMPLETION_MARKER_do_not_forward";

    // Present only in the Planner mock LLM completion — must NOT appear in Coder prompt
    private const string PlannerRawCompletionMarker = "PLANNER_RAW_COMPLETION_MARKER_do_not_forward";

    // Lives inside the published StructuredChange payload — must reach the Planner prompt
    private const string StoreUniqueSummary = "STORE_UNIQUE_SUMMARY_artifact_handoff_7f3a";

    // Lives inside the published ImplementationPlan payload — must reach the Coder prompt
    private const string StoreUniquePlanNote = "STORE_UNIQUE_PLAN_NOTE_artifact_handoff_2c91";

    private static string ValidStructuredChangeJson =>
        $$"""
        {
          "kind": "feature",
          "title": "Uppercase echo",
          "summary": "{{StoreUniqueSummary}}",
          "acceptanceCriteria": ["Prints the input transformed to UPPERCASE"],
          "constraints": ["Single .NET 9 console", "Keep read → process → print"],
          "priority": "Medium",
          "suspectedPaths": ["Echo/Program.cs"]
        }
        """;

    private static string ValidImplementationPlanJson =>
        $$"""
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
          "technicalNotes": "{{StoreUniquePlanNote}}",
          "intendedPaths": ["Echo/Program.cs"]
        }
        """;

    private const string ValidPatchPackageJson = """
        {
          "fixtureId": "echo-v1",
          "changes": [
            {
              "path": "Echo/Program.cs",
              "operation": "modify",
              "unifiedDiff": "--- a/Echo/Program.cs\n+++ b/Echo/Program.cs\n@@ -1,4 +1,4 @@\n // Spike_04 fixture baseline: read a line, echo it back (read → process → print).\n Console.Write(\"Enter text: \");\n var input = Console.ReadLine() ?? string.Empty;\n-Console.WriteLine(input);\n+Console.WriteLine(input.ToUpperInvariant());\n"
            }
          ],
          "entryProject": "Echo/Echo.csproj",
          "targetFramework": "net9.0",
          "summary": "Uppercase echoed line"
        }
        """;

    [Fact]
    public async Task Planner_prompt_comes_from_store_not_Analyst_raw_completion()
    {
        var root = CreateTempArtifactRoot();
        var store = new FileArtifactStore(root);
        var runner = new WorkflowRunner(store, artifactRoot: root);
        var builder = new CodeContextBuilder(store);

        // Analyst mock: chat marker + valid JSON. Validator publishes JSON only (marker dropped).
        var analystRawCompletion =
            $"""
            {AnalystRawCompletionMarker}
            {ValidStructuredChangeJson}
            """;

        var llm = new RecordingLLMProvider(
            new MockResponse(Content: analystRawCompletion),
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

            var analystResult = await analyst.ExecuteAsync(
                new AgentExecutionContext(instance.Id, inputArtifact.Id));
            var structured = analystResult.OutputArtifact;

            // Published artifact must not carry the Analyst chat marker
            Assert.DoesNotContain(AnalystRawCompletionMarker, structured.PayloadJson);
            Assert.Contains(StoreUniqueSummary, structured.PayloadJson);

            var codeContext = await builder.BuildAndPublishAsync(
                instance.Id,
                fixtureId: "echo-v1",
                fixtureRoot: SpikeTestPaths.EchoV1FixtureRoot,
                entryProject: "Echo/Echo.csproj");

            var bundle = await runner.SaveChangeBundleAsync(
                instance.Id, structured.Id, codeContext.Id, CancellationToken.None);

            await planner.ExecuteAsync(new AgentExecutionContext(instance.Id, bundle.Id));

            // Second LLM call is Planner (first was Analyst)
            Assert.Equal(2, llm.Calls.Count);
            var plannerUserPrompt = llm.Calls[1].UserPrompt;

            // Prompt includes StructuredChange + CodeContext loaded from the store (by id)
            var loadedStructured = await store.LoadAsync(structured.Id);
            var loadedCodeContext = await store.LoadAsync(codeContext.Id);
            Assert.NotNull(loadedStructured);
            Assert.NotNull(loadedCodeContext);
            Assert.Contains(StoreUniqueSummary, plannerUserPrompt);
            Assert.Contains(ArtifactJson.Serialize(loadedStructured), plannerUserPrompt);
            Assert.Contains(ArtifactJson.Serialize(loadedCodeContext), plannerUserPrompt);

            // Prompt must not include Analyst-only raw completion marker
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
        var root = CreateTempArtifactRoot();
        var store = new FileArtifactStore(root);
        var runner = new WorkflowRunner(store, artifactRoot: root);
        var builder = new CodeContextBuilder(store);

        // Each LLM step returns chat noise + valid JSON; only the JSON is published.
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
            new MockResponse(Content: ValidPatchPackageJson));

        var analyst = new AnalystAgent(
            llm, store, SpikeTestPaths.AnalystPromptTemplate, SpikeTestPaths.StructuredChangeSchema);
        var planner = new PlannerAgent(
            llm, store, SpikeTestPaths.PlannerPromptTemplate, SpikeTestPaths.ImplementationPlanSchema);
        var coder = new CoderAgent(
            llm, store, SpikeTestPaths.CoderPromptTemplate, SpikeTestPaths.PatchPackageSchema);

        try
        {
            var (instance, inputArtifact) = await runner.StartAndSaveChangeRequestAsync(
                "ChangeRequestToPatch",
                new ChangeRequestPayload(
                    Kind: ChangeRequest.KindFeature,
                    Title: "Uppercase echo",
                    Description: "Print input in UPPERCASE",
                    SuspectedPaths: ["Echo/Program.cs"]),
                CancellationToken.None);

            var analystResult = await analyst.ExecuteAsync(
                new AgentExecutionContext(instance.Id, inputArtifact.Id));
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
            var plan = plannerResult.OutputArtifact;

            // Published plan must not carry the Planner chat marker
            Assert.DoesNotContain(PlannerRawCompletionMarker, plan.PayloadJson);
            Assert.Contains(StoreUniquePlanNote, plan.PayloadJson);

            await coder.ExecuteAsync(new AgentExecutionContext(instance.Id, plan.Id));

            // Third LLM call is Coder (Analyst, then Planner)
            Assert.Equal(3, llm.Calls.Count);
            var coderUserPrompt = llm.Calls[2].UserPrompt;

            // Prompt is ImplementationPlan + CodeContext loaded from the store by id
            var loadedPlan = await store.LoadAsync(plan.Id);
            var loadedCodeContext = await store.LoadAsync(codeContext.Id);
            Assert.NotNull(loadedPlan);
            Assert.NotNull(loadedCodeContext);
            Assert.Contains(StoreUniquePlanNote, coderUserPrompt);
            Assert.Contains(ArtifactJson.Serialize(loadedPlan), coderUserPrompt);
            Assert.Contains(ArtifactJson.Serialize(loadedCodeContext), coderUserPrompt);

            // No prior raw completion text, and no StructuredChange smuggled along
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
