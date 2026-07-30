using Athlon.Spike.Contracts;

namespace Athlon.Spike.Agents;

/// <summary>
/// StructuredRequirement (by id) → validated ImplementationPlan artifact.
/// Prompt is built from LoadAsync(id) only — never from an Analyst completion string.
/// </summary>
public sealed class PlannerAgent : IAgent
{
    public const string AgentName = "PlannerAgent";

    private readonly ILLMProvider _llmProvider;
    private readonly IArtifactStore _artifactStore;
    private readonly PromptComposer _promptComposer;
    private readonly JsonSchemaValidator _validator;

    public PlannerAgent(
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

        // Handoff is by artifact id — load from store, do not accept Analyst chat text
        var inputArtifact = await _artifactStore.LoadAsync(context.InputArtifactId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException(
                $"Input artifact '{context.InputArtifactId}' was not found.");

        if (!string.Equals(inputArtifact.Type, ArtifactTypes.StructuredRequirement, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"PlannerAgent expects a StructuredRequirement artifact, got '{inputArtifact.Type}'.");
        }

        var inputArtifactJson = ArtifactJson.Serialize(inputArtifact);
        var (systemPrompt, userPrompt) = _promptComposer.Compose(inputArtifactJson);

        var completions = new List<LlmCompletionResult>();
        var validation = await CompleteAndValidateAsync(systemPrompt, userPrompt, completions, cancellationToken)
            .ConfigureAwait(false);

        if (!validation.IsValid)
        {
            var retryPrompt = _promptComposer.AppendValidationFeedback(userPrompt, validation.Errors);
            validation = await CompleteAndValidateAsync(systemPrompt, retryPrompt, completions, cancellationToken)
                .ConfigureAwait(false);

            if (!validation.IsValid)
            {
                var errorSummary = string.Join("; ", validation.Errors);
                throw new InvalidOperationException(
                    $"PlannerAgent failed schema validation after one retry: {errorSummary}");
            }
        }

        var outputArtifact = new Artifact(
            Id: Guid.NewGuid(),
            Type: ArtifactTypes.ImplementationPlan,
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

    private async Task<ValidationOutcome> CompleteAndValidateAsync(
        string systemPrompt,
        string userPrompt,
        List<LlmCompletionResult> completions,
        CancellationToken cancellationToken)
    {
        var completion = await _llmProvider.CompleteAsync(systemPrompt, userPrompt, cancellationToken)
            .ConfigureAwait(false);

        completions.Add(completion);
        return _validator.Validate(completion.Content);
    }
}
