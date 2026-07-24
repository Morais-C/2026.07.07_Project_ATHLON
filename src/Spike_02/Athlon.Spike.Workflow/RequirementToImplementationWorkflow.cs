using System.Diagnostics;
using Athlon.Spike.Contracts;

namespace Athlon.Spike.Workflow;

/// <summary>
/// Legacy single-Developer pipeline (Spike_01 shape).
/// Spike_02 demo now chains BA → Developer inline in Program.cs (Phase 4).
/// </summary>
public sealed class RequirementToImplementationWorkflow : IWorkflow
{
    public const string WorkflowNameValue = "RequirementToImplementation";

    private readonly WorkflowRunner _runner;       // shared lifecycle helpers
    private readonly IAgent _developerAgent;      // the only agent in this spike phase
    private readonly IWorkflowStepLogger _logger;
    private readonly Func<CancellationToken, Task<bool>> _approvalHandler;

    public RequirementToImplementationWorkflow(
        WorkflowRunner runner,
        IAgent developerAgent,
        IWorkflowStepLogger? logger = null,
        Func<CancellationToken, Task<bool>>? approvalHandler = null)
    {
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
        _developerAgent = developerAgent ?? throw new ArgumentNullException(nameof(developerAgent));
        _logger = logger ?? new ConsoleWorkflowStepLogger();
        // Default: always approve (Program.cs also passes AutoApprove: true)
        _approvalHandler = approvalHandler ?? (_ => Task.FromResult(true));
    }

    public string Name => WorkflowNameValue;

    public async Task<WorkflowResult> RunAsync(
        WorkflowInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.RequirementText);

        WorkflowInstance? instance = null;
        Artifact? inputArtifact = null;

        try
        {
            // 1) Start run + save the raw need as an immutable input artifact
            (instance, inputArtifact) = await _runner
                .StartAndSaveInputAsync(Name, input.RequirementText, cancellationToken)
                .ConfigureAwait(false);

            // 2) Developer works from artifact id only (not the raw string again)
            var agentStarted = Stopwatch.StartNew();
            var agentResult = await _developerAgent.ExecuteAsync(
                new AgentExecutionContext(instance.Id, inputArtifact.Id),
                cancellationToken).ConfigureAwait(false);
            agentStarted.Stop();
            _logger.LogStep("run-developer-agent", agentStarted.Elapsed, agentResult.OutputArtifact.Id);

            // 3) Human gate (skipped when AutoApprove is true)
            instance = _runner.MarkAwaitingApproval(instance);
            _logger.LogStep("awaiting-approval", TimeSpan.Zero);

            var approved = input.AutoApprove || await _approvalHandler(cancellationToken).ConfigureAwait(false);
            if (!approved)
            {
                instance = _runner.MarkFailed(instance);
                return new WorkflowResult(
                    Instance: instance,
                    InputArtifact: inputArtifact,
                    OutputArtifact: agentResult.OutputArtifact,
                    Telemetry: agentResult.Telemetry,
                    FailureMessage: "Workflow was not approved.");
            }

            // 4) Success path: mark done + persist LLM telemetry next to artifacts
            instance = _runner.MarkCompleted(instance);
            _logger.LogStep("complete", TimeSpan.Zero, agentResult.OutputArtifact.Id);

            await _runner.PersistTelemetryAsync(instance.Id, agentResult.Telemetry, cancellationToken)
                .ConfigureAwait(false);

            var result = new WorkflowResult(
                Instance: instance,
                InputArtifact: inputArtifact,
                OutputArtifact: agentResult.OutputArtifact,
                Telemetry: agentResult.Telemetry);

            WorkflowRunner.PrintRunSummary(result);
            return result;
        }
        catch (Exception ex)
        {
            // If we already created an instance, fail it instead of crashing silently
            if (instance is null || inputArtifact is null)
            {
                throw;
            }

            instance = _runner.MarkFailed(instance);
            return new WorkflowResult(
                Instance: instance,
                InputArtifact: inputArtifact,
                OutputArtifact: null,
                Telemetry: null,
                FailureMessage: ex.Message);
        }
    }
}
