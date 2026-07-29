using System.Text.Json;
using Athlon.Spike.Contracts;

namespace Athlon.Spike.Agents;

/// <summary>
/// Raw BusinessRequirement → bounds check → validated StructuredRequirement,
/// or OutOfBoundsException with no StructuredRequirement publish.
/// </summary>
public sealed class AnalystAgent : IAgent
{
    public const string AgentName = "AnalystAgent";

    private readonly ILLMProvider _llmProvider;
    private readonly IArtifactStore _artifactStore;
    private readonly PromptComposer _promptComposer;
    private readonly JsonSchemaValidator _validator;

    public AnalystAgent(
        ILLMProvider llmProvider,
        IArtifactStore artifactStore,
        string promptTemplatePath,
        string schemaPath)
    {
        _llmProvider = llmProvider ?? throw new ArgumentNullException(nameof(llmProvider));
        _artifactStore = artifactStore ?? throw new ArgumentNullException(nameof(artifactStore));
        _promptComposer = new PromptComposer(promptTemplatePath);
        _validator = new JsonSchemaValidator(schemaPath);
    }

    public string Name => AgentName;

    public async Task<AgentExecutionResult> ExecuteAsync(
        AgentExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var inputArtifact = await _artifactStore.LoadAsync(context.InputArtifactId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException(
                $"Input artifact '{context.InputArtifactId}' was not found.");

        if (!string.Equals(inputArtifact.Type, ArtifactTypes.BusinessRequirement, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"AnalystAgent expects a BusinessRequirement artifact, got '{inputArtifact.Type}'.");
        }

        var inputArtifactJson = ArtifactJson.Serialize(inputArtifact);
        var (systemPrompt, userPrompt) = _promptComposer.Compose(inputArtifactJson);

        var completions = new List<LlmCompletionResult>();
        var validation = await CompleteAndClassifyAsync(systemPrompt, userPrompt, completions, cancellationToken)
            .ConfigureAwait(false);

        // Same rigor as Planner: 1 retry with validation errors echoed (still may abort out-of-bounds)
        if (!validation.IsValid)
        {
            var retryPrompt = _promptComposer.AppendValidationFeedback(userPrompt, validation.Errors);
            validation = await CompleteAndClassifyAsync(systemPrompt, retryPrompt, completions, cancellationToken)
                .ConfigureAwait(false);

            if (!validation.IsValid)
            {
                var errorSummary = string.Join("; ", validation.Errors);
                throw new InvalidOperationException(
                    $"AnalystAgent failed schema validation after one retry: {errorSummary}");
            }
        }

        // Only publish once in-bounds StructuredRequirement schema is valid
        var outputArtifact = new Artifact(
            Id: Guid.NewGuid(),
            Type: ArtifactTypes.StructuredRequirement,
            Version: 1,
            Producer: AgentName,
            CreatedUtc: DateTime.UtcNow,
            WorkflowInstanceId: context.WorkflowInstanceId,
            PayloadJson: validation.NormalizedJson);

        await _artifactStore.SaveAsync(outputArtifact, cancellationToken).ConfigureAwait(false);

        return new AgentExecutionResult(
            OutputArtifact: outputArtifact,
            Telemetry: LlmTelemetryAggregator.Aggregate(completions));
    }

    private async Task<ValidationOutcome> CompleteAndClassifyAsync(
        string systemPrompt,
        string userPrompt,
        List<LlmCompletionResult> completions,
        CancellationToken cancellationToken)
    {
        var completion = await _llmProvider.CompleteAsync(systemPrompt, userPrompt, cancellationToken)
            .ConfigureAwait(false);

        completions.Add(completion);

        var json = JsonSchemaValidator.ExtractJsonPayload(completion.Content);
        if (TryGetOutOfBoundsReason(json, out var reason))
        {
            throw new OutOfBoundsException(reason, LlmTelemetryAggregator.Aggregate(completions));
        }

        return _validator.Validate(completion.Content);
    }

    /// <summary>
    /// Detects the abort shape { "inBounds": false, "reason": "..." }.
    /// </summary>
    public static bool TryGetOutOfBoundsReason(string json, out string reason)
    {
        reason = string.Empty;

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (!root.TryGetProperty("inBounds", out var inBounds) ||
                inBounds.ValueKind != JsonValueKind.False)
            {
                return false;
            }

            if (root.TryGetProperty("reason", out var reasonElement) &&
                reasonElement.ValueKind == JsonValueKind.String)
            {
                reason = reasonElement.GetString()?.Trim() ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                reason = "Need is outside Spike_03 console bounds.";
            }

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
