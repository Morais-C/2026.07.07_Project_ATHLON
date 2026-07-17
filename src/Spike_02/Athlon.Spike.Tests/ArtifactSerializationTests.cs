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
}
