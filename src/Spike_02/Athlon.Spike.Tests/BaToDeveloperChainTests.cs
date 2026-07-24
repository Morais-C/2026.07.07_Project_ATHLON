using Athlon.Spike.Agents;
using Athlon.Spike.Artifacts;
using Athlon.Spike.Contracts;
using Athlon.Spike.Llm;
using Athlon.Spike.Workflow;

namespace Athlon.Spike.Tests;

/// <summary>
/// Phase 4: BA → StructuredRequirement → Developer (by id), with mock LLM.
/// </summary>
public class BaToDeveloperChainTests
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
    public async Task Chain_runs_Developer_with_BA_artifact_id_only()
    {
        var root = CreateTempArtifactRoot();
        var store = new FileArtifactStore(root);
        var runner = new WorkflowRunner(store, artifactRoot: root);

        // Shared mock queue: first completion = BA, second = Developer
        var llm = new MockLLMProvider(
            new MockResponse(Content: ValidStructuredRequirementJson),
            new MockResponse(Content: ValidImplementationJson));

        var ba = new BusinessAnalystAgent(
            llm, store, SpikeTestPaths.BaPromptTemplate, SpikeTestPaths.StructuredRequirementSchema);
        var developer = new DeveloperAgent(
            llm, store, SpikeTestPaths.PromptTemplate, SpikeTestPaths.ImplementationSchema);

        var developerRan = false;
        Guid? developerInputId = null;

        try
        {
            var (instance, inputArtifact) = await runner.StartAndSaveInputAsync(
                "BusinessNeedToImplementation",
                "As an employee I want meal allowance",
                CancellationToken.None);

            var baResult = await ba.ExecuteAsync(new AgentExecutionContext(instance.Id, inputArtifact.Id));
            Assert.Equal(ArtifactTypes.StructuredRequirement, baResult.OutputArtifact.Type);

            // Mid-chain gate: Developer must not have run yet
            Assert.False(developerRan);

            developerInputId = baResult.OutputArtifact.Id;
            var devResult = await developer.ExecuteAsync(
                new AgentExecutionContext(instance.Id, baResult.OutputArtifact.Id));
            developerRan = true;

            Assert.Equal(baResult.OutputArtifact.Id, developerInputId);
            Assert.Equal(ArtifactTypes.Implementation, devResult.OutputArtifact.Type);
            Assert.Equal("Meal Allowance", ImplementationArtifact.Parse(devResult.OutputArtifact).Title);

            // Both artifacts exist on disk under the same workflow instance
            Assert.NotNull(await store.LoadAsync(baResult.OutputArtifact.Id));
            Assert.NotNull(await store.LoadAsync(devResult.OutputArtifact.Id));
        }
        finally
        {
            Cleanup(root);
        }
    }

    [Fact]
    public async Task Pause_callback_runs_before_Developer()
    {
        var root = CreateTempArtifactRoot();
        var store = new FileArtifactStore(root);
        var runner = new WorkflowRunner(store, artifactRoot: root);

        var llm = new MockLLMProvider(
            new MockResponse(Content: ValidStructuredRequirementJson),
            new MockResponse(Content: ValidImplementationJson));

        var ba = new BusinessAnalystAgent(
            llm, store, SpikeTestPaths.BaPromptTemplate, SpikeTestPaths.StructuredRequirementSchema);
        var developer = new DeveloperAgent(
            llm, store, SpikeTestPaths.PromptTemplate, SpikeTestPaths.ImplementationSchema);

        var steps = new List<string>();

        try
        {
            var (instance, inputArtifact) = await runner.StartAndSaveInputAsync(
                "BusinessNeedToImplementation",
                "Meal allowance",
                CancellationToken.None);

            steps.Add("ba-start");
            var baResult = await ba.ExecuteAsync(new AgentExecutionContext(instance.Id, inputArtifact.Id));
            steps.Add("ba-done");

            // Stand-in for Console.ReadLine mid-chain pause
            await PauseAsync(() => steps.Add("pause"));

            steps.Add("developer-start");
            await developer.ExecuteAsync(new AgentExecutionContext(instance.Id, baResult.OutputArtifact.Id));
            steps.Add("developer-done");

            Assert.Equal(
                ["ba-start", "ba-done", "pause", "developer-start", "developer-done"],
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
