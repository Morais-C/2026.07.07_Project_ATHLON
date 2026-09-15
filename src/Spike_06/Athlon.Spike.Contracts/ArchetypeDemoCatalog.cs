using System.Text.Json;

namespace Athlon.Spike.Contracts;

/// <summary>
/// Loads demo ChangeRequests from an archetype pack catalog (demos/change-requests.json).
/// </summary>
public static class ArchetypeDemoCatalog
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static IReadOnlyList<ArchetypeDemoEntry> Load(string demosFilePath)
    {
        if (string.IsNullOrWhiteSpace(demosFilePath))
        {
            throw new ArchetypePackException("Demo catalog path is required.");
        }

        if (!File.Exists(demosFilePath))
        {
            throw new ArchetypePackException($"Demo catalog not found: '{demosFilePath}'.");
        }

        DemoCatalogDocument document;
        try
        {
            var json = File.ReadAllText(demosFilePath);
            document = JsonSerializer.Deserialize<DemoCatalogDocument>(json, JsonOptions)
                ?? throw new ArchetypePackException($"Demo catalog is empty: '{demosFilePath}'.");
        }
        catch (JsonException ex)
        {
            throw new ArchetypePackException($"Demo catalog JSON is invalid: '{demosFilePath}'.", ex);
        }

        if (document.Demos is null || document.Demos.Count == 0)
        {
            throw new ArchetypePackException($"Demo catalog has no entries: '{demosFilePath}'.");
        }

        return document.Demos
            .Select(d => new ArchetypeDemoEntry(
                Id: RequireNonEmpty(d.Id, "demo.id", demosFilePath),
                Kind: RequireNonEmpty(d.Kind, "demo.kind", demosFilePath),
                Title: RequireNonEmpty(d.Title, "demo.title", demosFilePath),
                Description: RequireNonEmpty(d.Description, "demo.description", demosFilePath),
                SuspectedPaths: d.SuspectedPaths,
                StepsToReproduce: d.StepsToReproduce,
                ExpectedBehavior: d.ExpectedBehavior,
                ActualBehavior: d.ActualBehavior))
            .ToList();
    }

    public static ArchetypeDemoEntry ResolveActive(IReadOnlyList<ArchetypeDemoEntry> demos, string? demoId)
    {
        if (demos.Count == 0)
        {
            throw new ArchetypePackException("Demo catalog has no entries.");
        }

        if (string.IsNullOrWhiteSpace(demoId))
        {
            return demos[0];
        }

        var match = demos.FirstOrDefault(d =>
            string.Equals(d.Id, demoId.Trim(), StringComparison.OrdinalIgnoreCase));

        if (match is null)
        {
            var available = string.Join(", ", demos.Select(d => d.Id));
            throw new ArchetypePackException(
                $"Unknown demo id '{demoId.Trim()}'. Available demos: {available}.");
        }

        return match;
    }

    public static ChangeRequestPayload ToChangeRequest(ArchetypeDemoEntry demo) =>
        new(
            Kind: demo.Kind,
            Title: demo.Title,
            Description: demo.Description,
            StepsToReproduce: demo.StepsToReproduce,
            ExpectedBehavior: demo.ExpectedBehavior,
            ActualBehavior: demo.ActualBehavior,
            SuspectedPaths: demo.SuspectedPaths);

    private static string RequireNonEmpty(string? value, string fieldName, string contextPath)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArchetypePackException(
                $"Demo catalog '{contextPath}' is missing required field '{fieldName}'.");
        }

        return value.Trim();
    }

    private sealed class DemoCatalogDocument
    {
        public List<DemoEntryDocument>? Demos { get; set; }
    }

    private sealed class DemoEntryDocument
    {
        public string? Id { get; set; }

        public string? Kind { get; set; }

        public string? Title { get; set; }

        public string? Description { get; set; }

        public List<string>? SuspectedPaths { get; set; }

        public string? StepsToReproduce { get; set; }

        public string? ExpectedBehavior { get; set; }

        public string? ActualBehavior { get; set; }
    }
}

public sealed record ArchetypeDemoEntry(
    string Id,
    string Kind,
    string Title,
    string Description,
    IReadOnlyList<string>? SuspectedPaths,
    string? StepsToReproduce,
    string? ExpectedBehavior,
    string? ActualBehavior);
