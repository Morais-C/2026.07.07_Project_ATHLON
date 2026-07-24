using Athlon.Spike.Agents;
using Athlon.Spike.Artifacts;
using Athlon.Spike.ConsoleHost;
using Athlon.Spike.Contracts;
using Athlon.Spike.Llm;
using Athlon.Spike.Workflow;

// Spike_02 console — Minimal + real LLM (no CLI flags).
// Run from src/Spike_02 so ./prompts, ./schemas, ./artifacts, ./appsettings*.json resolve locally.
// Thesis: BA → artifact on disk → Developer loads by id only (no chat handoff).

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

    // 2) OpenRouter settings + immutable artifact store
    var (apiKey, model) = OpenRouterConfig.Load();
    const string artifactsRoot = "artifacts";
    var store = new FileArtifactStore(artifactsRoot);
    var runner = new WorkflowRunner(store, artifactRoot: artifactsRoot);

    using var llm = new OpenRouterProvider(apiKey: apiKey, model: model);

    var ba = new BusinessAnalystAgent(
        llm,
        store,
        promptTemplatePath: Path.Combine("prompts", "ba-v1.txt"),
        schemaPath: Path.Combine("schemas", "structured-requirement.schema.json"));

    var developer = new DeveloperAgent(
        llm,
        store,
        promptTemplatePath: Path.Combine("prompts", "developer-v1.txt"),
        schemaPath: Path.Combine("schemas", "implementation-artifact.schema.json"));

    Console.WriteLine($"Working directory : {Directory.GetCurrentDirectory()}");
    Console.WriteLine($"Artifacts         : {Path.GetFullPath(artifactsRoot)}");
    Console.WriteLine($"Model             : {model}");
    Console.WriteLine($"Workflow          : BA → Developer (by artifact id)");
    Console.WriteLine();

    // 3) Persist raw need, then BA → StructuredRequirement on disk
    var (instance, inputArtifact) = await runner.StartAndSaveInputAsync(
        "BusinessNeedToImplementation",
        need,
        CancellationToken.None);

    Console.WriteLine($"Workflow id       : {instance.Id:D}");
    Console.WriteLine($"Input artifact    : {inputArtifact.Id:D}");
    Console.WriteLine($"  path            : {ArtifactPath(inputArtifact)}");
    Console.WriteLine();
    Console.WriteLine("Running BusinessAnalystAgent...");

    var baResult = await ba.ExecuteAsync(
        new AgentExecutionContext(instance.Id, inputArtifact.Id),
        CancellationToken.None);

    var structured = baResult.OutputArtifact;
    Console.WriteLine($"BA artifact       : {structured.Id:D}");
    Console.WriteLine($"  path            : {ArtifactPath(structured)}");
    Console.WriteLine($"  type            : {structured.Type}");
    PrintAgentTelemetry("BA", baResult.Telemetry);
    Console.WriteLine();

    // 4) Mid-chain gate — inspect BA JSON on disk before Developer runs
    Console.WriteLine("Press Enter to continue to DeveloperAgent (or Ctrl+C to stop)...");
    Console.ReadLine();

    // 5) Developer: InputArtifactId only — prompt built from LoadAsync inside the agent
    Console.WriteLine("Running DeveloperAgent...");
    var devResult = await developer.ExecuteAsync(
        new AgentExecutionContext(instance.Id, structured.Id),
        CancellationToken.None);

    var implementation = devResult.OutputArtifact;
    Console.WriteLine($"Implementation    : {implementation.Id:D}");
    Console.WriteLine($"  path            : {ArtifactPath(implementation)}");
    Console.WriteLine($"  type            : {implementation.Type}");
    PrintAgentTelemetry("Developer", devResult.Telemetry);

    // 6) Combined telemetry next to this run's artifacts
    var combined = CombineTelemetry(baResult.Telemetry, devResult.Telemetry);
    await runner.PersistTelemetryAsync(instance.Id, combined, CancellationToken.None);
    WorkflowRunner.PrintRunSummary(new WorkflowResult(
        Instance: runner.MarkCompleted(instance),
        InputArtifact: inputArtifact,
        OutputArtifact: implementation,
        Telemetry: combined));

    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 1;
}

static string ArtifactPath(Artifact artifact) =>
    Path.Combine("artifacts", artifact.WorkflowInstanceId.ToString("D"), $"{artifact.Id:D}.json");

static void PrintAgentTelemetry(string label, LlmCompletionResult telemetry)
{
    Console.WriteLine($"  {label} tokens   : {telemetry.TotalTokens} (prompt {telemetry.PromptTokens}, completion {telemetry.CompletionTokens})");
    Console.WriteLine($"  {label} duration : {telemetry.Duration.TotalSeconds:F1}s");
    if (telemetry.EstimatedCostUsd is { } cost)
    {
        Console.WriteLine($"  {label} est.cost : ${cost:F4}");
    }
}

static LlmCompletionResult CombineTelemetry(LlmCompletionResult ba, LlmCompletionResult developer) =>
    new(
        Content: developer.Content,
        Model: developer.Model,
        PromptTokens: ba.PromptTokens + developer.PromptTokens,
        CompletionTokens: ba.CompletionTokens + developer.CompletionTokens,
        TotalTokens: ba.TotalTokens + developer.TotalTokens,
        Duration: ba.Duration + developer.Duration,
        EstimatedCostUsd: (ba.EstimatedCostUsd ?? 0m) + (developer.EstimatedCostUsd ?? 0m));
