using System.Text.RegularExpressions;
using Json.Schema;

namespace Athlon.Spike.Agents;

public sealed class JsonSchemaValidator
{
    private readonly JsonSchema _schema;

    public JsonSchemaValidator(string schemaPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaPath);

        if (!File.Exists(schemaPath))
        {
            throw new FileNotFoundException($"JSON schema not found at '{schemaPath}'.", schemaPath);
        }

        var schemaJson = File.ReadAllText(schemaPath);
        _schema = JsonSchema.FromText(schemaJson)
            ?? throw new InvalidOperationException($"Failed to load JSON schema from '{schemaPath}'.");
    }

    public ValidationOutcome Validate(string rawContent)
    {
        var json = StripJsonFences(rawContent);

        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(json);
            var result = _schema.Evaluate(
                document.RootElement,
                new EvaluationOptions { OutputFormat = OutputFormat.List });

            if (result.IsValid)
            {
                return ValidationOutcome.Valid(json);
            }

            var errors = CollectErrors(result);
            if (errors.Count == 0)
            {
                errors.Add("JSON does not match the required schema.");
            }

            return ValidationOutcome.Invalid(json, errors);
        }
        catch (System.Text.Json.JsonException ex)
        {
            return ValidationOutcome.Invalid(json, [$"JSON syntax error: {ex.Message}"]);
        }
    }

    internal static string StripJsonFences(string content)
    {
        ArgumentNullException.ThrowIfNull(content);

        var trimmed = content.Trim();

        var fenced = Regex.Match(
            trimmed,
            @"^```(?:json)?\s*([\s\S]*?)\s*```$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        if (fenced.Success)
        {
            return fenced.Groups[1].Value.Trim();
        }

        return trimmed;
    }

    private static List<string> CollectErrors(EvaluationResults result)
    {
        var errors = new List<string>();
        CollectErrors(result, errors);
        return errors;
    }

    private static void CollectErrors(EvaluationResults result, List<string> errors)
    {
        if (result.Errors is not null)
        {
            foreach (var error in result.Errors.Values)
            {
                errors.Add(error);
            }
        }

        if (result.Details is not null)
        {
            foreach (var detail in result.Details)
            {
                CollectErrors(detail, errors);
            }
        }
    }
}

public sealed record ValidationOutcome(bool IsValid, string NormalizedJson, IReadOnlyList<string> Errors)
{
    public static ValidationOutcome Valid(string normalizedJson) =>
        new(true, normalizedJson, Array.Empty<string>());

    public static ValidationOutcome Invalid(string normalizedJson, IReadOnlyList<string> errors) =>
        new(false, normalizedJson, errors);
}
