using Athlon.Spike.Agents;
using Athlon.Spike.Artifacts;
using Athlon.Spike.Contracts;
using Athlon.Spike.Llm;
using Athlon.Spike.Workflow;

namespace Athlon.Spike.Tests;

/// <summary>
/// Phase 1 exit: in-bounds ChangeRequest → StructuredChange + CodeContext; OOB / over-cap fail fast.
/// </summary>
public class Phase1ChangeChainTests
{
    [Fact]
    public async Task In_bounds_ChangeRequest_yields_StructuredChange_and_CodeContext()
    {
        var root = CreateTempArtifactRoot();
        var store = new FileArtifactStore(root);
        var runner = new WorkflowRunner(store, artifactRoot: root);
        var builder = new CodeContextBuilder(store);

        var llm = new MockLLMProvider(new MockResponse(Content: MiniErpTestFixtures.ValidStructuredChangeJson));
        var analyst = new AnalystAgent(
            llm, store, SpikeTestPaths.AnalystPromptTemplate, SpikeTestPaths.StructuredChangeSchema);

        try
        {
            var (instance, inputArtifact) = await runner.StartAndSaveChangeRequestAsync(
                "ChangeRequestToCodeContext",
                MiniErpTestFixtures.SampleChangeRequest(),
                CancellationToken.None);

            var structured = (await analyst.ExecuteAsync(
                new AgentExecutionContext(instance.Id, inputArtifact.Id))).OutputArtifact;

            var codeContext = await builder.BuildAndPublishAsync(
                instance.Id,
                fixtureId: MiniErpTestFixtures.FixtureId,
                fixtureRoot: MiniErpTestFixtures.FixtureRoot,
                entryProject: MiniErpTestFixtures.EntryProject);

            Assert.Equal(ArtifactTypes.ChangeRequest, inputArtifact.Type);
            Assert.Equal(ArtifactTypes.StructuredChange, structured.Type);
            Assert.Equal(ArtifactTypes.CodeContext, codeContext.Type);

            Assert.Equal("Add health comment", StructuredChange.Parse(structured).Title);
            Assert.Equal(4, CodeContext.Parse(codeContext).Files.Count);
        }
        finally
        {
            Cleanup(root);
        }
    }

    [Fact]
    public async Task Out_of_bounds_ChangeRequest_aborts_without_StructuredChange_or_CodeContext()
    {
        var root = CreateTempArtifactRoot();
        var store = new FileArtifactStore(root);
        var runner = new WorkflowRunner(store, artifactRoot: root);

        var llm = new MockLLMProvider(new MockResponse(Content: """
            {
              "inBounds": false,
              "reason": "Requires SQL persistence and JWT auth — out of scope for rest-api-v1."
            }
            """));

        var analyst = new AnalystAgent(
            llm, store, SpikeTestPaths.AnalystPromptTemplate, SpikeTestPaths.StructuredChangeSchema);

        try
        {
            var (instance, inputArtifact) = await runner.StartAndSaveChangeRequestAsync(
                "ChangeRequestToCodeContext",
                new ChangeRequestPayload(
                    Kind: ChangeRequest.KindFeature,
                    Title: "SaaS portal",
                    Description: "Build a multi-tenant SaaS web portal with SQL"),
                CancellationToken.None);

            await Assert.ThrowsAsync<OutOfBoundsException>(
                () => analyst.ExecuteAsync(new AgentExecutionContext(instance.Id, inputArtifact.Id)));

            var files = Directory.GetFiles(root, "*.json", SearchOption.AllDirectories);
            Assert.Single(files);
            var loaded = await store.LoadAsync(inputArtifact.Id);
            Assert.NotNull(loaded);
            Assert.Equal(ArtifactTypes.ChangeRequest, loaded.Type);
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
