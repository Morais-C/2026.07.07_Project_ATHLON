namespace Athlon.Spike.Contracts;

public static class ArtifactTypes
{
    public const string ChangeRequest = "ChangeRequest";
    public const string StructuredChange = "StructuredChange";
    public const string CodeContext = "CodeContext";

    // Spike_03 greenfield types — kept until Phase 2 rewires Planner/Coder
    public const string BusinessRequirement = "BusinessRequirement";
    public const string StructuredRequirement = "StructuredRequirement";
    public const string ImplementationPlan = "ImplementationPlan";
    public const string CodePackage = "CodePackage";
}
