using System.Text.Json;
using Athlon.Spike.Contracts;

namespace Athlon.Spike.Agents;

/// <summary>
/// ImplementationPlan (by id) → validated CodePackage artifact.
/// Prompt is built from LoadAsync(id) only — never from a Planner completion string.
/// </summary>
public sealed class CoderAgent : IAgent
{
    public const string AgentName = "CoderAgent";

    private readonly ILLMProvider _llmProvider;
    private readonly IArtifactStore _artifactStore;
    private readonly PromptComposer _promptComposer;
    private readonly JsonSchemaValidator _validator;

    public CoderAgent(
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

        if (!string.Equals(inputArtifact.Type, ArtifactTypes.ImplementationPlan, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"CoderAgent expects an ImplementationPlan artifact, got '{inputArtifact.Type}'.");
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
                    $"CoderAgent failed schema validation after one retry: {errorSummary}");
            }
        }

        var outputArtifact = new Artifact(
            Id: Guid.NewGuid(),
            Type: ArtifactTypes.CodePackage,
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
        var schemaOutcome = _validator.Validate(completion.Content);
        if (!schemaOutcome.IsValid)
        {
            return schemaOutcome;
        }

        // Extra path safety beyond schema pattern (absolute / '..')
        return ValidateCodePackagePaths(schemaOutcome.NormalizedJson);
    }

    private static ValidationOutcome ValidateCodePackagePaths(string normalizedJson)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<CodePackagePayload>(
                normalizedJson,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            if (payload is null)
            {
                return ValidationOutcome.Invalid(normalizedJson, ["CodePackage payload is missing or invalid."]);
            }

            var pathErrors = CodePackage.ValidatePaths(payload);
            if (pathErrors.Count > 0)
            {
                return ValidationOutcome.Invalid(normalizedJson, pathErrors);
            }

            return ValidationOutcome.Valid(normalizedJson);
        }
        catch (JsonException ex)
        {
            return ValidationOutcome.Invalid(normalizedJson, [$"JSON syntax error: {ex.Message}"]);
        }
    }
}
