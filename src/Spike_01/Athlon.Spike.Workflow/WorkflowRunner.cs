using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Athlon.Spike.Contracts;

namespace Athlon.Spike.Workflow;

public sealed class WorkflowRunner
{
    private readonly IArtifactStore _artifactStore;
    private readonly IWorkflowStepLogger _logger;
    private readonly string _artifactRoot;

    public WorkflowRunner(
        IArtifactStore artifactStore,
        IWorkflowStepLogger? logger = null,
        string? artifactRoot = null)
    {
        _artifactStore = artifactStore ?? throw new ArgumentNullException(nameof(artifactStore));
        _logger = logger ?? new ConsoleWorkflowStepLogger();
        _artifactRoot = artifactRoot
            ?? Environment.GetEnvironmentVariable("ATHLON_ARTIFACTS_PATH")
            ?? Path.Combine(Directory.GetCurrentDirectory(), "artifacts");
    }

    public async Task<(WorkflowInstance Instance, Artifact InputArtifact)> StartAndSaveInputAsync(
        string workflowName,
        string requirementText,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workflowName);
        ArgumentException.ThrowIfNullOrWhiteSpace(requirementText);

        var started = Stopwatch.StartNew();
        var instance = new WorkflowInstance(
            Id: Guid.NewGuid(),
            WorkflowName: workflowName,
            Status: WorkflowStatus.Started,
            StartedUtc: DateTime.UtcNow);

        var inputArtifact = BusinessRequirement.FromText(
            requirementText,
            instance.Id,
            producer: "Workflow");

        await _artifactStore.SaveAsync(inputArtifact, cancellationToken).ConfigureAwait(false);
        started.Stop();

        _logger.LogStep("save-input-artifact", started.Elapsed, inputArtifact.Id);
        return (instance, inputArtifact);
    }

    public WorkflowInstance MarkAwaitingApproval(WorkflowInstance instance)
    {
        ArgumentNullException.ThrowIfNull(instance);
        return instance with { Status = WorkflowStatus.AwaitingApproval };
    }

    public WorkflowInstance MarkCompleted(WorkflowInstance instance)
    {
        ArgumentNullException.ThrowIfNull(instance);
        return instance with
        {
            Status = WorkflowStatus.Completed,
            CompletedUtc = DateTime.UtcNow
        };
    }

    public WorkflowInstance MarkFailed(WorkflowInstance instance)
    {
        ArgumentNullException.ThrowIfNull(instance);
        return instance with
        {
            Status = WorkflowStatus.Failed,
            CompletedUtc = DateTime.UtcNow
        };
    }

    public void PrintRunSummary(WorkflowResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.Telemetry is null)
        {
            Console.WriteLine("Workflow complete (no telemetry available).");
            return;
        }

        var telemetry = result.Telemetry;
        Console.WriteLine("Workflow complete");
        Console.WriteLine($"  Output artifact : {result.OutputArtifact?.Id:D}");
        Console.WriteLine($"  Model           : {telemetry.Model}");
        Console.WriteLine($"  Prompt tokens   : {telemetry.PromptTokens}");
        Console.WriteLine($"  Completion tokens: {telemetry.CompletionTokens}");
        Console.WriteLine($"  Total tokens    : {telemetry.TotalTokens}");
        Console.WriteLine($"  Duration        : {telemetry.Duration.TotalSeconds:F1}s");
        Console.WriteLine($"  Est. cost (USD) : ${telemetry.EstimatedCostUsd:F4}");
    }

    public async Task PersistTelemetryAsync(
        Guid workflowInstanceId,
        LlmCompletionResult telemetry,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(telemetry);

        var directory = Path.Combine(_artifactRoot, workflowInstanceId.ToString("D"));
        Directory.CreateDirectory(directory);

        var telemetryPath = Path.Combine(directory, "telemetry.json");
        if (File.Exists(telemetryPath))
        {
            return;
        }

        var payload = new WorkflowTelemetryDocument(
            Model: telemetry.Model,
            PromptTokens: telemetry.PromptTokens,
            CompletionTokens: telemetry.CompletionTokens,
            TotalTokens: telemetry.TotalTokens,
            DurationSeconds: telemetry.Duration.TotalSeconds,
            EstimatedCostUsd: telemetry.EstimatedCostUsd);

        var json = JsonSerializer.Serialize(payload, TelemetryJsonOptions);
        await File.WriteAllTextAsync(telemetryPath, json, cancellationToken).ConfigureAwait(false);
    }

    private static readonly JsonSerializerOptions TelemetryJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    private sealed record WorkflowTelemetryDocument(
        string Model,
        int PromptTokens,
        int CompletionTokens,
        int TotalTokens,
        double DurationSeconds,
        decimal? EstimatedCostUsd);
}
