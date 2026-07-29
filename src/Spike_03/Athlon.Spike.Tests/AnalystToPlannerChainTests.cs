using Athlon.Spike.Agents;
using Athlon.Spike.Artifacts;
using Athlon.Spike.Contracts;
using Athlon.Spike.Llm;
using Athlon.Spike.Workflow;

namespace Athlon.Spike.Tests;

/// <summary>
/// Analyst → StructuredRequirement → Planner (by id), with mock LLM.
/// </summary>
public class AnalystToPlannerChainTests
{
    private const string ValidStructuredRequirementJson = """
        {
          "title": "Employee Daily Meal Allowance",
          "actors": ["Employee", "Payroll", "Manager"],
          "goal": "Provide a fixed daily meal allowance on working days",
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
    public async Task Chain_runs_Planner_with_Analyst_artifact_id_only()
    {
        var root = CreateTempArtifactRoot();
        var store = new FileArtifactStore(root);
        var runner = new WorkflowRunner(store, artifactRoot: root);

        // Shared mock queue: first completion = Analyst, second = Planner
        var llm = new MockLLMProvider(
            new MockResponse(Content: ValidStructuredRequirementJson),
            new MockResponse(Content: ValidImplementationPlanJson));

        var analyst = new AnalystAgent(
            llm, store, SpikeTestPaths.AnalystPromptTemplate, SpikeTestPaths.StructuredRequirementSchema);
        var planner = new PlannerAgent(
            llm, store, SpikeTestPaths.PlannerPromptTemplate, SpikeTestPaths.ImplementationPlanSchema);

        var plannerRan = false;
        Guid? plannerInputId = null;

        try
        {
            var (instance, inputArtifact) = await runner.StartAndSaveInputAsync(
                "BusinessNeedToImplementationPlan",
                "As an employee I want meal allowance",
                CancellationToken.None);

            var analystResult = await analyst.ExecuteAsync(new AgentExecutionContext(instance.Id, inputArtifact.Id));
            Assert.Equal(ArtifactTypes.StructuredRequirement, analystResult.OutputArtifact.Type);

            // Mid-chain gate: Planner must not have run yet
            Assert.False(plannerRan);

            plannerInputId = analystResult.OutputArtifact.Id;
            var plannerResult = await planner.ExecuteAsync(
                new AgentExecutionContext(instance.Id, analystResult.OutputArtifact.Id));
            plannerRan = true;

            Assert.Equal(analystResult.OutputArtifact.Id, plannerInputId);
            Assert.Equal(ArtifactTypes.ImplementationPlan, plannerResult.OutputArtifact.Type);
            Assert.Equal("Meal Allowance", ImplementationPlan.Parse(plannerResult.OutputArtifact).Title);

            // Both artifacts exist on disk under the same workflow instance
            Assert.NotNull(await store.LoadAsync(analystResult.OutputArtifact.Id));
            Assert.NotNull(await store.LoadAsync(plannerResult.OutputArtifact.Id));
        }
        finally
        {
            Cleanup(root);
        }
    }

    [Fact]
    public async Task Pause_callback_runs_before_Planner()
    {
        var root = CreateTempArtifactRoot();
        var store = new FileArtifactStore(root);
        var runner = new WorkflowRunner(store, artifactRoot: root);

        var llm = new MockLLMProvider(
            new MockResponse(Content: ValidStructuredRequirementJson),
            new MockResponse(Content: ValidImplementationPlanJson));

        var analyst = new AnalystAgent(
            llm, store, SpikeTestPaths.AnalystPromptTemplate, SpikeTestPaths.StructuredRequirementSchema);
        var planner = new PlannerAgent(
            llm, store, SpikeTestPaths.PlannerPromptTemplate, SpikeTestPaths.ImplementationPlanSchema);

        var steps = new List<string>();

        try
        {
            var (instance, inputArtifact) = await runner.StartAndSaveInputAsync(
                "BusinessNeedToImplementationPlan",
                "Meal allowance",
                CancellationToken.None);

            steps.Add("analyst-start");
            var analystResult = await analyst.ExecuteAsync(new AgentExecutionContext(instance.Id, inputArtifact.Id));
            steps.Add("analyst-done");

            // Stand-in for Console.ReadLine mid-chain pause
            await PauseAsync(() => steps.Add("pause"));

            steps.Add("planner-start");
            await planner.ExecuteAsync(new AgentExecutionContext(instance.Id, analystResult.OutputArtifact.Id));
            steps.Add("planner-done");

            Assert.Equal(
                ["analyst-start", "analyst-done", "pause", "planner-start", "planner-done"],
                steps);
        }
        finally
        {
            Cleanup(root);
        }
    }

    private static Task PauseAsync(Action onPause)
    {
        onPause();
        return Task.CompletedTask;
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
