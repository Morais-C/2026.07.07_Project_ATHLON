using Athlon.Spike.Agents;
using Athlon.Spike.Artifacts;
using Athlon.Spike.Contracts;
using Athlon.Spike.Llm;
using Athlon.Spike.Workflow;

namespace Athlon.Spike.Tests;

/// <summary>
/// Thesis: Planner prompt is built from LoadAsync(id) only — not from Analyst chat text.
/// </summary>
public class ArtifactHandoffThesisTests
{
    // Present only in the Analyst mock LLM completion — must NOT appear in Planner prompt
    private const string AnalystRawCompletionMarker = "ANALYST_RAW_COMPLETION_MARKER_do_not_forward";

    private const string StoreUniqueGoal = "STORE_UNIQUE_GOAL_artifact_handoff_7f3a";

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

    private const string ValidImplementationPlanJson = """
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
