using Athlon.Spike.Agents;
using Athlon.Spike.Artifacts;
using Athlon.Spike.ConsoleHost;
using Athlon.Spike.Contracts;
using Athlon.Spike.Llm;
using Athlon.Spike.Workflow;

// Spike_05 console — full change chain, fail fast (no Enter pauses).
// Run from src/Spike_05 so ./artifacts, ./Publish, ./appsettings*.json resolve locally.
// Archetype pack paths come from archetypes/{archetypeId}/ via ArchetypePackLoader.
// Chain: ChangeRequest → Analyst → CodeContext → Planner → Coder → Applier (apply + build).

try
{
    ArchetypePack pack;
    try
    {
        var (archetypeId, spikeRoot) = ArchetypeConfig.Load();
        pack = ArchetypePackLoader.Load(spikeRoot, archetypeId);
    }
    catch (ArchetypePackException ex)
    {
        Console.Error.WriteLine($"Archetype pack error: {ex.Message}");
        return 1;
    }

    // Sample change request (in bounds for console-v1). Swap KindFeature ↔ KindBugfix for demos.
    // Demo catalog lives in pack.Paths.Demos; host picks active demo in a later phase.
    var changeRequest = new ChangeRequestPayload(
        Kind: ChangeRequest.KindFeature,
        Title: "Uppercase echo",
        Description: "Change the echo console so it prints the input in UPPERCASE.",
        SuspectedPaths: ["Echo/Program.cs"]);

    const string entryProject = "Echo/Echo.csproj";
    const string artifactsRoot = "artifacts";
    const string publishRoot = "Publish";

    var fixtureId = pack.Baseline.FixtureId;
    var fixtureRoot = pack.Baseline.FixtureRoot;

    var (apiKey, model) = OpenRouterConfig.Load();
    var store = new FileArtifactStore(artifactsRoot);
    var runner = new WorkflowRunner(store, artifactRoot: artifactsRoot);
    var codeContextBuilder = CodeContextBuilder.FromArchetypePack(store, pack);
    var applier = new Applier(store, publishRoot: publishRoot);

    using var llm = new OpenRouterProvider(apiKey: apiKey, model: model);

    var analyst = new AnalystAgent(
        llm,
        store,
        promptTemplatePath: pack.Analyst.PromptPath,
        schemaPath: pack.Analyst.OutputSchemaPath);

    var planner = new PlannerAgent(
        llm,
        store,
        promptTemplatePath: pack.Planner.PromptPath,
        schemaPath: pack.Planner.OutputSchemaPath);

    var coder = new CoderAgent(
        llm,
        store,
        promptTemplatePath: pack.Coder.PromptPath,
        schemaPath: pack.Coder.OutputSchemaPath);

    Console.WriteLine("=== Spike_05 — ChangeRequest → Analyst → Planner → Coder → Applier ===");
    Console.WriteLine($"Working directory : {Directory.GetCurrentDirectory()}");
    Console.WriteLine($"Archetype         : {pack.ArchetypeId} v{pack.Version} ({pack.DisplayName})");
    Console.WriteLine($"Pack root         : {pack.PackRoot}");
    Console.WriteLine($"Artifacts         : {Path.GetFullPath(artifactsRoot)}");
    Console.WriteLine($"Publish root      : {Path.GetFullPath(publishRoot)}");
    Console.WriteLine($"Fixture           : {fixtureId} ({fixtureRoot})");
    Console.WriteLine($"CodeContext caps  : {pack.CodeContext.MaxFilesAllowed} files, {pack.CodeContext.MaxCharsAllowed} chars");
    Console.WriteLine($"Model             : {model}");
    Console.WriteLine();
    Console.WriteLine($"Hardcoded ChangeRequest ({changeRequest.Kind}): {changeRequest.Title}");
    Console.WriteLine(changeRequest.Description);
    Console.WriteLine();

    // Persist ChangeRequest, then run the chain (fail fast on any step)
    var (instance, inputArtifact) = await runner.StartAndSaveChangeRequestAsync(
        "ChangeRequestToPublish",
        changeRequest,
        CancellationToken.None);

    Console.WriteLine($"Workflow id       : {instance.Id:D}");
    Console.WriteLine($"Input artifact    : {inputArtifact.Id:D}");
    Console.WriteLine($"  path            : {ArtifactPath(inputArtifact)}");
    Console.WriteLine();

    // --- Analyst ---
    Console.WriteLine("--- Step 1/5: AnalystAgent ---");
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

    // --- CodeContext + ChangeBundle (deterministic) ---
    Console.WriteLine("--- Step 2/5: CodeContextBuilder + ChangeBundle ---");
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

    var bundle = await runner.SaveChangeBundleAsync(
        instance.Id,
        structured.Id,
        codeContextArtifact.Id,
        CancellationToken.None);
    Console.WriteLine($"ChangeBundle      : {bundle.Id:D}");
    Console.WriteLine($"  path            : {ArtifactPath(bundle)}");
    Console.WriteLine();

    // --- Planner ---
    Console.WriteLine("--- Step 3/5: PlannerAgent ---");
    var plannerResult = await planner.ExecuteAsync(
        new AgentExecutionContext(instance.Id, bundle.Id),
        CancellationToken.None);

    var plan = plannerResult.OutputArtifact;
    Console.WriteLine($"ImplementationPlan: {plan.Id:D}");
    Console.WriteLine($"  path            : {ArtifactPath(plan)}");
    Console.WriteLine($"  type            : {plan.Type}");
    PrintAgentTelemetry("Planner", plannerResult.Telemetry);
    Console.WriteLine();

    // --- Coder ---
    Console.WriteLine("--- Step 4/5: CoderAgent ---");
    var coderResult = await coder.ExecuteAsync(
        new AgentExecutionContext(instance.Id, plan.Id),
        CancellationToken.None);

    var patchPackage = coderResult.OutputArtifact;
    Console.WriteLine($"PatchPackage      : {patchPackage.Id:D}");
    Console.WriteLine($"  path            : {ArtifactPath(patchPackage)}");
    Console.WriteLine($"  type            : {patchPackage.Type}");
    PrintAgentTelemetry("Coder", coderResult.Telemetry);
    Console.WriteLine();

    // --- Applier (deterministic) ---
    Console.WriteLine("--- Step 5/5: Applier (copy → apply → build) ---");
    var applyResult = await applier.ApplyAsync(
        instance.Id,
        patchPackage.Id,
        expectedFixtureId: fixtureId,
        fixtureRoot: fixtureRoot,
        CancellationToken.None);

    Console.WriteLine($"Publish directory : {applyResult.PublishDirectory}");
    Console.WriteLine($"  manifest        : {applyResult.ManifestPath}");
    Console.WriteLine($"  apply           : {(applyResult.ApplySucceeded ? "OK" : "FAIL")}");
    Console.WriteLine($"  build           : {(applyResult.BuildSucceeded ? "OK" : "FAIL")}");
    if (applyResult.FailureMessage is { } fail)
    {
        Console.WriteLine($"  failure         : {fail}");
    }

    Console.WriteLine("  functional test : deferred (Tester agent later)");
    Console.WriteLine();

    var combined = CombineTelemetry(analystResult.Telemetry, plannerResult.Telemetry, coderResult.Telemetry);
    await runner.PersistTelemetryAsync(instance.Id, combined, CancellationToken.None);

    if (!applyResult.Succeeded)
    {
        runner.MarkFailed(instance);
        WorkflowRunner.PrintRunSummary(new WorkflowResult(
            Instance: instance,
            InputArtifact: inputArtifact,
            OutputArtifact: patchPackage,
            Telemetry: combined,
            FailureMessage: applyResult.FailureMessage));
        return 4;
    }

    WorkflowRunner.PrintRunSummary(new WorkflowResult(
        Instance: runner.MarkCompleted(instance),
        InputArtifact: inputArtifact,
        OutputArtifact: patchPackage,
        Telemetry: combined));

    Console.WriteLine();
    Console.WriteLine($"Open Publish folder: {Path.GetFullPath(applyResult.PublishDirectory)}");
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

static LlmCompletionResult CombineTelemetry(params LlmCompletionResult[] parts)
{
    var last = parts[^1];
    return new(
        Content: last.Content,
        Model: last.Model,
        PromptTokens: parts.Sum(p => p.PromptTokens),
        CompletionTokens: parts.Sum(p => p.CompletionTokens),
        TotalTokens: parts.Sum(p => p.TotalTokens),
        Duration: parts.Aggregate(TimeSpan.Zero, (sum, p) => sum + p.Duration),
        EstimatedCostUsd: parts.Sum(p => p.EstimatedCostUsd ?? 0m));
}
