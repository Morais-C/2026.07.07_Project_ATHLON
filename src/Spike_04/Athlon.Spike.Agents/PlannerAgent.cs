using Athlon.Spike.Contracts;

namespace Athlon.Spike.Agents;

/// <summary>
/// ChangeBundle (by id) → LoadAsync StructuredChange + CodeContext → validated ImplementationPlan.
/// Prompt is built from LoadAsync only — never from prior LLM completion text.
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

        var bundleArtifact = await _artifactStore.LoadAsync(context.InputArtifactId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException(
                $"Input artifact '{context.InputArtifactId}' was not found.");

        if (!string.Equals(bundleArtifact.Type, ArtifactTypes.ChangeBundle, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"PlannerAgent expects a ChangeBundle artifact, got '{bundleArtifact.Type}'.");
        }

        var bundle = ChangeBundle.Parse(bundleArtifact);

        var structuredChange = await _artifactStore.LoadAsync(bundle.StructuredChangeId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException(
                $"StructuredChange artifact '{bundle.StructuredChangeId}' was not found.");

        if (!string.Equals(structuredChange.Type, ArtifactTypes.StructuredChange, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"ChangeBundle.structuredChangeId must reference a StructuredChange, got '{structuredChange.Type}'.");
        }

        var codeContextArtifact = await _artifactStore.LoadAsync(bundle.CodeContextId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException(
                $"CodeContext artifact '{bundle.CodeContextId}' was not found.");

        if (!string.Equals(codeContextArtifact.Type, ArtifactTypes.CodeContext, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"ChangeBundle.codeContextId must reference a CodeContext, got '{codeContextArtifact.Type}'.");
        }

        var codeContext = CodeContext.Parse(codeContextArtifact);

        var (systemPrompt, userPrompt) = _promptComposer.Compose(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["STRUCTURED_CHANGE_JSON"] = ArtifactJson.Serialize(structuredChange),
            ["CODE_CONTEXT_JSON"] = ArtifactJson.Serialize(codeContextArtifact)
        });

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

        // Inject fixture / CodeContext linkage from store — not from LLM text (L4)
        var payloadJson = ImplementationPlan.MergeLinkage(
            validation.NormalizedJson,
            fixtureId: codeContext.FixtureId,
            codeContextArtifactId: codeContextArtifact.Id,
            entryProject: codeContext.EntryProject,
            targetFramework: codeContext.TargetFramework);

        var outputArtifact = new Artifact(
            Id: Guid.NewGuid(),
            Type: ArtifactTypes.ImplementationPlan,
            Version: 1,
            Producer: AgentName,
            CreatedUtc: DateTime.UtcNow,
            WorkflowInstanceId: context.WorkflowInstanceId,
            PayloadJson: payloadJson);

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
