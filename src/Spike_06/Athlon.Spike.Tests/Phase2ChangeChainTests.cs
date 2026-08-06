using Athlon.Spike.Agents;
using Athlon.Spike.Artifacts;
using Athlon.Spike.Contracts;
using Athlon.Spike.Llm;
using Athlon.Spike.Workflow;

namespace Athlon.Spike.Tests;

/// <summary>
/// Phase 2 exit: ChangeBundle → Planner → Coder publishes PatchPackage; invalid → retry; two failures → no publish.
/// </summary>
public class Phase2ChangeChainTests
{
    private const string ValidImplementationPlanJson = """
        {
          "title": "Uppercase echo",
          "summary": "Echo typed line in UPPERCASE",
          "tasks": [
            {
              "id": "T1",
              "description": "Change Program.cs to uppercase the input before printing",
              "estimate": "30m"
            }
          ],
          "acceptanceCriteria": [
            "Prints the input transformed to UPPERCASE"
          ],
          "technicalNotes": "Minimal edit to Echo/Program.cs",
          "intendedPaths": ["Echo/Program.cs"]
        }
        """;

    private const string ValidPatchPackageJson = """
        {
          "fixtureId": "echo-v1",
          "changes": [
            {
              "path": "Echo/Program.cs",
              "operation": "modify",
              "unifiedDiff": "--- a/Echo/Program.cs\n+++ b/Echo/Program.cs\n@@ -1,5 +1,5 @@\n-Console.WriteLine(line);\n+Console.WriteLine(line.ToUpperInvariant());\n"
            }
          ],
          "entryProject": "Echo/Echo.csproj",
          "targetFramework": "net9.0",
          "summary": "Uppercase echoed line"
        }
        """;

    [Fact]
    public async Task ChangeBundle_yields_ImplementationPlan_and_PatchPackage()
    {
        var root = CreateTempArtifactRoot();
        var store = new FileArtifactStore(root);
        var runner = new WorkflowRunner(store, artifactRoot: root);
        var builder = new CodeContextBuilder(store);

        var llm = new MockLLMProvider(
            new MockResponse(Content: ValidImplementationPlanJson),
            new MockResponse(Content: ValidPatchPackageJson));

        var planner = new PlannerAgent(
            llm, store, SpikeTestPaths.PlannerPromptTemplate, SpikeTestPaths.ImplementationPlanSchema);
        var coder = new CoderAgent(
            llm, store, SpikeTestPaths.CoderPromptTemplate, SpikeTestPaths.PatchPackageSchema);

        try
        {
            var workflowId = Guid.NewGuid();

            var structured = StructuredChange.Create(
                new StructuredChangePayload(
                    Kind: ChangeRequest.KindFeature,
                    Title: "Uppercase echo",
                    Summary: "Echo UPPERCASE",
                    AcceptanceCriteria: ["Prints UPPERCASE"],
                    Constraints: ["Console only"],
                    Priority: "Medium",
                    SuspectedPaths: ["Echo/Program.cs"]),
                workflowId,
                producer: AnalystAgent.AgentName);
            await store.SaveAsync(structured);

            var codeContext = await builder.BuildAndPublishAsync(
                workflowId,
                fixtureId: "echo-v1",
                fixtureRoot: SpikeTestPaths.EchoV1FixtureRoot,
                entryProject: "Echo/Echo.csproj");

            var bundle = await runner.SaveChangeBundleAsync(
                workflowId, structured.Id, codeContext.Id, CancellationToken.None);

            var plan = (await planner.ExecuteAsync(new AgentExecutionContext(workflowId, bundle.Id))).OutputArtifact;
            var patch = (await coder.ExecuteAsync(new AgentExecutionContext(workflowId, plan.Id))).OutputArtifact;

            Assert.Equal(ArtifactTypes.ChangeBundle, bundle.Type);
            Assert.Equal(ArtifactTypes.ImplementationPlan, plan.Type);
            Assert.Equal(ArtifactTypes.PatchPackage, patch.Type);

            var planPayload = ImplementationPlan.Parse(plan);
            Assert.Equal(codeContext.Id.ToString("D"), planPayload.CodeContextArtifactId);
            Assert.Equal("echo-v1", planPayload.FixtureId);

            var package = PatchPackage.Parse(patch);
            Assert.Equal("echo-v1", package.FixtureId);
            Assert.Single(package.Changes);
            Assert.Equal(PatchPackage.OperationModify, package.Changes[0].Operation);
        }
        finally
        {
            Cleanup(root);
        }
    }

    [Fact]
    public async Task Invalid_Coder_response_twice_does_not_publish_PatchPackage()
    {
        var root = CreateTempArtifactRoot();
        var store = new FileArtifactStore(root);
        var workflowId = Guid.NewGuid();

        try
        {
            var builder = new CodeContextBuilder(store);
            var codeContext = await builder.BuildAndPublishAsync(
                workflowId,
                fixtureId: "echo-v1",
                fixtureRoot: SpikeTestPaths.EchoV1FixtureRoot,
                entryProject: "Echo/Echo.csproj");

            var plan = ImplementationPlan.Create(
                new ImplementationPlanPayload(
                    Title: "Uppercase echo",
                    Summary: "Echo UPPERCASE",
                    Tasks: [new ImplementationTask("T1", "Edit Program.cs", "30m")],
                    AcceptanceCriteria: ["Prints UPPERCASE"],
                    TechnicalNotes: "Minimal",
                    IntendedPaths: ["Echo/Program.cs"],
                    FixtureId: "echo-v1",
                    CodeContextArtifactId: codeContext.Id.ToString("D"),
                    EntryProject: "Echo/Echo.csproj",
                    TargetFramework: "net9.0"),
                workflowId,
                producer: PlannerAgent.AgentName);
            await store.SaveAsync(plan);

            var seedCount = Directory.GetFiles(root, "*.json", SearchOption.AllDirectories).Length;

            var coder = new CoderAgent(
                new MockLLMProvider(
                    new MockResponse(Content: "not json"),
                    new MockResponse(Content: """{ "fixtureId": "echo-v1" }""")),
                store,
                SpikeTestPaths.CoderPromptTemplate,
                SpikeTestPaths.PatchPackageSchema);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => coder.ExecuteAsync(new AgentExecutionContext(workflowId, plan.Id)));

            Assert.Contains("after one retry", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(seedCount, Directory.GetFiles(root, "*.json", SearchOption.AllDirectories).Length);
        }
        finally
        {
            Cleanup(root);
        }
    }

    private static string CreateTempArtifactRoot() =>
        Path.Combine(Path.GetTempPath(), "athlon-spike-tests", Guid.NewGuid().ToString("D"));

    private static void Cleanup(string root)
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
