namespace Athlon.Spike.Workflow;

public sealed class ConsoleWorkflowStepLogger : IWorkflowStepLogger
{
    public void LogStep(string step, TimeSpan duration, Guid? artifactId = null)
    {
        var artifactSuffix = artifactId.HasValue ? $" artifactId={artifactId.Value:D}" : string.Empty;
        Console.WriteLine($"[workflow] step={step} durationMs={duration.TotalMilliseconds:F0}{artifactSuffix}");
    }
}
