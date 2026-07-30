using Athlon.Spike.Agents;
using Athlon.Spike.Artifacts;
using Athlon.Spike.ConsoleHost;
using Athlon.Spike.Contracts;
using Athlon.Spike.Llm;
using Athlon.Spike.Workflow;

// Spike_04 console — Phase 1: ChangeRequest → Analyst → CodeContext (fail fast).
// Run from src/Spike_04 so ./prompts, ./schemas, ./artifacts, ./fixtures, ./appsettings*.json resolve locally.
// Planner / Coder / Applier land in later phases.

try
{
    // 1) Sample change request against fixtures/echo-v1 (in bounds)
    var changeRequest = new ChangeRequestPayload(
        Kind: ChangeRequest.KindFeature,
        Title: "Uppercase echo",
        Description: "Change the echo console so it prints the input in UPPERCASE.",
        SuspectedPaths: ["Echo/Program.cs"]);

    const string fixtureId = "echo-v1";
    const string fixtureRoot = "fixtures/echo-v1";
    const string entryProject = "Echo/Echo.csproj";

    // 2) OpenRouter settings + immutable artifact store
    var (apiKey, model) = OpenRouterConfig.Load();
    const string artifactsRoot = "artifacts";
    var store = new FileArtifactStore(artifactsRoot);
    var runner = new WorkflowRunner(store, artifactRoot: artifactsRoot);
    var codeContextBuilder = new CodeContextBuilder(store);

    using var llm = new OpenRouterProvider(apiKey: apiKey, model: model);

    var analyst = new AnalystAgent(
        llm,
        store,
        promptTemplatePath: Path.Combine("prompts", "analyst-v1.txt"),
        schemaPath: Path.Combine("schemas", "structured-change.schema.json"));

    Console.WriteLine("=== Spike_04 — ChangeRequest → Analyst → CodeContext ===");
    Console.WriteLine($"Working directory : {Directory.GetCurrentDirectory()}");
    Console.WriteLine($"Artifacts         : {Path.GetFullPath(artifactsRoot)}");
    Console.WriteLine($"Fixture           : {fixtureId} ({Path.GetFullPath(fixtureRoot)})");
    Console.WriteLine($"Model             : {model}");
    Console.WriteLine();
    Console.WriteLine($"Hardcoded ChangeRequest ({changeRequest.Kind}): {changeRequest.Title}");
    Console.WriteLine(changeRequest.Description);
    Console.WriteLine();

    // 3) Persist ChangeRequest, then Analyst → StructuredChange (or abort out of bounds)
    var (instance, inputArtifact) = await runner.StartAndSaveChangeRequestAsync(
        "ChangeRequestToCodeContext",
        changeRequest,
        CancellationToken.None);

    Console.WriteLine($"Workflow id       : {instance.Id:D}");
    Console.WriteLine($"Input artifact    : {inputArtifact.Id:D}");
    Console.WriteLine($"  path            : {ArtifactPath(inputArtifact)}");
    Console.WriteLine();

    // --- Analyst ---
    Console.WriteLine("--- Step 1/2: AnalystAgent ---");
    AgentExecutionResult analystResult;
    try
    {
        analystResult = await analyst.ExecuteAsync(
            new AgentExecutionContext(instance.Id, inputArtifact.Id),
            CancellationToken.None);
    }
    catch (OutOfBoundsException ex)
    {
        runner.MarkFailed(instance);
        if (ex.Telemetry is { } telemetry)
        {
            await runner.PersistTelemetryAsync(instance.Id, telemetry, CancellationToken.None);
            PrintAgentTelemetry("Analyst", telemetry);
        }

        Console.WriteLine();
        Console.WriteLine("OUT OF BOUNDS — Analyst aborted without publishing StructuredChange.");
        Console.WriteLine($"Reason: {ex.Reason}");
        return 2;
    }

    var structured = analystResult.OutputArtifact;
    Console.WriteLine($"StructuredChange  : {structured.Id:D}");
    Console.WriteLine($"  path            : {ArtifactPath(structured)}");
    Console.WriteLine($"  type            : {structured.Type}");
    PrintAgentTelemetry("Analyst", analystResult.Telemetry);
    Console.WriteLine();

    // --- CodeContext (deterministic) ---
    Console.WriteLine("--- Step 2/2: CodeContextBuilder ---");
    Artifact codeContextArtifact;
    try
    {
        codeContextArtifact = await codeContextBuilder.BuildAndPublishAsync(
            instance.Id,
            fixtureId,
            fixtureRoot,
            entryProject,
            cancellationToken: CancellationToken.None);
    }
    catch (Exception ex) when (ex is InvalidOperationException or DirectoryNotFoundException or FileNotFoundException)
    {
        runner.MarkFailed(instance);
        await runner.PersistTelemetryAsync(instance.Id, analystResult.Telemetry, CancellationToken.None);
        Console.WriteLine();
        Console.WriteLine($"CODE CONTEXT FAILED — {ex.Message}");
        return 3;
    }

    var codeContext = CodeContext.Parse(codeContextArtifact);
    Console.WriteLine($"CodeContext       : {codeContextArtifact.Id:D}");
    Console.WriteLine($"  path            : {ArtifactPath(codeContextArtifact)}");
    Console.WriteLine($"  files           : {codeContext.Files.Count} (cap {codeContext.MaxFilesAllowed})");
    Console.WriteLine($"  chars           : {codeContext.TotalChars} (cap {codeContext.MaxCharsAllowed})");
    Console.WriteLine();

    await runner.PersistTelemetryAsync(instance.Id, analystResult.Telemetry, CancellationToken.None);
    WorkflowRunner.PrintRunSummary(new WorkflowResult(
        Instance: runner.MarkCompleted(instance),
        InputArtifact: inputArtifact,
        OutputArtifact: codeContextArtifact,
        Telemetry: analystResult.Telemetry));

    Console.WriteLine();
    Console.WriteLine("Phase 1 complete — Planner/Coder/Applier come next.");
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
