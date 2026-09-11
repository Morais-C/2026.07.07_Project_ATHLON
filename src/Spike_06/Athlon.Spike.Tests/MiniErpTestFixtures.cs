using System.Text.Json;
using Athlon.Spike.Contracts;

namespace Athlon.Spike.Tests;

/// <summary>
/// Shared mini-erp-v1 / rest-api-v1 test constants and mock payloads for the adapted test suite.
/// </summary>
internal static class MiniErpTestFixtures
{
    public const string FixtureId = "mini-erp-v1";
    public const string EntryProject = "MiniErp/MiniErp.csproj";
    public const string ProgramPath = "MiniErp/Program.cs";
    public const string OpenApiPath = "openapi.yaml";

    public static string FixtureRoot => SpikeTestPaths.MiniErpV1FixtureRoot;

    public static ArchetypePack Pack => SpikeTestPaths.RestApiV1Pack;

    /// <summary>Exact unified diff against checked-in fixtures/mini-erp-v1/MiniErp/Program.cs.</summary>
    public const string AddCommentDiff =
        """
        --- a/MiniErp/Program.cs
        +++ b/MiniErp/Program.cs
        @@ -1,4 +1,5 @@
        +// Spike_06 test: minimal in-bounds REST change
         var builder = WebApplication.CreateBuilder(args);
         var app = builder.Build();
         
         app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
        """;

    public const string ValidStructuredChangeJson = """
        {
          "kind": "feature",
          "title": "Add health comment",
          "summary": "Add a comment to Program.cs without changing API behavior",
          "acceptanceCriteria": ["GET /health still returns status ok"],
          "constraints": ["Minimal API only", "In-memory only", "No new NuGet"],
          "priority": "Medium",
          "suspectedPaths": ["MiniErp/Program.cs"]
        }
        """;

    public const string ValidImplementationPlanJson = """
        {
          "title": "Add health comment",
          "summary": "Add a comment to Program.cs",
          "tasks": [
            {
              "id": "T1",
              "description": "Add comment line to MiniErp/Program.cs",
              "estimate": "15m"
            }
          ],
          "acceptanceCriteria": [
            "GET /health still returns status ok"
          ],
          "technicalNotes": "Minimal edit to MiniErp/Program.cs",
          "intendedPaths": ["MiniErp/Program.cs"]
        }
        """;

    public static string ValidPatchPackageJson() =>
        JsonSerializer.Serialize(
            new PatchPackagePayload(
                FixtureId,
                [
                    new PatchFileChange(
                        ProgramPath,
                        PatchPackage.OperationModify,
                        AddCommentDiff.Replace("\r\n", "\n"))
                ],
                EntryProject,
                "net9.0",
                "Add comment to Program.cs"),
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

    public static ChangeRequestPayload SampleChangeRequest() =>
        new(
            Kind: ChangeRequest.KindFeature,
            Title: "Add health comment",
            Description: "Add a comment to Program.cs without changing API behavior.",
            SuspectedPaths: [ProgramPath]);
}
