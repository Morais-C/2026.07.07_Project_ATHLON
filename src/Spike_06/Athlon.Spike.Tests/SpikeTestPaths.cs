using Athlon.Spike.Contracts;

namespace Athlon.Spike.Tests;

/// <summary>
/// Test paths resolve through the loaded <c>rest-api-v1</c> pack — not spike-root prompts/schemas.
/// </summary>
internal static class SpikeTestPaths
{
    private static readonly Lazy<ArchetypePack> RestApiV1PackLazy = new(
        () => ArchetypePackLoader.Load(Root, "rest-api-v1"));

    public static string Root =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    public static ArchetypePack RestApiV1Pack => RestApiV1PackLazy.Value;

    /// <summary>Alias kept so Phase 0 compiles against Spike_05 test names until Phase 3 renames call sites.</summary>
    public static ArchetypePack ConsoleV1Pack => RestApiV1Pack;

    public static string RestApiV1PackRoot => RestApiV1Pack.PackRoot;

    public static string ConsoleV1PackRoot => RestApiV1PackRoot;

    public static string AnalystPromptTemplate => RestApiV1Pack.Analyst.PromptPath;

    public static string PlannerPromptTemplate => RestApiV1Pack.Planner.PromptPath;

    public static string CoderPromptTemplate => RestApiV1Pack.Coder.PromptPath;

    public static string StructuredChangeSchema => RestApiV1Pack.Analyst.OutputSchemaPath;

    public static string ImplementationPlanSchema => RestApiV1Pack.Planner.OutputSchemaPath;

    public static string PatchPackageSchema => RestApiV1Pack.Coder.OutputSchemaPath;

    public static string StructuredRequirementSchema =>
        Path.Combine(RestApiV1Pack.Paths.SchemasDirectory, "structured-requirement.schema.json");

    public static string CodePackageSchema =>
        Path.Combine(RestApiV1Pack.Paths.SchemasDirectory, "code-package.schema.json");

    public static string ChangeBundleSchema =>
        Path.Combine(RestApiV1Pack.Paths.SchemasDirectory, "change-bundle.schema.json");

    public static string ChangeRequestSchema =>
        Path.Combine(RestApiV1Pack.Paths.SchemasDirectory, "change-request.schema.json");

    public static string CodeContextSchema =>
        Path.Combine(RestApiV1Pack.Paths.SchemasDirectory, "code-context.schema.json");

    public static string MiniErpV1FixtureRoot => RestApiV1Pack.Baseline.FixtureRoot;

    public static string EchoV1FixtureRoot => MiniErpV1FixtureRoot;
}
