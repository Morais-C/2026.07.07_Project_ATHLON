using Athlon.Spike.Agents;

namespace Athlon.Spike.Tests;

public class StructuredRequirementSchemaTests
{
    [Fact]
    public void Valid_payload_passes_schema()
    {
        var json = """
            {
              "title": "Employee Daily Meal Allowance",
              "actors": ["Employee", "Manager"],
              "goal": "Cover lunch expenses on working days",
              "acceptanceCriteriaDraft": [
                "Eligible employees receive a fixed daily allowance"
              ],
              "constraints": ["On-site working days only"],
              "priority": "Medium"
            }
            """;

        var outcome = CreateValidator().Validate(json);

        Assert.True(outcome.IsValid);
        Assert.Empty(outcome.Errors);
    }

    [Fact]
    public void Thin_text_echo_fails_schema()
    {
        // BA must not publish Spike_01-style { "text": "..." }
        var json = """{ "text": "As an employee I want meal allowance" }""";

        var outcome = CreateValidator().Validate(json);

        Assert.False(outcome.IsValid);
        Assert.NotEmpty(outcome.Errors);
    }

    [Fact]
    public void Missing_required_field_fails_schema()
    {
        var json = """
            {
              "title": "Meal Allowance",
              "actors": ["Employee"],
              "goal": "Cover lunch",
              "acceptanceCriteriaDraft": ["Gets allowance"],
              "constraints": ["Working days"]
            }
            """;

        var outcome = CreateValidator().Validate(json);

        Assert.False(outcome.IsValid);
        Assert.NotEmpty(outcome.Errors);
    }

    private static JsonSchemaValidator CreateValidator() =>
        new(SpikeTestPaths.StructuredRequirementSchema);
}
