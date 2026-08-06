namespace Athlon.Spike.Agents;

public sealed class PromptComposer
{
    private readonly string _template;

    public PromptComposer(string promptTemplatePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(promptTemplatePath);

        if (!File.Exists(promptTemplatePath))
        {
            throw new FileNotFoundException($"Prompt template not found at '{promptTemplatePath}'.", promptTemplatePath);
        }

        _template = File.ReadAllText(promptTemplatePath);
    }

    public (string SystemPrompt, string UserPrompt) Compose(string inputArtifactJson) =>
        Compose(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["INPUT_ARTIFACT_JSON"] = inputArtifactJson
        });

    public (string SystemPrompt, string UserPrompt) Compose(IReadOnlyDictionary<string, string> replacements)
    {
        ArgumentNullException.ThrowIfNull(replacements);

        var systemMarker = "=== SYSTEM ===";
        var userMarker = "=== USER ===";

        var systemIndex = _template.IndexOf(systemMarker, StringComparison.Ordinal);
        var userIndex = _template.IndexOf(userMarker, StringComparison.Ordinal);

        if (systemIndex < 0 || userIndex < 0 || userIndex <= systemIndex)
        {
            throw new InvalidOperationException(
                "Prompt template must contain '=== SYSTEM ===' and '=== USER ===' sections.");
        }

        var systemPrompt = _template[(systemIndex + systemMarker.Length)..userIndex].Trim();
        var userTemplate = _template[(userIndex + userMarker.Length)..].Trim();
        var userPrompt = userTemplate;

        foreach (var (key, value) in replacements)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            userPrompt = userPrompt.Replace($"{{{{{key}}}}}", value, StringComparison.Ordinal);
        }

        return (systemPrompt, userPrompt);
    }

    public string AppendValidationFeedback(string userPrompt, IReadOnlyList<string> errors)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userPrompt);
        ArgumentNullException.ThrowIfNull(errors);

        var errorBlock = string.Join(Environment.NewLine, errors.Select(error => $"- {error}"));

        return $"""
            {userPrompt}

            Previous response failed schema validation:
            {errorBlock}

            Return corrected JSON only.
            """;
    }
}
