using System.Text.Json;
using Athlon.Spike.Agents;
using Athlon.Spike.Artifacts;
using Athlon.Spike.Contracts;
using Athlon.Spike.Llm;
using Athlon.Spike.Workflow;

namespace Athlon.Spike.Tests;

/// <summary>
/// Spike_05 thesis end to end via console-v1 pack: ChangeRequest → Analyst → CodeContext → Planner → Coder → Applier,
/// ending in a Publish folder that compiles. Functional run checks belong to a future Tester agent (L11).
/// </summary>
public class EndToEndPublishTests
{
    private const string ValidStructuredChangeJson = """
        {
          "kind": "feature",
          "title": "Uppercase echo",
          "summary": "Echo the typed line in uppercase",
          "acceptanceCriteria": ["Prints the input transformed to UPPERCASE"],
          "constraints": ["Single .NET 9 console", "Keep read → process → print"],
          "priority": "Medium",
          "suspectedPaths": ["Echo/Program.cs"]
        }
        """;

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

    /// <summary>
    /// Exact unified diff against checked-in fixtures/echo-v1/Echo/Program.cs.
    /// </summary>
    private const string UppercaseEchoDiff =
        """
        --- a/Echo/Program.cs
        +++ b/Echo/Program.cs
        @@ -1,4 +1,4 @@
         // Spike_04 fixture baseline: read a line, echo it back (read → process → print).
         Console.Write("Enter text: ");
         var input = Console.ReadLine() ?? string.Empty;
        -Console.WriteLine(input);
        +Console.WriteLine(input.ToUpperInvariant());
        """;

    [Fact]
    public async Task ChangeRequest_reaches_a_Publish_folder_that_builds()
    {
        var pack = SpikeTestPaths.ConsoleV1Pack;
        var artifactRoot = CreateTempDir("artifacts");
        var publishRoot = CreateTempDir("publish");
        var store = new FileArtifactStore(artifactRoot);
        var runner = new WorkflowRunner(store, artifactRoot: artifactRoot);
        var builder = CodeContextBuilder.FromArchetypePack(store, pack);

        var llm = new MockLLMProvider(
            new MockResponse(Content: ValidStructuredChangeJson),
            new MockResponse(Content: ValidImplementationPlanJson),
            new MockResponse(Content: UppercasePatchPackageJson()));

        var analyst = new AnalystAgent(
            llm, store, pack.Analyst.PromptPath, pack.Analyst.OutputSchemaPath);
        var planner = new PlannerAgent(
            llm, store, pack.Planner.PromptPath, pack.Planner.OutputSchemaPath);
        var coder = new CoderAgent(
            llm, store, pack.Coder.PromptPath, pack.Coder.OutputSchemaPath);
        var applier = new Applier(store, publishRoot: publishRoot);

        try
        {
            var (instance, inputArtifact) = await runner.StartAndSaveChangeRequestAsync(
                "ChangeRequestToPublish",
                new ChangeRequestPayload(
                    Kind: ChangeRequest.KindFeature,
                    Title: "Uppercase echo",
                    Description: "Print input in UPPERCASE",
                    SuspectedPaths: ["Echo/Program.cs"]),
                CancellationToken.None);

            var structured = (await analyst.ExecuteAsync(
                new AgentExecutionContext(instance.Id, inputArtifact.Id))).OutputArtifact;

            var codeContext = await builder.BuildAndPublishAsync(
                instance.Id,
                fixtureId: pack.Baseline.FixtureId,
                fixtureRoot: pack.Baseline.FixtureRoot,
                entryProject: "Echo/Echo.csproj");

            var bundle = await runner.SaveChangeBundleAsync(
                instance.Id, structured.Id, codeContext.Id, CancellationToken.None);

            var plan = (await planner.ExecuteAsync(
                new AgentExecutionContext(instance.Id, bundle.Id))).OutputArtifact;
            var patchPackage = (await coder.ExecuteAsync(
                new AgentExecutionContext(instance.Id, plan.Id))).OutputArtifact;

            Assert.Equal(ArtifactTypes.StructuredChange, structured.Type);
            Assert.Equal(ArtifactTypes.CodeContext, codeContext.Type);
            Assert.Equal(ArtifactTypes.ChangeBundle, bundle.Type);
            Assert.Equal(ArtifactTypes.ImplementationPlan, plan.Type);
            Assert.Equal(ArtifactTypes.PatchPackage, patchPackage.Type);

            // Prompt/schema paths must come from the pack, not spike-root duplicates
            Assert.StartsWith(pack.PackRoot, pack.Analyst.PromptPath, StringComparison.OrdinalIgnoreCase);
            Assert.StartsWith(pack.PackRoot, pack.Planner.PromptPath, StringComparison.OrdinalIgnoreCase);
            Assert.StartsWith(pack.PackRoot, pack.Coder.PromptPath, StringComparison.OrdinalIgnoreCase);

            var result = await applier.ApplyAsync(
                instance.Id,
                patchPackage.Id,
                expectedFixtureId: pack.Baseline.FixtureId,
                fixtureRoot: pack.Baseline.FixtureRoot);

            Assert.True(result.ApplySucceeded, result.FailureMessage);
            Assert.True(result.BuildSucceeded, result.BuildOutput);
            Assert.True(result.Succeeded, result.FailureMessage);
            Assert.Equal(
                Path.TrimEndingDirectorySeparator(Path.GetFullPath(Path.Combine(publishRoot, instance.Id.ToString("D")))),
                Path.TrimEndingDirectorySeparator(Path.GetFullPath(result.PublishDirectory)));
            Assert.True(File.Exists(Path.Combine(result.PublishDirectory, "Echo", "Echo.csproj")));
            Assert.True(File.Exists(Path.Combine(result.PublishDirectory, "Echo", "Program.cs")));

            var publishedProgram = await File.ReadAllTextAsync(
                Path.Combine(result.PublishDirectory, "Echo", "Program.cs"));
            Assert.Contains("ToUpperInvariant()", publishedProgram, StringComparison.Ordinal);

            var manifest = await File.ReadAllTextAsync(result.ManifestPath);
            Assert.Contains("deferred-to-tester-agent", manifest, StringComparison.Ordinal);
            Assert.Contains(patchPackage.Id.ToString("D"), manifest, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Cleanup(artifactRoot);
            Cleanup(publishRoot);
        }
    }

    [Fact]
    public async Task Out_of_bounds_ChangeRequest_aborts_before_any_PatchPackage_or_Publish_folder()
    {
        var pack = SpikeTestPaths.ConsoleV1Pack;
        var artifactRoot = CreateTempDir("artifacts");
        var publishRoot = CreateTempDir("publish");
        var store = new FileArtifactStore(artifactRoot);
        var runner = new WorkflowRunner(store, artifactRoot: artifactRoot);

        var llm = new MockLLMProvider(new MockResponse(Content: """
            {
              "inBounds": false,
              "reason": "Requires a web front end and a SQL database — not a single console app."
            }
            """));

        var analyst = new AnalystAgent(
            llm, store, pack.Analyst.PromptPath, pack.Analyst.OutputSchemaPath);

        try
        {
            var (instance, inputArtifact) = await runner.StartAndSaveChangeRequestAsync(
                "ChangeRequestToPublish",
                new ChangeRequestPayload(
                    Kind: ChangeRequest.KindFeature,
                    Title: "SaaS portal",
                    Description: "Build a multi-tenant SaaS web portal with SQL and OAuth"),
                CancellationToken.None);

            await Assert.ThrowsAsync<OutOfBoundsException>(
                () => analyst.ExecuteAsync(new AgentExecutionContext(instance.Id, inputArtifact.Id)));

            // Chain stops at the Analyst: only the ChangeRequest was ever published
            var artifactFiles = Directory.GetFiles(artifactRoot, "*.json", SearchOption.AllDirectories);
            Assert.Single(artifactFiles);
            var onlyArtifact = await store.LoadAsync(inputArtifact.Id);
            Assert.NotNull(onlyArtifact);
            Assert.Equal(ArtifactTypes.ChangeRequest, onlyArtifact.Type);

            Assert.False(Directory.Exists(Path.Combine(publishRoot, instance.Id.ToString("D"))));
        }
        finally
        {
            Cleanup(artifactRoot);
            Cleanup(publishRoot);
        }
    }

    [Fact]
    public async Task Over_cap_CodeContext_aborts_without_Publish_folder()
    {
        var pack = SpikeTestPaths.ConsoleV1Pack;
        var artifactRoot = CreateTempDir("artifacts");
        var publishRoot = CreateTempDir("publish");
        var store = new FileArtifactStore(artifactRoot);
        var runner = new WorkflowRunner(store, artifactRoot: artifactRoot);
        // echo-v1 has 2 source files — cap at 1 to force abort (below pack default)
        var builder = new CodeContextBuilder(store, maxFilesAllowed: 1);

        var llm = new MockLLMProvider(new MockResponse(Content: ValidStructuredChangeJson));
        var analyst = new AnalystAgent(
            llm, store, pack.Analyst.PromptPath, pack.Analyst.OutputSchemaPath);

        try
        {
            var (instance, inputArtifact) = await runner.StartAndSaveChangeRequestAsync(
                "ChangeRequestToPublish",
                new ChangeRequestPayload(
                    Kind: ChangeRequest.KindFeature,
                    Title: "Uppercase echo",
                    Description: "Print input in UPPERCASE"),
                CancellationToken.None);

            await analyst.ExecuteAsync(new AgentExecutionContext(instance.Id, inputArtifact.Id));

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                builder.BuildAndPublishAsync(
                    instance.Id,
                    fixtureId: pack.Baseline.FixtureId,
                    fixtureRoot: pack.Baseline.FixtureRoot,
                    entryProject: "Echo/Echo.csproj"));

            Assert.Contains("file cap", ex.Message, StringComparison.OrdinalIgnoreCase);
            Assert.False(Directory.Exists(Path.Combine(publishRoot, instance.Id.ToString("D"))));
        }
        finally
        {
            Cleanup(artifactRoot);
            Cleanup(publishRoot);
        }
    }

    private static string UppercasePatchPackageJson()
    {
        var payload = new PatchPackagePayload(
            FixtureId: "echo-v1",
            Changes:
            [
                new PatchFileChange(
                    "Echo/Program.cs",
                    PatchPackage.OperationModify,
                    UppercaseEchoDiff.Replace("\r\n", "\n"))
            ],
            EntryProject: "Echo/Echo.csproj",
            TargetFramework: "net9.0",
            Summary: "Uppercase echoed line");

        return JsonSerializer.Serialize(
            payload,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }

    private static string CreateTempDir(string label) =>
        Path.Combine(Path.GetTempPath(), "athlon-spike-tests", label, Guid.NewGuid().ToString("D"));

    private static void Cleanup(string root)
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
