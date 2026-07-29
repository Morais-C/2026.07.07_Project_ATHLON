using Athlon.Spike.Agents;
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
    public void ImplementationPlan_round_trips_through_json()
    {
        var workflowId = Guid.NewGuid();
        var payload = new ImplementationPlanPayload(
            Title: "Meal allowance",
            Summary: "Daily meal subsidy for staff",
            Tasks:
            [
                new ImplementationTask("T1", "Add allowance field to payroll", "2d")
            ],
            AcceptanceCriteria: ["Employees receive daily allowance"],
            TechnicalNotes: "Extend payroll module");

        var original = ImplementationPlan.Create(
            payload,
            workflowId,
            producer: PlannerAgent.AgentName);

        var json = ArtifactJson.Serialize(original);
        var restored = ArtifactJson.Deserialize(json);
        var parsed = ImplementationPlan.Parse(restored);

        Assert.Equal(original.Id, restored.Id);
        Assert.Equal(ArtifactTypes.ImplementationPlan, restored.Type);
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
            producer: AnalystAgent.AgentName);

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
    public void CodePackage_round_trips_through_json()
    {
        var workflowId = Guid.NewGuid();
        var payload = new CodePackagePayload(
            Files:
            [
                new CodePackageFile("Hello/Hello.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />"),
                new CodePackageFile("Hello/Program.cs", "Console.WriteLine(\"Hello\");")
            ],
            EntryProject: "Hello/Hello.csproj",
            TargetFramework: "net9.0",
            ExpectedOutputContains: "Hello");

        var original = CodePackage.Create(payload, workflowId, producer: CoderAgent.AgentName);

        var json = ArtifactJson.Serialize(original);
        var restored = ArtifactJson.Deserialize(json);
        var parsed = CodePackage.Parse(restored);

        Assert.Equal(original.Id, restored.Id);
        Assert.Equal(ArtifactTypes.CodePackage, restored.Type);
        Assert.Equal("Hello/Hello.csproj", parsed.EntryProject);
        Assert.Equal(2, parsed.Files.Count);
    }

    [Fact]
    public void CodePackage_Create_rejects_parent_directory_paths()
    {
        var payload = new CodePackagePayload(
            Files: [new CodePackageFile("../x/Program.cs", "x")],
            EntryProject: "../x/X.csproj",
            TargetFramework: "net9.0",
            ExpectedOutputContains: "x");

        var ex = Assert.Throws<ArgumentException>(() =>
            CodePackage.Create(payload, Guid.NewGuid(), producer: CoderAgent.AgentName));

        Assert.Contains("unsafe", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("Program.cs", true)]
    [InlineData("src/Program.cs", true)]
    [InlineData("../Program.cs", false)]
    [InlineData("/tmp/Program.cs", false)]
    [InlineData("C:\\temp\\Program.cs", false)]
    public void CodePackage_IsSafeRelativePath(string path, bool expected) =>
        Assert.Equal(expected, CodePackage.IsSafeRelativePath(path));

    [Fact]
    public void StructuredRequirement_Parse_rejects_wrong_artifact_type()
    {
        var wrong = BusinessRequirement.FromText("not structured", Guid.NewGuid());

        var ex = Assert.Throws<ArgumentException>(() => StructuredRequirement.Parse(wrong));
        Assert.Contains(ArtifactTypes.StructuredRequirement, ex.Message);
    }
}
