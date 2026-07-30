using Athlon.Spike.Agents;
using Athlon.Spike.Artifacts;
using Athlon.Spike.Contracts;
using Athlon.Spike.Llm;

namespace Athlon.Spike.Tests;

public class CoderAgentTests
{
    private const string ValidCodePackageJson = """
        {
          "files": [
            {
              "path": "Calculator/Calculator.csproj",
              "content": "<Project Sdk=\"Microsoft.NET.Sdk\">\\n  <PropertyGroup>\\n    <OutputType>Exe</OutputType>\\n    <TargetFramework>net9.0</TargetFramework>\\n  </PropertyGroup>\\n</Project>"
            },
            {
              "path": "Calculator/Program.cs",
              "content": "Console.WriteLine(\\\"Result: 42\\\");"
            }
          ],
          "entryProject": "Calculator/Calculator.csproj",
          "targetFramework": "net9.0",
          "expectedOutputContains": "Result: 42"
        }
        """;

    [Fact]
    public async Task Publishes_valid_CodePackage_from_mock_llm_response()
    {
        var root = CreateTempArtifactRoot();
        var store = new FileArtifactStore(root);
        var workflowId = Guid.NewGuid();

        try
        {
            var input = CreateImplementationPlan(workflowId);
            await store.SaveAsync(input);

            var agent = CreateAgent(store, new MockLLMProvider(new MockResponse(Content: ValidCodePackageJson)));
            var result = await agent.ExecuteAsync(new AgentExecutionContext(workflowId, input.Id));

            Assert.Equal(ArtifactTypes.CodePackage, result.OutputArtifact.Type);
            Assert.Equal(AgentName, result.OutputArtifact.Producer);

            var package = CodePackage.Parse(result.OutputArtifact);
            Assert.Equal("Calculator/Calculator.csproj", package.EntryProject);
            Assert.Equal("net9.0", package.TargetFramework);
            Assert.Equal("Result: 42", package.ExpectedOutputContains);
            Assert.Equal(2, package.Files.Count);
            Assert.True(result.Telemetry.TotalTokens > 0);
        }
        finally
        {
            Cleanup(root);
        }
    }

    [Fact]
    public async Task Retries_once_on_invalid_json_then_publishes()
    {
        var root = CreateTempArtifactRoot();
        var store = new FileArtifactStore(root);
        var workflowId = Guid.NewGuid();

        try
        {
            var input = CreateImplementationPlan(workflowId);
            await store.SaveAsync(input);

            var agent = CreateAgent(store, new MockLLMProvider(
                new MockResponse(Content: "not valid json"),
                new MockResponse(Content: ValidCodePackageJson)));

            var result = await agent.ExecuteAsync(new AgentExecutionContext(workflowId, input.Id));

            Assert.Equal(ArtifactTypes.CodePackage, result.OutputArtifact.Type);
            Assert.Equal(200, result.Telemetry.PromptTokens);
            Assert.Equal(100, result.Telemetry.CompletionTokens);
        }
        finally
        {
            Cleanup(root);
        }
    }

    [Fact]
    public async Task Fails_after_retry_without_publishing_CodePackage()
    {
        var root = CreateTempArtifactRoot();
        var store = new FileArtifactStore(root);
        var workflowId = Guid.NewGuid();

        try
        {
            var input = CreateImplementationPlan(workflowId);
            await store.SaveAsync(input);

            var agent = CreateAgent(store, new MockLLMProvider(
                new MockResponse(Content: "still not json"),
                new MockResponse(Content: """{ "files": [] }""")));

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => agent.ExecuteAsync(new AgentExecutionContext(workflowId, input.Id)));

            Assert.Contains("after one retry", exception.Message, StringComparison.OrdinalIgnoreCase);

            var files = Directory.GetFiles(root, "*.json", SearchOption.AllDirectories);
            Assert.Single(files);
        }
        finally
        {
            Cleanup(root);
        }
    }

    [Fact]
    public async Task Rejects_unsafe_paths_and_does_not_publish()
    {
        var root = CreateTempArtifactRoot();
        var store = new FileArtifactStore(root);
        var workflowId = Guid.NewGuid();

        const string unsafePackage = """
            {
              "files": [
                {
                  "path": "../evil/Program.cs",
                  "content": "Console.WriteLine(\"nope\");"
                }
              ],
              "entryProject": "../evil/Evil.csproj",
              "targetFramework": "net9.0",
              "expectedOutputContains": "nope"
            }
            """;

        try
        {
            var input = CreateImplementationPlan(workflowId);
            await store.SaveAsync(input);

            var agent = CreateAgent(store, new MockLLMProvider(
                new MockResponse(Content: unsafePackage),
                new MockResponse(Content: unsafePackage)));

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => agent.ExecuteAsync(new AgentExecutionContext(workflowId, input.Id)));

            Assert.Contains("after one retry", exception.Message, StringComparison.OrdinalIgnoreCase);

            var files = Directory.GetFiles(root, "*.json", SearchOption.AllDirectories);
            Assert.Single(files);
        }
        finally
        {
            Cleanup(root);
        }
    }

    [Fact]
    public async Task Rejects_StructuredRequirement_input_type()
    {
        var root = CreateTempArtifactRoot();
        var store = new FileArtifactStore(root);
        var workflowId = Guid.NewGuid();

        try
        {
            var wrong = StructuredRequirement.Create(
                new StructuredRequirementPayload(
                    Title: "Echo",
                    Actors: ["User"],
                    Goal: "Echo input",
                    AcceptanceCriteriaDraft: ["Prints input"],
                    Constraints: ["Console only"],
                    Priority: "Low"),
                workflowId,
                producer: AnalystAgent.AgentName);
            await store.SaveAsync(wrong);

            var agent = CreateAgent(store, new MockLLMProvider(new MockResponse(Content: ValidCodePackageJson)));

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => agent.ExecuteAsync(new AgentExecutionContext(workflowId, wrong.Id)));

            Assert.Contains(ArtifactTypes.ImplementationPlan, exception.Message);
            Assert.Contains(ArtifactTypes.StructuredRequirement, exception.Message);
        }
        finally
        {
            Cleanup(root);
        }
    }

    private static Artifact CreateImplementationPlan(Guid workflowId) =>
        ImplementationPlan.Create(
            new ImplementationPlanPayload(
                Title: "Console Calculator",
                Summary: "Read two numbers and an operator, print the result",
                Tasks: [new ImplementationTask("T1", "Implement Program.cs calculator loop", "1h")],
                AcceptanceCriteria: ["Prints computed result"],
                TechnicalNotes: "net9 console, no NuGet"),
            workflowId,
            producer: PlannerAgent.AgentName);

    private static CoderAgent CreateAgent(FileArtifactStore store, MockLLMProvider llm) =>
        new(
            llm,
            store,
            SpikeTestPaths.CoderPromptTemplate,
            SpikeTestPaths.CodePackageSchema);

    private static string CreateTempArtifactRoot() =>
        Path.Combine(Path.GetTempPath(), "athlon-spike-tests", Guid.NewGuid().ToString("D"));

    private static void Cleanup(string root)
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private const string AgentName = CoderAgent.AgentName;
}
