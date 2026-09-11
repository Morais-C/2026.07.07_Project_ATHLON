using Athlon.Spike.Agents;
using Athlon.Spike.Artifacts;
using Athlon.Spike.Contracts;
using Athlon.Spike.Llm;

namespace Athlon.Spike.Tests;

public class AnalystAgentTests
{
    private const string ValidStructuredChangeJson = """
        {
          "kind": "feature",
          "title": "Uppercase echo",
          "summary": "Echo the typed line in uppercase",
          "acceptanceCriteria": [
            "Prints the input transformed to UPPERCASE"
          ],
          "constraints": ["Single .NET 9 console", "Keep read → process → print"],
          "priority": "Medium",
          "suspectedPaths": ["MiniErp/Program.cs"]
        }
        """;

    private const string ValidBugfixStructuredChangeJson = """
        {
          "kind": "bugfix",
          "title": "Trim echo input",
          "summary": "Trim leading/trailing whitespace before echoing",
          "acceptanceCriteria": [
            "Echoed text has no leading or trailing whitespace"
          ],
          "constraints": ["Single .NET 9 console"],
          "priority": "High"
        }
        """;

    [Fact]
    public async Task Publishes_valid_StructuredChange_from_feature_ChangeRequest()
    {
        var root = CreateTempArtifactRoot();
        var store = new FileArtifactStore(root);
        var workflowId = Guid.NewGuid();

        try
        {
            var input = ChangeRequest.Create(
                new ChangeRequestPayload(
                    Kind: ChangeRequest.KindFeature,
                    Title: "Uppercase echo",
                    Description: "Print input in UPPERCASE"),
                workflowId);
            await store.SaveAsync(input);

            var agent = CreateAgent(store, new MockLLMProvider(new MockResponse(Content: ValidStructuredChangeJson)));
            var result = await agent.ExecuteAsync(new AgentExecutionContext(workflowId, input.Id));

            Assert.Equal(ArtifactTypes.StructuredChange, result.OutputArtifact.Type);
            Assert.Equal(AgentName, result.OutputArtifact.Producer);
            Assert.Equal("Uppercase echo", StructuredChange.Parse(result.OutputArtifact).Title);
            Assert.True(result.Telemetry.TotalTokens > 0);

            var loaded = await store.LoadAsync(result.OutputArtifact.Id);
            Assert.NotNull(loaded);
        }
        finally
        {
            Cleanup(root);
        }
    }

    [Fact]
    public async Task Publishes_valid_StructuredChange_from_bugfix_ChangeRequest()
    {
        var root = CreateTempArtifactRoot();
        var store = new FileArtifactStore(root);
        var workflowId = Guid.NewGuid();

        try
        {
            var input = ChangeRequest.Create(
                new ChangeRequestPayload(
                    Kind: ChangeRequest.KindBugfix,
                    Title: "Trim echo input",
                    Description: "Trailing spaces are echoed",
                    StepsToReproduce: "Type ' hi '",
                    ExpectedBehavior: "hi",
                    ActualBehavior: " hi "),
                workflowId);
            await store.SaveAsync(input);

            var agent = CreateAgent(
                store,
                new MockLLMProvider(new MockResponse(Content: ValidBugfixStructuredChangeJson)));
            var result = await agent.ExecuteAsync(new AgentExecutionContext(workflowId, input.Id));

            var parsed = StructuredChange.Parse(result.OutputArtifact);
            Assert.Equal(ChangeRequest.KindBugfix, parsed.Kind);
            Assert.Equal("Trim echo input", parsed.Title);
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
            var input = SampleFeatureRequest(workflowId);
            await store.SaveAsync(input);

            var agent = CreateAgent(store, new MockLLMProvider(
                new MockResponse(Content: """{ "text": "echo only — not structured" }"""),
                new MockResponse(Content: ValidStructuredChangeJson)));

            var result = await agent.ExecuteAsync(new AgentExecutionContext(workflowId, input.Id));

            Assert.Equal(ArtifactTypes.StructuredChange, result.OutputArtifact.Type);
            Assert.Equal(200, result.Telemetry.PromptTokens);
            Assert.Equal(100, result.Telemetry.CompletionTokens);
        }
        finally
        {
            Cleanup(root);
        }
    }

    [Fact]
    public async Task Fails_after_retry_without_publishing_output_artifact()
    {
        var root = CreateTempArtifactRoot();
        var store = new FileArtifactStore(root);
        var workflowId = Guid.NewGuid();

        try
        {
            var input = SampleFeatureRequest(workflowId);
            await store.SaveAsync(input);

            var agent = CreateAgent(store, new MockLLMProvider(
                new MockResponse(Content: "still not json"),
                new MockResponse(Content: """{ "title": "missing required fields" }""")));

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
    public async Task Aborts_out_of_bounds_without_publishing_StructuredChange()
    {
        var root = CreateTempArtifactRoot();
        var store = new FileArtifactStore(root);
        var workflowId = Guid.NewGuid();

        try
        {
            var input = ChangeRequest.Create(
                new ChangeRequestPayload(
                    Kind: ChangeRequest.KindFeature,
                    Title: "SaaS portal",
                    Description: "Build a multi-tenant SaaS web portal with SQL and OAuth"),
                workflowId);
            await store.SaveAsync(input);

            var agent = CreateAgent(store, new MockLLMProvider(new MockResponse(Content: """
                {
                  "inBounds": false,
                  "reason": "Requires web, database, and OAuth — not a single console app."
                }
                """)));

            var exception = await Assert.ThrowsAsync<OutOfBoundsException>(
                () => agent.ExecuteAsync(new AgentExecutionContext(workflowId, input.Id)));

            Assert.Contains("web", exception.Reason, StringComparison.OrdinalIgnoreCase);
            Assert.NotNull(exception.Telemetry);

            var files = Directory.GetFiles(root, "*.json", SearchOption.AllDirectories);
            Assert.Single(files);
            var loadedInput = await store.LoadAsync(input.Id);
            Assert.NotNull(loadedInput);
            Assert.Equal(ArtifactTypes.ChangeRequest, loadedInput.Type);
        }
        finally
        {
            Cleanup(root);
        }
    }

    [Fact]
    public void TryGetOutOfBoundsReason_detects_abort_shape()
    {
        Assert.True(AnalystAgent.TryGetOutOfBoundsReason(
            """{ "inBounds": false, "reason": "Needs a database." }""",
            out var reason));
        Assert.Equal("Needs a database.", reason);

        Assert.False(AnalystAgent.TryGetOutOfBoundsReason(
            ValidStructuredChangeJson,
            out _));
    }

    private static AnalystAgent CreateAgent(FileArtifactStore store, MockLLMProvider llm) =>
        new(
            llm,
            store,
            SpikeTestPaths.AnalystPromptTemplate,
            SpikeTestPaths.StructuredChangeSchema);

    private static Artifact SampleFeatureRequest(Guid workflowId) =>
        ChangeRequest.Create(
            new ChangeRequestPayload(
                Kind: ChangeRequest.KindFeature,
                Title: "Uppercase echo",
                Description: "Print input in UPPERCASE"),
            workflowId);

    private static string CreateTempArtifactRoot() =>
        Path.Combine(Path.GetTempPath(), "athlon-spike-tests", Guid.NewGuid().ToString("D"));

    private static void Cleanup(string root)
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private const string AgentName = AnalystAgent.AgentName;
}
