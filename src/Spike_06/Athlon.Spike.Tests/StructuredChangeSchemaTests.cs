using Athlon.Spike.Agents;

namespace Athlon.Spike.Tests;

public class StructuredChangeSchemaTests
{
    [Fact]
    public void Valid_feature_payload_passes_schema()
    {
        var json = """
            {
              "kind": "feature",
              "title": "Uppercase echo",
              "summary": "Echo input in uppercase",
              "acceptanceCriteria": ["Prints UPPERCASE input"],
              "constraints": ["Single .NET 9 console"],
              "priority": "Medium"
            }
            """;

        var outcome = CreateValidator().Validate(json);

        Assert.True(outcome.IsValid, string.Join("; ", outcome.Errors));
        Assert.Empty(outcome.Errors);
    }

    [Fact]
    public void Valid_bugfix_payload_passes_schema()
    {
        var json = """
            {
              "kind": "bugfix",
              "title": "Trim echo",
              "summary": "Trim whitespace before echo",
              "acceptanceCriteria": ["No leading/trailing spaces"],
              "constraints": ["Single .NET 9 console"],
              "priority": "High",
              "suspectedPaths": ["MiniErp/Program.cs"]
            }
            """;

        var outcome = CreateValidator().Validate(json);

        Assert.True(outcome.IsValid, string.Join("; ", outcome.Errors));
    }

    [Fact]
    public void Thin_text_echo_fails_schema()
    {
        var json = """{ "text": "Make echo uppercase" }""";

        var outcome = CreateValidator().Validate(json);

        Assert.False(outcome.IsValid);
        Assert.NotEmpty(outcome.Errors);
    }

    [Fact]
    public void Invalid_kind_fails_schema()
    {
        var json = """
            {
              "kind": "refactor",
              "title": "Cleanup",
              "summary": "Refactor echo",
              "acceptanceCriteria": ["Still echoes"],
              "constraints": ["net9"],
              "priority": "Low"
            }
            """;

        var outcome = CreateValidator().Validate(json);

        Assert.False(outcome.IsValid);
        Assert.NotEmpty(outcome.Errors);
    }

    private static JsonSchemaValidator CreateValidator() =>
        new(SpikeTestPaths.StructuredChangeSchema);
}
