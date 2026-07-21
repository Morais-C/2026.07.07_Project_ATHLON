using Athlon.Spike.Agents;
using Athlon.Spike.Artifacts;
using Athlon.Spike.ConsoleHost;
using Athlon.Spike.Contracts;
using Athlon.Spike.Llm;
using Athlon.Spike.Workflow;

// Spike_02 console — Minimal + real LLM (no CLI flags).
// Run from src/Spike_02 so ./prompts, ./schemas, ./artifacts, ./appsettings*.json resolve locally.
// Phase 0.5: still the Spike_01 single-Developer loop.
// Later: BA → StructuredRequirement → Developer (by artifact id).

try
{
    // 1) Sample business need — edit this string to try other scenarios
    var need =
        """
        As an employee, I want a daily meal allowance so that I can cover lunch expenses while at the office.

        Acceptance intent:
        - Eligible employees receive a fixed daily allowance on working days
        - Payroll must show the allowance as a separate line item
        - Managers can review monthly totals per team
        """;

    // 2) OpenRouter settings (appsettings.json + optional Local override)
    var (apiKey, model) = OpenRouterConfig.Load();

    const string artifactsRoot = "artifacts";

    // 3) Immutable JSON artifact store on disk
    var store = new FileArtifactStore(artifactsRoot);

    // 4) Real LLM + Developer agent (BA agent comes in Phase 2)
    using var llm = new OpenRouterProvider(apiKey: apiKey, model: model);
    var agent = new DeveloperAgent(
        llm,
        store,
        promptTemplatePath: Path.Combine("prompts", "developer-v1.txt"),
        schemaPath: Path.Combine("schemas", "implementation-artifact.schema.json"));

    // 5) Run workflow: save input artifact → Developer → complete (auto-continue)
    var runner = new WorkflowRunner(store, artifactRoot: artifactsRoot);
    var workflow = new RequirementToImplementationWorkflow(runner, agent);

    Console.WriteLine($"Working directory : {Directory.GetCurrentDirectory()}");
    Console.WriteLine($"Artifacts         : {Path.GetFullPath(artifactsRoot)}");
    Console.WriteLine($"Model             : {model}");
    Console.WriteLine($"Workflow          : {RequirementToImplementationWorkflow.WorkflowNameValue}");
    Console.WriteLine();

    var result = await workflow.RunAsync(
        new WorkflowInput(need, AutoApprove: true),
        CancellationToken.None);

    // 6) Show where artifacts landed (open these JSON files to inspect)
    Console.WriteLine();
    Console.WriteLine($"Workflow id     : {result.Instance.Id:D}");
    Console.WriteLine($"Status          : {result.Instance.Status}");
    Console.WriteLine($"Input artifact  : {result.InputArtifact.Id:D}");
    Console.WriteLine($"  path          : {ArtifactPath(result.InputArtifact)}");

    if (result.OutputArtifact is not null)
    {
        Console.WriteLine($"Output artifact : {result.OutputArtifact.Id:D}");
        Console.WriteLine($"  path          : {ArtifactPath(result.OutputArtifact)}");
    }

    if (!result.Succeeded)
    {
        Console.WriteLine($"Failure         : {result.FailureMessage}");
        return 1;
    }

    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 1;
}

static string ArtifactPath(Artifact artifact) =>
    Path.Combine("artifacts", artifact.WorkflowInstanceId.ToString("D"), $"{artifact.Id:D}.json");
