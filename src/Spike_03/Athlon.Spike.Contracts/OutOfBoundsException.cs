namespace Athlon.Spike.Contracts;

/// <summary>
/// Analyst determined the raw need is outside Spike_03 console bounds.
/// No StructuredRequirement was published.
/// </summary>
public sealed class OutOfBoundsException : Exception
{
    public OutOfBoundsException(string reason, LlmCompletionResult? telemetry = null)
        : base(FormatMessage(reason))
    {
        Reason = string.IsNullOrWhiteSpace(reason) ? "Out of bounds." : reason.Trim();
        Telemetry = telemetry;
    }

    public string Reason { get; }

    public LlmCompletionResult? Telemetry { get; }

    private static string FormatMessage(string reason) =>
        $"Out of bounds: {(string.IsNullOrWhiteSpace(reason) ? "need is outside console spike scope." : reason.Trim())}";
}
