using System.Text.Json;
using System.Text.Json.Serialization;

namespace Athlon.Spike.Contracts;

public sealed record ImplementationTask(string Id, string Description, string Estimate);

/// <summary>
/// Planner output for incremental change. LLM produces the planning fields;
/// PlannerAgent injects fixture/code-context linkage from the loaded ChangeBundle.
/// </summary>
public sealed record ImplementationPlanPayload(
    string Title,
    string Summary,
    IReadOnlyList<ImplementationTask> Tasks,
    IReadOnlyList<string> AcceptanceCriteria,
    string TechnicalNotes,
    IReadOnlyList<string> IntendedPaths,
    string FixtureId,
    string CodeContextArtifactId,
    string EntryProject,
    string TargetFramework);

public static class ImplementationPlan
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static Artifact Create(
        ImplementationPlanPayload payload,
        Guid workflowInstanceId,
        string producer,
        Guid? artifactId = null,
        int version = 1)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentException.ThrowIfNullOrWhiteSpace(producer);

        var payloadJson = JsonSerializer.Serialize(payload, JsonOptions);

        return new Artifact(
            Id: artifactId ?? Guid.NewGuid(),
            Type: ArtifactTypes.ImplementationPlan,
            Version: version,
            Producer: producer,
            CreatedUtc: DateTime.UtcNow,
            WorkflowInstanceId: workflowInstanceId,
            PayloadJson: payloadJson);
    }

    public static ImplementationPlanPayload Parse(Artifact artifact)
    {
        ArgumentNullException.ThrowIfNull(artifact);

        if (!string.Equals(artifact.Type, ArtifactTypes.ImplementationPlan, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Expected artifact type '{ArtifactTypes.ImplementationPlan}', got '{artifact.Type}'.",
                nameof(artifact));
        }

        return JsonSerializer.Deserialize<ImplementationPlanPayload>(artifact.PayloadJson, JsonOptions)
            ?? throw new InvalidOperationException("ImplementationPlan payload is missing or invalid.");
    }

    /// <summary>
    /// Merge LLM plan body with linkage fields from CodeContext / ChangeBundle.
    /// </summary>
    public static string MergeLinkage(
        string llmPlanJson,
        string fixtureId,
        Guid codeContextArtifactId,
        string entryProject,
        string targetFramework)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(llmPlanJson);
        ArgumentException.ThrowIfNullOrWhiteSpace(fixtureId);
        ArgumentException.ThrowIfNullOrWhiteSpace(entryProject);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetFramework);

        if (codeContextArtifactId == Guid.Empty)
        {
            throw new ArgumentException("codeContextArtifactId is required.", nameof(codeContextArtifactId));
        }

        using var document = JsonDocument.Parse(llmPlanJson);
        var root = document.RootElement;

        var tasks = new List<ImplementationTask>();
        foreach (var task in root.GetProperty("tasks").EnumerateArray())
        {
            tasks.Add(new ImplementationTask(
                task.GetProperty("id").GetString() ?? string.Empty,
                task.GetProperty("description").GetString() ?? string.Empty,
                task.GetProperty("estimate").GetString() ?? string.Empty));
        }

        var acceptance = root.GetProperty("acceptanceCriteria").EnumerateArray()
            .Select(static e => e.GetString() ?? string.Empty)
            .ToList();

        var intendedPaths = root.GetProperty("intendedPaths").EnumerateArray()
            .Select(static e => e.GetString() ?? string.Empty)
            .ToList();

        var payload = new ImplementationPlanPayload(
            Title: root.GetProperty("title").GetString() ?? string.Empty,
            Summary: root.GetProperty("summary").GetString() ?? string.Empty,
            Tasks: tasks,
            AcceptanceCriteria: acceptance,
            TechnicalNotes: root.TryGetProperty("technicalNotes", out var notes)
                ? notes.GetString() ?? string.Empty
                : string.Empty,
            IntendedPaths: intendedPaths,
            FixtureId: fixtureId,
            CodeContextArtifactId: codeContextArtifactId.ToString("D"),
            EntryProject: entryProject,
            TargetFramework: targetFramework);

        return JsonSerializer.Serialize(payload, JsonOptions);
    }
}
