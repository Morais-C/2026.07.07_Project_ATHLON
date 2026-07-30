namespace Athlon.Spike.Contracts;

public static class ArtifactTypes
{
    public const string ChangeRequest = "ChangeRequest";
    public const string StructuredChange = "StructuredChange";
    public const string CodeContext = "CodeContext";
    public const string ChangeBundle = "ChangeBundle";
    public const string ImplementationPlan = "ImplementationPlan";
    public const string PatchPackage = "PatchPackage";

    // Spike_03 greenfield leftovers — kept for PublisherTests until Phase 3 Applier replaces them
    public const string BusinessRequirement = "BusinessRequirement";
    public const string StructuredRequirement = "StructuredRequirement";
    public const string CodePackage = "CodePackage";
}
