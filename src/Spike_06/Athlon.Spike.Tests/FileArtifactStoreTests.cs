using Athlon.Spike.Artifacts;
using Athlon.Spike.Contracts;

namespace Athlon.Spike.Tests;

public class FileArtifactStoreTests
{
    [Fact]
    public async Task Save_and_load_round_trip()
    {
        var root = Path.Combine(Path.GetTempPath(), "athlon-spike-tests", Guid.NewGuid().ToString("D"));
        var store = new FileArtifactStore(root);

        try
        {
            var workflowId = Guid.NewGuid();
            var artifact = BusinessRequirement.FromText("Meal allowance", workflowId);

            await store.SaveAsync(artifact);

            var loaded = await store.LoadAsync(artifact.Id);

            Assert.NotNull(loaded);
            Assert.Equal(artifact.Id, loaded.Id);
            Assert.Equal(artifact.Type, loaded.Type);
            Assert.Equal(artifact.WorkflowInstanceId, loaded.WorkflowInstanceId);
            Assert.Equal("Meal allowance", BusinessRequirement.GetText(loaded));

            var expectedPath = Path.Combine(root, workflowId.ToString("D"), $"{artifact.Id:D}.json");
            Assert.True(File.Exists(expectedPath));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Save_throws_when_artifact_already_exists()
    {
        var root = Path.Combine(Path.GetTempPath(), "athlon-spike-tests", Guid.NewGuid().ToString("D"));
        var store = new FileArtifactStore(root);

        try
        {
            var workflowId = Guid.NewGuid();
            var artifact = BusinessRequirement.FromText("Immutable artifact", workflowId);

            await store.SaveAsync(artifact);

            await Assert.ThrowsAsync<InvalidOperationException>(() => store.SaveAsync(artifact));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}
