using System.Text.Json;
using Athlon.Spike.Contracts;

namespace Athlon.Spike.Agents;

/// <summary>
/// ImplementationPlan (by id) → LoadAsync CodeContext via plan linkage → validated PatchPackage.
/// Prompt is built from LoadAsync only — never from a Planner completion string.
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

        var planArtifact = await _artifactStore.LoadAsync(context.InputArtifactId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException(
                $"Input artifact '{context.InputArtifactId}' was not found.");

        if (!string.Equals(planArtifact.Type, ArtifactTypes.ImplementationPlan, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"CoderAgent expects an ImplementationPlan artifact, got '{planArtifact.Type}'.");
        }

        var plan = ImplementationPlan.Parse(planArtifact);
        if (!Guid.TryParse(plan.CodeContextArtifactId, out var codeContextId) || codeContextId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "ImplementationPlan.codeContextArtifactId must be a non-empty GUID.");
        }

        var codeContextArtifact = await _artifactStore.LoadAsync(codeContextId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException(
                $"CodeContext artifact '{codeContextId}' was not found.");

        if (!string.Equals(codeContextArtifact.Type, ArtifactTypes.CodeContext, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"ImplementationPlan.codeContextArtifactId must reference a CodeContext, got '{codeContextArtifact.Type}'.");
        }

        var (systemPrompt, userPrompt) = _promptComposer.Compose(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["IMPLEMENTATION_PLAN_JSON"] = ArtifactJson.Serialize(planArtifact),
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
                    $"CoderAgent failed schema validation after one retry: {errorSummary}");
            }
        }

        var outputArtifact = new Artifact(
            Id: Guid.NewGuid(),
            Type: ArtifactTypes.PatchPackage,
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

        return ValidatePatchPackagePaths(schemaOutcome.NormalizedJson);
    }

    private static ValidationOutcome ValidatePatchPackagePaths(string normalizedJson)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<PatchPackagePayload>(
                normalizedJson,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            if (payload is null)
            {
                return ValidationOutcome.Invalid(normalizedJson, ["PatchPackage payload is missing or invalid."]);
            }

            var pathErrors = PatchPackage.ValidatePaths(payload);
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
