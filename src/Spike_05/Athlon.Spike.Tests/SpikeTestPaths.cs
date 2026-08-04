using Athlon.Spike.Contracts;

namespace Athlon.Spike.Tests;

/// <summary>
/// Test paths resolve through the loaded <c>console-v1</c> pack (Phase 4) — not spike-root prompts/schemas.
/// </summary>
internal static class SpikeTestPaths
{
    private static readonly Lazy<ArchetypePack> ConsoleV1PackLazy = new(
        () => ArchetypePackLoader.Load(Root, "console-v1"));

    public static string Root =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    public static ArchetypePack ConsoleV1Pack => ConsoleV1PackLazy.Value;

    public static string ConsoleV1PackRoot => ConsoleV1Pack.PackRoot;

    public static string AnalystPromptTemplate => ConsoleV1Pack.Analyst.PromptPath;

    public static string PlannerPromptTemplate => ConsoleV1Pack.Planner.PromptPath;

    public static string CoderPromptTemplate => ConsoleV1Pack.Coder.PromptPath;

    public static string StructuredChangeSchema => ConsoleV1Pack.Analyst.OutputSchemaPath;

    public static string ImplementationPlanSchema => ConsoleV1Pack.Planner.OutputSchemaPath;

    public static string PatchPackageSchema => ConsoleV1Pack.Coder.OutputSchemaPath;

    public static string StructuredRequirementSchema =>
        Path.Combine(ConsoleV1Pack.Paths.SchemasDirectory, "structured-requirement.schema.json");

    public static string CodePackageSchema =>
        Path.Combine(ConsoleV1Pack.Paths.SchemasDirectory, "code-package.schema.json");

    public static string ChangeBundleSchema =>
        Path.Combine(ConsoleV1Pack.Paths.SchemasDirectory, "change-bundle.schema.json");

    public static string ChangeRequestSchema =>
        Path.Combine(ConsoleV1Pack.Paths.SchemasDirectory, "change-request.schema.json");

    public static string CodeContextSchema =>
        Path.Combine(ConsoleV1Pack.Paths.SchemasDirectory, "code-context.schema.json");

    public static string EchoV1FixtureRoot => ConsoleV1Pack.Baseline.FixtureRoot;
}
