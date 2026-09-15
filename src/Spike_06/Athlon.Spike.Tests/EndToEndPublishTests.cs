using System.Text.Json;
using Athlon.Spike.Agents;
using Athlon.Spike.Artifacts;
using Athlon.Spike.Contracts;
using Athlon.Spike.Llm;
using Athlon.Spike.Workflow;

namespace Athlon.Spike.Tests;

/// <summary>
/// Spike_06 thesis end to end via rest-api-v1 pack: ChangeRequest → Analyst → CodeContext → Planner → Coder → Applier,
/// ending in a Publish folder that passes all proof gates.
/// </summary>
public class EndToEndPublishTests
{
    [Fact]
    public async Task ChangeRequest_reaches_a_Publish_folder_that_passes_all_proof_gates()
    {
        var pack = MiniErpTestFixtures.Pack;
        var artifactRoot = CreateTempDir("artifacts");
        var publishRoot = CreateTempDir("publish");
        var store = new FileArtifactStore(artifactRoot);
        var runner = new WorkflowRunner(store, artifactRoot: artifactRoot);
        var builder = CodeContextBuilder.FromArchetypePack(store, pack);

        var llm = new MockLLMProvider(
            new MockResponse(Content: MiniErpTestFixtures.ValidStructuredChangeJson),
            new MockResponse(Content: MiniErpTestFixtures.ValidImplementationPlanJson),
            new MockResponse(Content: MiniErpTestFixtures.ValidPatchPackageJson()));

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
                MiniErpTestFixtures.SampleChangeRequest(),
                CancellationToken.None);

            var structured = (await analyst.ExecuteAsync(
                new AgentExecutionContext(instance.Id, inputArtifact.Id))).OutputArtifact;

            var codeContext = await builder.BuildAndPublishAsync(
                instance.Id,
                fixtureId: pack.Baseline.FixtureId,
                fixtureRoot: pack.Baseline.FixtureRoot,
                entryProject: MiniErpTestFixtures.EntryProject);

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
            Assert.True(result.OpenapiConsistencySucceeded, result.OpenapiConsistencyOutput);
            Assert.True(result.ContractTestsSucceeded, result.ContractTestOutput);
            Assert.True(result.Succeeded, result.FailureMessage);
            Assert.Equal(
                Path.TrimEndingDirectorySeparator(Path.GetFullPath(Path.Combine(publishRoot, instance.Id.ToString("D")))),
                Path.TrimEndingDirectorySeparator(Path.GetFullPath(result.PublishDirectory)));
            Assert.True(File.Exists(Path.Combine(result.PublishDirectory, "MiniErp", "MiniErp.csproj")));
            Assert.True(File.Exists(Path.Combine(result.PublishDirectory, "MiniErp", "Program.cs")));
            Assert.True(File.Exists(Path.Combine(result.PublishDirectory, "openapi.yaml")));

            var publishedProgram = await File.ReadAllTextAsync(
                Path.Combine(result.PublishDirectory, "MiniErp", "Program.cs"));
            Assert.Contains("Spike_06 test", publishedProgram, StringComparison.Ordinal);

            var manifest = await File.ReadAllTextAsync(result.ManifestPath);
            Assert.Contains("\"openapiConsistencySucceeded\": true", manifest, StringComparison.Ordinal);
            Assert.Contains("\"contractTestsSucceeded\": true", manifest, StringComparison.Ordinal);
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
        var pack = MiniErpTestFixtures.Pack;
        var artifactRoot = CreateTempDir("artifacts");
        var publishRoot = CreateTempDir("publish");
        var store = new FileArtifactStore(artifactRoot);
        var runner = new WorkflowRunner(store, artifactRoot: artifactRoot);

        var llm = new MockLLMProvider(new MockResponse(Content: """
            {
              "inBounds": false,
              "reason": "Requires SQL persistence and JWT auth — out of scope for rest-api-v1."
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
        var pack = MiniErpTestFixtures.Pack;
        var artifactRoot = CreateTempDir("artifacts");
        var publishRoot = CreateTempDir("publish");
        var store = new FileArtifactStore(artifactRoot);
        var runner = new WorkflowRunner(store, artifactRoot: artifactRoot);
        // mini-erp-v1 loads 4 files with default extensions — cap at 3 to force abort
        var builder = new CodeContextBuilder(store, maxFilesAllowed: 3);

        var llm = new MockLLMProvider(new MockResponse(Content: MiniErpTestFixtures.ValidStructuredChangeJson));
        var analyst = new AnalystAgent(
            llm, store, pack.Analyst.PromptPath, pack.Analyst.OutputSchemaPath);

        try
        {
            var (instance, inputArtifact) = await runner.StartAndSaveChangeRequestAsync(
                "ChangeRequestToPublish",
                MiniErpTestFixtures.SampleChangeRequest(),
                CancellationToken.None);

            await analyst.ExecuteAsync(new AgentExecutionContext(instance.Id, inputArtifact.Id));

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                builder.BuildAndPublishAsync(
                    instance.Id,
                    fixtureId: pack.Baseline.FixtureId,
                    fixtureRoot: pack.Baseline.FixtureRoot,
                    entryProject: MiniErpTestFixtures.EntryProject));

            Assert.Contains("file cap", ex.Message, StringComparison.OrdinalIgnoreCase);
            Assert.False(Directory.Exists(Path.Combine(publishRoot, instance.Id.ToString("D"))));
        }
        finally
        {
            Cleanup(artifactRoot);
            Cleanup(publishRoot);
        }
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
