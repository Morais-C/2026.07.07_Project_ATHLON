using Athlon.Spike.Agents;
using Athlon.Spike.Artifacts;
using Athlon.Spike.Contracts;
using Athlon.Spike.Llm;
using Athlon.Spike.Workflow;

namespace Athlon.Spike.Tests;

/// <summary>
/// Thesis: Developer prompt is built from LoadAsync(id) only — not from BA chat text.
/// </summary>
public class ArtifactHandoffThesisTests
{
    // Present only in the BA mock LLM completion — must NOT appear in Developer prompt
    private const string BaRawCompletionMarker = "BA_RAW_COMPLETION_MARKER_do_not_forward";

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

    private const string ValidImplementationJson = """
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
    public async Task Developer_prompt_comes_from_store_not_BA_raw_completion()
    {
        var root = CreateTempArtifactRoot();
        var store = new FileArtifactStore(root);
        var runner = new WorkflowRunner(store, artifactRoot: root);

        // BA mock: chat marker + valid JSON. Validator publishes JSON only (marker dropped).
        var baRawCompletion =
            $"""
            {BaRawCompletionMarker}
            {ValidStructuredRequirementJson}
            """;

        var llm = new RecordingLLMProvider(
            new MockResponse(Content: baRawCompletion),
            new MockResponse(Content: ValidImplementationJson));

        var ba = new BusinessAnalystAgent(
            llm, store, SpikeTestPaths.BaPromptTemplate, SpikeTestPaths.StructuredRequirementSchema);
        var developer = new DeveloperAgent(
            llm, store, SpikeTestPaths.PromptTemplate, SpikeTestPaths.ImplementationSchema);

        try
        {
            var (instance, inputArtifact) = await runner.StartAndSaveInputAsync(
                "BusinessNeedToImplementation",
                "As an employee I want meal allowance",
                CancellationToken.None);

            var baResult = await ba.ExecuteAsync(new AgentExecutionContext(instance.Id, inputArtifact.Id));
            var structured = baResult.OutputArtifact;

            // Published artifact must not carry the BA chat marker
            Assert.DoesNotContain(BaRawCompletionMarker, structured.PayloadJson);
            Assert.Contains(StoreUniqueGoal, structured.PayloadJson);

            await developer.ExecuteAsync(new AgentExecutionContext(instance.Id, structured.Id));

            // Second LLM call is Developer (first was BA)
            Assert.Equal(2, llm.Calls.Count);
            var developerUserPrompt = llm.Calls[1].UserPrompt;

            // Prompt includes StructuredRequirement data loaded from the store (by id)
            var loaded = await store.LoadAsync(structured.Id);
            Assert.NotNull(loaded);
            Assert.Contains(StoreUniqueGoal, developerUserPrompt);
            Assert.Contains(ArtifactJson.Serialize(loaded), developerUserPrompt);

            // Prompt must not include BA-only raw completion marker
            Assert.DoesNotContain(BaRawCompletionMarker, developerUserPrompt);
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
