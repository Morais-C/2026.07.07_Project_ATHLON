using Athlon.Spike.Agents;
using Athlon.Spike.Contracts;

namespace Athlon.Spike.Tests;

public class PatchPackageSchemaTests
{
    [Fact]
    public void Accepts_valid_modify_package()
    {
        var json = """
            {
              "fixtureId": "echo-v1",
              "changes": [
                {
                  "path": "Echo/Program.cs",
                  "operation": "modify",
                  "unifiedDiff": "--- a/Echo/Program.cs\n+++ b/Echo/Program.cs\n@@ -1 +1 @@\n-a\n+b\n"
                }
              ],
              "entryProject": "Echo/Echo.csproj",
              "targetFramework": "net9.0",
              "summary": "Uppercase"
            }
            """;

        var outcome = CreateValidator().Validate(json);
        Assert.True(outcome.IsValid);
    }

    [Fact]
    public void Rejects_unknown_operation()
    {
        var json = """
            {
              "fixtureId": "echo-v1",
              "changes": [
                {
                  "path": "Echo/Program.cs",
                  "operation": "replace",
                  "unifiedDiff": "--- a/Echo/Program.cs\n+++ b/Echo/Program.cs\n@@ -1 +1 @@\n-a\n+b\n"
                }
              ],
              "entryProject": "Echo/Echo.csproj",
              "targetFramework": "net9.0",
              "summary": "Bad op"
            }
            """;

        var outcome = CreateValidator().Validate(json);
        Assert.False(outcome.IsValid);
    }

    [Fact]
    public void PatchPackage_Create_rejects_parent_directory_paths()
    {
        var payload = new PatchPackagePayload(
            FixtureId: "echo-v1",
            Changes:
            [
                new PatchFileChange("../evil/Program.cs", PatchPackage.OperationModify, "--- a/x\n+++ b/x\n")
            ],
            EntryProject: "../evil/Evil.csproj",
            TargetFramework: "net9.0",
            Summary: "nope");

        var ex = Assert.Throws<ArgumentException>(() =>
            PatchPackage.Create(payload, Guid.NewGuid(), producer: CoderAgent.AgentName));

        Assert.Contains("unsafe", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PatchPackage_round_trips_through_json()
    {
        var workflowId = Guid.NewGuid();
        var payload = new PatchPackagePayload(
            FixtureId: "echo-v1",
            Changes:
            [
                new PatchFileChange(
                    "Echo/Program.cs",
                    PatchPackage.OperationModify,
                    "--- a/Echo/Program.cs\n+++ b/Echo/Program.cs\n@@ -1 +1 @@\n-a\n+b\n")
            ],
            EntryProject: "Echo/Echo.csproj",
            TargetFramework: "net9.0",
            Summary: "Uppercase");

        var original = PatchPackage.Create(payload, workflowId, producer: CoderAgent.AgentName);
        var restored = ArtifactJson.Deserialize(ArtifactJson.Serialize(original));
        var parsed = PatchPackage.Parse(restored);

        Assert.Equal(ArtifactTypes.PatchPackage, restored.Type);
        Assert.Equal("echo-v1", parsed.FixtureId);
        Assert.Single(parsed.Changes);
    }

    [Fact]
    public void ChangeBundle_round_trips_through_json()
    {
        var workflowId = Guid.NewGuid();
        var structuredId = Guid.NewGuid();
        var codeContextId = Guid.NewGuid();

        var original = ChangeBundle.Create(
            new ChangeBundlePayload(structuredId, codeContextId),
            workflowId);
        var restored = ArtifactJson.Deserialize(ArtifactJson.Serialize(original));
        var parsed = ChangeBundle.Parse(restored);

        Assert.Equal(ArtifactTypes.ChangeBundle, restored.Type);
        Assert.Equal(structuredId, parsed.StructuredChangeId);
        Assert.Equal(codeContextId, parsed.CodeContextId);
    }

    private static JsonSchemaValidator CreateValidator() =>
        new(SpikeTestPaths.PatchPackageSchema);
}
