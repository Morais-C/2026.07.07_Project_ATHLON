using Athlon.Spike.Artifacts;
using Athlon.Spike.Contracts;
using Athlon.Spike.Workflow;

namespace Athlon.Spike.Tests;

public class PublisherTests
{
    [Fact]
    public async Task Publishes_and_builds_CodePackage()
    {
        var artifactRoot = CreateTempDir("artifacts");
        var publishRoot = CreateTempDir("publish");
        var store = new FileArtifactStore(artifactRoot);
        var workflowId = Guid.NewGuid();

        try
        {
            var codePackage = CodePackage.Create(
                HelloWorldPayload(),
                workflowId,
                producer: "CoderAgent");
            await store.SaveAsync(codePackage);

            var publisher = new Publisher(store, publishRoot: publishRoot);
            var result = await publisher.PublishAsync(workflowId, codePackage.Id);

            Assert.True(result.BuildSucceeded, result.BuildOutput);
            Assert.True(result.Succeeded, result.FailureMessage);
            Assert.True(File.Exists(result.ManifestPath));
            Assert.True(File.Exists(Path.Combine(result.PublishDirectory, "AthlonApp", "Program.cs")));
            Assert.True(File.Exists(Path.Combine(result.PublishDirectory, "AthlonApp", "AthlonApp.csproj")));

            var manifest = await File.ReadAllTextAsync(result.ManifestPath);
            Assert.Contains("deferred-to-tester-agent", manifest, StringComparison.Ordinal);
        }
        finally
        {
            Cleanup(artifactRoot);
            Cleanup(publishRoot);
        }
    }

    [Fact]
    public async Task Fails_when_publish_directory_already_exists()
    {
        var artifactRoot = CreateTempDir("artifacts");
        var publishRoot = CreateTempDir("publish");
        var store = new FileArtifactStore(artifactRoot);
        var workflowId = Guid.NewGuid();

        try
        {
            var codePackage = CodePackage.Create(
                HelloWorldPayload(),
                workflowId,
                producer: "CoderAgent");
            await store.SaveAsync(codePackage);

            Directory.CreateDirectory(Path.Combine(publishRoot, workflowId.ToString("D")));

            var publisher = new Publisher(store, publishRoot: publishRoot);
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => publisher.PublishAsync(workflowId, codePackage.Id));

            Assert.Contains("already exists", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Cleanup(artifactRoot);
            Cleanup(publishRoot);
        }
    }

    [Fact]
    public async Task Build_failure_is_recorded_in_manifest()
    {
        var artifactRoot = CreateTempDir("artifacts");
        var publishRoot = CreateTempDir("publish");
        var store = new FileArtifactStore(artifactRoot);
        var workflowId = Guid.NewGuid();

        try
        {
            var payload = new CodePackagePayload(
                Files:
                [
                    new CodePackageFile(
                        "AthlonApp/AthlonApp.csproj",
                        """
                        <Project Sdk="Microsoft.NET.Sdk">
                          <PropertyGroup>
                            <OutputType>Exe</OutputType>
                            <TargetFramework>net9.0</TargetFramework>
                            <ImplicitUsings>enable</ImplicitUsings>
                          </PropertyGroup>
                        </Project>
                        """),
                    new CodePackageFile(
                        "AthlonApp/Program.cs",
                        """
                        this is not valid C# !!!!
                        """)
                ],
                EntryProject: "AthlonApp/AthlonApp.csproj",
                TargetFramework: "net9.0",
                ExpectedOutputContains: "unused-for-publisher");

            var codePackage = CodePackage.Create(payload, workflowId, producer: "CoderAgent");
            await store.SaveAsync(codePackage);

            var publisher = new Publisher(store, publishRoot: publishRoot);
            var result = await publisher.PublishAsync(workflowId, codePackage.Id);

            Assert.False(result.BuildSucceeded);
            Assert.False(result.Succeeded);
            Assert.Contains("dotnet build failed", result.FailureMessage, StringComparison.OrdinalIgnoreCase);
            Assert.True(File.Exists(result.ManifestPath));
        }
        finally
        {
            Cleanup(artifactRoot);
            Cleanup(publishRoot);
        }
    }

    private static CodePackagePayload HelloWorldPayload() =>
        new(
            Files:
            [
                new CodePackageFile(
                    "AthlonApp/AthlonApp.csproj",
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
                    "AthlonApp/Program.cs",
                    """
                    Console.WriteLine("Hello from Athlon");
                    """)
            ],
            EntryProject: "AthlonApp/AthlonApp.csproj",
            TargetFramework: "net9.0",
            ExpectedOutputContains: "Hello from Athlon");

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
