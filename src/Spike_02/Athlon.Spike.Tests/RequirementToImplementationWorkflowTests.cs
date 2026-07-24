using Athlon.Spike.Agents;
using Athlon.Spike.Artifacts;
using Athlon.Spike.Contracts;
using Athlon.Spike.Llm;
using Athlon.Spike.Workflow;

namespace Athlon.Spike.Tests;

public class RequirementToImplementationWorkflowTests
{
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

    [Fact(Skip = "Phase 4 replaces this single-Developer path with BA → StructuredRequirement → Developer")]
    public async Task Completes_with_status_transitions_when_auto_approved()
    {
        var root = CreateTempArtifactRoot();
        var store = new FileArtifactStore(root);
        var logger = new CapturingWorkflowStepLogger();
        var workflow = CreateWorkflow(store, logger, new MockLLMProvider(new MockResponse(Content: ValidImplementationJson)));

        try
        {
            var result = await workflow.RunAsync(new WorkflowInput(
                RequirementText: "As an employee I want meal allowance",
                AutoApprove: true));

            Assert.True(result.Succeeded);
            Assert.Equal(WorkflowStatus.Completed, result.Instance.Status);
            Assert.NotNull(result.OutputArtifact);
            Assert.NotNull(result.Telemetry);
            Assert.Contains("save-input-artifact", logger.Steps.Select(step => step.Step));
            Assert.Contains("run-developer-agent", logger.Steps.Select(step => step.Step));
            Assert.Contains("awaiting-approval", logger.Steps.Select(step => step.Step));
            Assert.Contains("complete", logger.Steps.Select(step => step.Step));

            var telemetryPath = Path.Combine(root, result.Instance.Id.ToString("D"), "telemetry.json");
            Assert.True(File.Exists(telemetryPath));
        }
        finally
        {
            Cleanup(root);
        }
    }

    [Fact(Skip = "Phase 4 replaces this single-Developer path with BA → StructuredRequirement → Developer")]
    public async Task Fails_when_not_approved()
    {
        var root = CreateTempArtifactRoot();
        var store = new FileArtifactStore(root);
        var workflow = CreateWorkflow(
            store,
            new CapturingWorkflowStepLogger(),
            new MockLLMProvider(new MockResponse(Content: ValidImplementationJson)),
            approvalHandler: _ => Task.FromResult(false));

        try
        {
            var result = await workflow.RunAsync(new WorkflowInput(
                RequirementText: "Meal allowance",
                AutoApprove: false));

            Assert.False(result.Succeeded);
            Assert.Equal(WorkflowStatus.Failed, result.Instance.Status);
            Assert.NotNull(result.OutputArtifact);
            Assert.Equal("Workflow was not approved.", result.FailureMessage);
        }
        finally
        {
            Cleanup(root);
        }
    }

    [Fact(Skip = "Phase 4 replaces this single-Developer path with BA → StructuredRequirement → Developer")]
    public async Task Fails_when_agent_validation_fails_twice()
    {
        var root = CreateTempArtifactRoot();
        var store = new FileArtifactStore(root);
        var workflow = CreateWorkflow(
            store,
            new CapturingWorkflowStepLogger(),
            new MockLLMProvider(
                new MockResponse(Content: "invalid"),
                new MockResponse(Content: "{ \"title\": \"incomplete\" }")));

        try
        {
            var result = await workflow.RunAsync(new WorkflowInput(
                RequirementText: "Meal allowance",
                AutoApprove: true));

            Assert.False(result.Succeeded);
            Assert.Equal(WorkflowStatus.Failed, result.Instance.Status);
            Assert.Null(result.OutputArtifact);
            Assert.Contains("after one retry", result.FailureMessage, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Cleanup(root);
        }
    }

    private static RequirementToImplementationWorkflow CreateWorkflow(
        FileArtifactStore store,
        IWorkflowStepLogger logger,
        MockLLMProvider llm,
        Func<CancellationToken, Task<bool>>? approvalHandler = null)
    {
        var agent = new DeveloperAgent(
            llm,
            store,
            SpikeTestPaths.PromptTemplate,
            SpikeTestPaths.ImplementationSchema);

        var runner = new WorkflowRunner(store, logger, store.RootPath);
        return new RequirementToImplementationWorkflow(runner, agent, logger, approvalHandler);
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

    private sealed class CapturingWorkflowStepLogger : IWorkflowStepLogger
    {
        public List<(string Step, TimeSpan Duration, Guid? ArtifactId)> Steps { get; } = [];

        public void LogStep(string step, TimeSpan duration, Guid? artifactId = null) =>
            Steps.Add((step, duration, artifactId));
    }
}
