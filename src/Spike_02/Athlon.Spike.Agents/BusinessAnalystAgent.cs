using Athlon.Spike.Contracts;

namespace Athlon.Spike.Agents;

/// <summary>
/// Raw BusinessRequirement → validated StructuredRequirement artifact.
/// Publish only after schema OK — no chat handoff to the next agent.
/// </summary>
public sealed class BusinessAnalystAgent : IAgent
{
    public const string AgentName = "BusinessAnalystAgent";

    private readonly ILLMProvider _llmProvider;
    private readonly IArtifactStore _artifactStore;
    private readonly PromptComposer _promptComposer;
    private readonly JsonSchemaValidator _validator;

    public BusinessAnalystAgent(
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
                $"BusinessAnalystAgent expects a BusinessRequirement artifact, got '{inputArtifact.Type}'.");
        }

        var inputArtifactJson = ArtifactJson.Serialize(inputArtifact);
        var (systemPrompt, userPrompt) = _promptComposer.Compose(inputArtifactJson);

        var completions = new List<LlmCompletionResult>();
        var validation = await CompleteAndValidateAsync(systemPrompt, userPrompt, completions, cancellationToken)
            .ConfigureAwait(false);

        // Same rigor as Developer: 1 retry with validation errors echoed
        if (!validation.IsValid)
        {
            var retryPrompt = _promptComposer.AppendValidationFeedback(userPrompt, validation.Errors);
            validation = await CompleteAndValidateAsync(systemPrompt, retryPrompt, completions, cancellationToken)
                .ConfigureAwait(false);

            if (!validation.IsValid)
            {
                var errorSummary = string.Join("; ", validation.Errors);
                throw new InvalidOperationException(
                    $"BusinessAnalystAgent failed schema validation after one retry: {errorSummary}");
            }
        }

        // Only publish once schema is valid
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
