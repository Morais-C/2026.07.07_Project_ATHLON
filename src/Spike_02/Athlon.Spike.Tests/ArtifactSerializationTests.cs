using Athlon.Spike.Contracts;

namespace Athlon.Spike.Tests;

public class ArtifactSerializationTests
{
    [Fact]
    public void BusinessRequirement_round_trips_through_json()
    {
        var workflowId = Guid.NewGuid();
        var original = BusinessRequirement.FromText("Meal allowance for employees", workflowId);

        var json = ArtifactJson.Serialize(original);
        var restored = ArtifactJson.Deserialize(json);

        Assert.Equal(original.Id, restored.Id);
        Assert.Equal(original.Type, restored.Type);
        Assert.Equal(original.WorkflowInstanceId, restored.WorkflowInstanceId);
        Assert.Equal("Meal allowance for employees", BusinessRequirement.GetText(restored));
    }

    [Fact]
    public void ImplementationArtifact_round_trips_through_json()
    {
        var workflowId = Guid.NewGuid();
        var payload = new ImplementationArtifactPayload(
            Title: "Meal allowance",
            Summary: "Daily meal subsidy for staff",
            Tasks:
            [
                new ImplementationTask("T1", "Add allowance field to payroll", "2d")
            ],
            AcceptanceCriteria: ["Employees receive daily allowance"],
            TechnicalNotes: "Extend payroll module");

        var original = ImplementationArtifact.Create(
            payload,
            workflowId,
            producer: "DeveloperAgent");

        var json = ArtifactJson.Serialize(original);
        var restored = ArtifactJson.Deserialize(json);
        var parsed = ImplementationArtifact.Parse(restored);

        Assert.Equal(original.Id, restored.Id);
        Assert.Equal("Meal allowance", parsed.Title);
        Assert.Single(parsed.Tasks);
        Assert.Equal("T1", parsed.Tasks[0].Id);
    }

    [Fact]
    public void StructuredRequirement_round_trips_through_json()
    {
        var workflowId = Guid.NewGuid();
        var payload = new StructuredRequirementPayload(
            Title: "Employee Daily Meal Allowance",
            Actors: ["Employee", "Payroll", "Manager"],
            Goal: "Provide a fixed daily meal allowance on working days",
            AcceptanceCriteriaDraft:
            [
                "Eligible employees receive a fixed daily allowance on working days",
                "Payroll shows the allowance as a separate line item"
            ],
            Constraints: ["On-site working days only", "Configurable rate"],
            Priority: "Medium");

        var original = StructuredRequirement.Create(
            payload,
            workflowId,
            producer: "BusinessAnalystAgent");

        var json = ArtifactJson.Serialize(original);
        var restored = ArtifactJson.Deserialize(json);
        var parsed = StructuredRequirement.Parse(restored);

        Assert.Equal(original.Id, restored.Id);
        Assert.Equal(ArtifactTypes.StructuredRequirement, restored.Type);
        Assert.Equal("Employee Daily Meal Allowance", parsed.Title);
        Assert.Equal(3, parsed.Actors.Count);
        Assert.Equal("Medium", parsed.Priority);
        Assert.Equal(2, parsed.AcceptanceCriteriaDraft.Count);
    }

    [Fact]
    public void StructuredRequirement_Parse_rejects_wrong_artifact_type()
    {
        var wrong = BusinessRequirement.FromText("not structured", Guid.NewGuid());

        var ex = Assert.Throws<ArgumentException>(() => StructuredRequirement.Parse(wrong));
        Assert.Contains(ArtifactTypes.StructuredRequirement, ex.Message);
    }
}
