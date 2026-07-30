using System.Text.Json;
using Athlon.Spike.Agents;
using Athlon.Spike.Artifacts;
using Athlon.Spike.Contracts;
using Athlon.Spike.Llm;
using Athlon.Spike.Workflow;

namespace Athlon.Spike.Tests;

/// <summary>
/// Spike_03 thesis end to end: raw need → Analyst → Planner → Coder → Publisher,
/// ending in a Publish folder that compiles. Functional run checks belong to a future Tester agent (L10).
/// </summary>
public class EndToEndPublishTests
{
    private const string ValidStructuredRequirementJson = """
        {
          "title": "Console Greeter",
          "actors": ["User"],
          "goal": "Print a greeting for a name typed by the user",
          "acceptanceCriteriaDraft": ["Prints a greeting containing the typed name"],
          "constraints": ["Single .NET 9 console app"],
          "priority": "Low"
        }
        """;

    private const string ValidImplementationPlanJson = """
        {
          "title": "Console Greeter",
          "summary": "Read a name, print a greeting",
          "tasks": [
            {
              "id": "T1",
              "description": "Read a line and print a greeting",
              "estimate": "30m"
            }
          ],
          "acceptanceCriteria": ["Prints a greeting containing the typed name"],
          "technicalNotes": "net9.0 console, no NuGet packages"
        }
        """;

    [Fact(Skip = "Spike_04 Phase 4–5: full change chain → Applier Publish E2E.")]
    public async Task Business_need_reaches_a_Publish_folder_that_builds()
    {
        var artifactRoot = CreateTempDir("artifacts");
        var publishRoot = CreateTempDir("publish");
        var store = new FileArtifactStore(artifactRoot);
        var runner = new WorkflowRunner(store, artifactRoot: artifactRoot);

        var llm = new MockLLMProvider(
            new MockResponse(Content: ValidStructuredRequirementJson),
            new MockResponse(Content: ValidImplementationPlanJson),
            new MockResponse(Content: GreeterCodePackageJson()));

        var analyst = new AnalystAgent(
            llm, store, SpikeTestPaths.AnalystPromptTemplate, SpikeTestPaths.StructuredRequirementSchema);
        var planner = new PlannerAgent(
            llm, store, SpikeTestPaths.PlannerPromptTemplate, SpikeTestPaths.ImplementationPlanSchema);
        var coder = new CoderAgent(
            llm, store, SpikeTestPaths.CoderPromptTemplate, SpikeTestPaths.CodePackageSchema);
        var publisher = new Publisher(store, publishRoot: publishRoot);

        try
        {
            var (instance, inputArtifact) = await runner.StartAndSaveInputAsync(
                "BusinessNeedToPublish",
                "I want a console app that greets a person by the name they type",
                CancellationToken.None);

            var structured = (await analyst.ExecuteAsync(
                new AgentExecutionContext(instance.Id, inputArtifact.Id))).OutputArtifact;
            var plan = (await planner.ExecuteAsync(
                new AgentExecutionContext(instance.Id, structured.Id))).OutputArtifact;
            var codePackage = (await coder.ExecuteAsync(
                new AgentExecutionContext(instance.Id, plan.Id))).OutputArtifact;

            Assert.Equal(ArtifactTypes.StructuredRequirement, structured.Type);
            Assert.Equal(ArtifactTypes.ImplementationPlan, plan.Type);
            Assert.Equal(ArtifactTypes.CodePackage, codePackage.Type);

            var result = await publisher.PublishAsync(instance.Id, codePackage.Id);

            Assert.True(result.BuildSucceeded, result.BuildOutput);
            Assert.True(result.Succeeded, result.FailureMessage);
            Assert.Equal(
                Path.TrimEndingDirectorySeparator(Path.GetFullPath(Path.Combine(publishRoot, instance.Id.ToString("D")))),
                Path.TrimEndingDirectorySeparator(Path.GetFullPath(result.PublishDirectory)));
            Assert.True(File.Exists(Path.Combine(result.PublishDirectory, "Greeter", "Greeter.csproj")));
            Assert.True(File.Exists(Path.Combine(result.PublishDirectory, "Greeter", "Program.cs")));

            var manifest = await File.ReadAllTextAsync(result.ManifestPath);
            Assert.Contains("deferred-to-tester-agent", manifest, StringComparison.Ordinal);
            Assert.Contains(codePackage.Id.ToString("D"), manifest, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Cleanup(artifactRoot);
            Cleanup(publishRoot);
        }
    }

    [Fact(Skip = "Spike_04 Phase 4–5: OOB abort covered by AnalystAgentTests / Phase1ChangeChainTests.")]
    public async Task Out_of_bounds_need_aborts_before_any_CodePackage_or_Publish_folder()
    {
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
            llm, store, SpikeTestPaths.AnalystPromptTemplate, SpikeTestPaths.StructuredRequirementSchema);

        try
        {
            var (instance, inputArtifact) = await runner.StartAndSaveInputAsync(
                "BusinessNeedToPublish",
                "Build a multi-tenant SaaS web portal with SQL and OAuth",
                CancellationToken.None);

            await Assert.ThrowsAsync<OutOfBoundsException>(
                () => analyst.ExecuteAsync(new AgentExecutionContext(instance.Id, inputArtifact.Id)));

            // Chain stops at the Analyst: only the raw need was ever published
            var artifactFiles = Directory.GetFiles(artifactRoot, "*.json", SearchOption.AllDirectories);
            Assert.Single(artifactFiles);
            var onlyArtifact = await store.LoadAsync(inputArtifact.Id);
            Assert.NotNull(onlyArtifact);
            Assert.Equal(ArtifactTypes.BusinessRequirement, onlyArtifact.Type);

            Assert.False(Directory.Exists(Path.Combine(publishRoot, instance.Id.ToString("D"))));
        }
        finally
        {
            Cleanup(artifactRoot);
            Cleanup(publishRoot);
        }
    }

    // Built as JSON from the payload record so the C# source below stays readable
    private static string GreeterCodePackageJson()
    {
        var payload = new CodePackagePayload(
            Files:
            [
                new CodePackageFile(
                    "Greeter/Greeter.csproj",
                    """
                    <Project Sdk="Microsoft.NET.Sdk">
                      <PropertyGroup>
                        <OutputType>Exe</OutputType>
                        <TargetFramework>net9.0</TargetFramework>
                        <ImplicitUsings>enable</ImplicitUsings>
                        <Nullable>enable</Nullable>
                      </PropertyGroup>
                    </Project>
                    """),
                new CodePackageFile(
                    "Greeter/Program.cs",
                    """
                    Console.Write("Name: ");
                    var name = Console.ReadLine();
                    Console.WriteLine($"Hello, {(string.IsNullOrWhiteSpace(name) ? "stranger" : name)}!");
                    """)
            ],
            EntryProject: "Greeter/Greeter.csproj",
            TargetFramework: "net9.0",
            ExpectedOutputContains: "Hello,");

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
