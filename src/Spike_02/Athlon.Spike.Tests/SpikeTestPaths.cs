namespace Athlon.Spike.Tests;

internal static class SpikeTestPaths
{
    public static string Root =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    public static string PromptTemplate => Path.Combine(Root, "prompts", "developer-v1.txt");

    public static string ImplementationSchema => Path.Combine(Root, "schemas", "implementation-artifact.schema.json");

    public static string StructuredRequirementSchema =>
        Path.Combine(Root, "schemas", "structured-requirement.schema.json");
}
