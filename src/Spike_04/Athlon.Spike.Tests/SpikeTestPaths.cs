namespace Athlon.Spike.Tests;

internal static class SpikeTestPaths
{
    public static string Root =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    public static string PlannerPromptTemplate => Path.Combine(Root, "prompts", "planner-v1.txt");

    public static string AnalystPromptTemplate => Path.Combine(Root, "prompts", "analyst-v1.txt");

    public static string ImplementationPlanSchema => Path.Combine(Root, "schemas", "implementation-plan.schema.json");

    public static string CodePackageSchema => Path.Combine(Root, "schemas", "code-package.schema.json");

    public static string CoderPromptTemplate => Path.Combine(Root, "prompts", "coder-v1.txt");

    public static string StructuredRequirementSchema =>
        Path.Combine(Root, "schemas", "structured-requirement.schema.json");

    public static string StructuredChangeSchema =>
        Path.Combine(Root, "schemas", "structured-change.schema.json");

    public static string ChangeRequestSchema =>
        Path.Combine(Root, "schemas", "change-request.schema.json");

    public static string CodeContextSchema =>
        Path.Combine(Root, "schemas", "code-context.schema.json");

    public static string EchoV1FixtureRoot =>
        Path.Combine(Root, "fixtures", "echo-v1");
}
