namespace Athlon.Spike.Workflow;

public interface IWorkflowStepLogger
{
    void LogStep(string step, TimeSpan duration, Guid? artifactId = null);
}
