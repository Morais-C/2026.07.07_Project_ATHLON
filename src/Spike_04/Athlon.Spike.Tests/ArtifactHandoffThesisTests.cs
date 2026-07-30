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

    private const string StoreUniqueGoal = "STORE_UNIQUE_GOAL_artifact_handoff_7f3a";

    // Lives inside the published ImplementationPlan payload — must reach the Coder prompt
    private const string StoreUniquePlanNote = "STORE_UNIQUE_PLAN_NOTE_artifact_handoff_2c91";

    private static string ValidStructuredRequirementJson =>
        $$"""
        {
          "title": "Employee Daily Meal Allowance",
          "actors": ["Employee", "Payroll"],
          "goal": "{{StoreUniqueGoal}}",
          "acceptanceCriteriaDraft": [
            "Eligible employees receive a fixed daily allowance on working days"
          ],
          "constraints": ["On-site working days only"],
          "priority": "Medium"
        }
        """;

    private static string ValidImplementationPlanJson =>
        $$"""
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
          "technicalNotes": "{{StoreUniquePlanNote}}"
        }
        """;

    private const string ValidCodePackageJson = """
        {
          "files": [
            {
              "path": "Allowance/Allowance.csproj",
              "content": "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net9.0</TargetFramework></PropertyGroup></Project>"
            },
            {
              "path": "Allowance/Program.cs",
              "content": "Console.WriteLine(\"Allowance: 7.63\");"
            }
          ],
          "entryProject": "Allowance/Allowance.csproj",
          "targetFramework": "net9.0",
          "expectedOutputContains": "Allowance: 7.63"
        }
        """;

    [Fact(Skip = "Spike_04 Phase 5: thesis tests rewired for StructuredChange / ChangeBundle handoff.")]
    public async Task Planner_prompt_comes_from_store_not_Analyst_raw_completion()
    {
        var root = CreateTempArtifactRoot();
        var store = new FileArtifactStore(root);
        var runner = new WorkflowRunner(store, artifactRoot: root);

        // Analyst mock: chat marker + valid JSON. Validator publishes JSON only (marker dropped).
        var analystRawCompletion =
            $"""
            {AnalystRawCompletionMarker}
            {ValidStructuredRequirementJson}
            """;

        var llm = new RecordingLLMProvider(
            new MockResponse(Content: analystRawCompletion),
            new MockResponse(Content: ValidImplementationPlanJson));

        var analyst = new AnalystAgent(
            llm, store, SpikeTestPaths.AnalystPromptTemplate, SpikeTestPaths.StructuredRequirementSchema);
        var planner = new PlannerAgent(
            llm, store, SpikeTestPaths.PlannerPromptTemplate, SpikeTestPaths.ImplementationPlanSchema);

        try
        {
            var (instance, inputArtifact) = await runner.StartAndSaveInputAsync(
                "BusinessNeedToImplementationPlan",
                "As an employee I want meal allowance",
                CancellationToken.None);

            var analystResult = await analyst.ExecuteAsync(new AgentExecutionContext(instance.Id, inputArtifact.Id));
            var structured = analystResult.OutputArtifact;

            // Published artifact must not carry the Analyst chat marker
            Assert.DoesNotContain(AnalystRawCompletionMarker, structured.PayloadJson);
            Assert.Contains(StoreUniqueGoal, structured.PayloadJson);

            await planner.ExecuteAsync(new AgentExecutionContext(instance.Id, structured.Id));

            // Second LLM call is Planner (first was Analyst)
            Assert.Equal(2, llm.Calls.Count);
            var plannerUserPrompt = llm.Calls[1].UserPrompt;

            // Prompt includes StructuredRequirement data loaded from the store (by id)
            var loaded = await store.LoadAsync(structured.Id);
            Assert.NotNull(loaded);
            Assert.Contains(StoreUniqueGoal, plannerUserPrompt);
            Assert.Contains(ArtifactJson.Serialize(loaded), plannerUserPrompt);

            // Prompt must not include Analyst-only raw completion marker
            Assert.DoesNotContain(AnalystRawCompletionMarker, plannerUserPrompt);
        }
        finally
        {
            Cleanup(root);
        }
    }

    [Fact(Skip = "Spike_04 Phase 5: thesis tests rewired for StructuredChange / ChangeBundle handoff.")]
    public async Task Coder_prompt_comes_from_store_not_Planner_raw_completion()
    {
        var root = CreateTempArtifactRoot();
        var store = new FileArtifactStore(root);
        var runner = new WorkflowRunner(store, artifactRoot: root);

        // Each LLM step returns chat noise + valid JSON; only the JSON is published.
        var analystRawCompletion =
            $"""
            {AnalystRawCompletionMarker}
            {ValidStructuredRequirementJson}
            """;

        var plannerRawCompletion =
            $"""
            {PlannerRawCompletionMarker}
            {ValidImplementationPlanJson}
            """;

        var llm = new RecordingLLMProvider(
            new MockResponse(Content: analystRawCompletion),
            new MockResponse(Content: plannerRawCompletion),
            new MockResponse(Content: ValidCodePackageJson));

        var analyst = new AnalystAgent(
            llm, store, SpikeTestPaths.AnalystPromptTemplate, SpikeTestPaths.StructuredRequirementSchema);
        var planner = new PlannerAgent(
            llm, store, SpikeTestPaths.PlannerPromptTemplate, SpikeTestPaths.ImplementationPlanSchema);
        var coder = new CoderAgent(
            llm, store, SpikeTestPaths.CoderPromptTemplate, SpikeTestPaths.CodePackageSchema);

        try
        {
            var (instance, inputArtifact) = await runner.StartAndSaveInputAsync(
                "BusinessNeedToPublish",
                "As an employee I want meal allowance",
                CancellationToken.None);

            var analystResult = await analyst.ExecuteAsync(new AgentExecutionContext(instance.Id, inputArtifact.Id));
            var plannerResult = await planner.ExecuteAsync(
                new AgentExecutionContext(instance.Id, analystResult.OutputArtifact.Id));
            var plan = plannerResult.OutputArtifact;

            // Published plan must not carry the Planner chat marker
            Assert.DoesNotContain(PlannerRawCompletionMarker, plan.PayloadJson);
            Assert.Contains(StoreUniquePlanNote, plan.PayloadJson);

            await coder.ExecuteAsync(new AgentExecutionContext(instance.Id, plan.Id));

            // Third LLM call is Coder (Analyst, then Planner)
            Assert.Equal(3, llm.Calls.Count);
            var coderUserPrompt = llm.Calls[2].UserPrompt;

            // Prompt is the ImplementationPlan artifact loaded from the store by id
            var loadedPlan = await store.LoadAsync(plan.Id);
            Assert.NotNull(loadedPlan);
            Assert.Contains(StoreUniquePlanNote, coderUserPrompt);
            Assert.Contains(ArtifactJson.Serialize(loadedPlan), coderUserPrompt);

            // No prior raw completion text, and no upstream artifact smuggled along
            Assert.DoesNotContain(PlannerRawCompletionMarker, coderUserPrompt);
            Assert.DoesNotContain(AnalystRawCompletionMarker, coderUserPrompt);
            Assert.DoesNotContain(StoreUniqueGoal, coderUserPrompt);
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
