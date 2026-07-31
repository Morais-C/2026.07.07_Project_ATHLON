namespace Athlon.Spike.Contracts;

/// <summary>
/// Archetype pack manifest is missing, invalid, or fails path validation.
/// Host should fail fast without starting the workflow chain.
/// </summary>
public sealed class ArchetypePackException : Exception
{
    public ArchetypePackException(string message)
        : base(message)
    {
    }

    public ArchetypePackException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
