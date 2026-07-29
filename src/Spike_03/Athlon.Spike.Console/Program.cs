using Athlon.Spike.Agents;
using Athlon.Spike.Artifacts;
using Athlon.Spike.ConsoleHost;
using Athlon.Spike.Contracts;
using Athlon.Spike.Llm;
using Athlon.Spike.Workflow;

// Spike_03 console — Minimal + real LLM (no CLI flags).
// Run from src/Spike_03 so ./prompts, ./schemas, ./artifacts, ./Publish, ./appsettings*.json resolve locally.
// Chain: Analyst → Planner → Coder → Publisher (deterministic materialize + build).
// Functional run checks deferred to a future Tester agent.

try
{
    // 1) Sample need — simple Read → Process → Print console (in bounds)
    var need =
        """
        Build a small .NET 9 console application that calculates an employee's monthly payslip. 
        It must support gross salary, overtime, bonuses, taxes, social security deductions, and configurable tax brackets. 
        The application should generate a detailed payroll summary.
        Assume some sort of hard coded tax brackets and social security deductions
        Must be defined what is asked to the user, and what is the output.
        """;

    // 2) OpenRouter settings + immutable artifact store
    var (apiKey, model) = OpenRouterConfig.Load();
    const string artifactsRoot = "artifacts";
    const string publishRoot = "Publish";
    var store = new FileArtifactStore(artifactsRoot);
    var runner = new WorkflowRunner(store, artifactRoot: artifactsRoot);
    var publisher = new Publisher(store, publishRoot: publishRoot);

    using var llm = new OpenRouterProvider(apiKey: apiKey, model: model);

    var analyst = new AnalystAgent(
        llm,
        store,
        promptTemplatePath: Path.Combine("prompts", "analyst-v1.txt"),
        schemaPath: Path.Combine("schemas", "structured-requirement.schema.json"));

    var planner = new PlannerAgent(
        llm,
        store,
        promptTemplatePath: Path.Combine("prompts", "planner-v1.txt"),
        schemaPath: Path.Combine("schemas", "implementation-plan.schema.json"));

    var coder = new CoderAgent(
        llm,
        store,
        promptTemplatePath: Path.Combine("prompts", "coder-v1.txt"),
        schemaPath: Path.Combine("schemas", "code-package.schema.json"));

    Console.WriteLine("=== Spike_03 — Analyst → Planner → Coder → Publisher ===");
    Console.WriteLine($"Working directory : {Directory.GetCurrentDirectory()}");
    Console.WriteLine($"Artifacts         : {Path.GetFullPath(artifactsRoot)}");
    Console.WriteLine($"Publish root      : {Path.GetFullPath(publishRoot)}");
    Console.WriteLine($"Model             : {model}");
    Console.WriteLine();
    Console.WriteLine("Hardcoded need:");
    Console.WriteLine(need.Trim());
    Console.WriteLine();

    // 3) Persist raw need, then Analyst → StructuredRequirement (or abort out of bounds)
    var (instance, inputArtifact) = await runner.StartAndSaveInputAsync(
        "BusinessNeedToPublish",
        need,
        CancellationToken.None);

    Console.WriteLine($"Workflow id       : {instance.Id:D}");
    Console.WriteLine($"Input artifact    : {inputArtifact.Id:D}");
    Console.WriteLine($"  path            : {ArtifactPath(inputArtifact)}");
    Console.WriteLine();

    // --- Analyst ---
    Console.WriteLine("--- Step 1/4: AnalystAgent ---");
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
        Console.WriteLine("OUT OF BOUNDS — Analyst aborted without publishing StructuredRequirement.");
        Console.WriteLine($"Reason: {ex.Reason}");
        Console.WriteLine();
        Console.WriteLine("Press Enter to exit...");
        Console.ReadLine();
        return 2;
    }

    var structured = analystResult.OutputArtifact;
    Console.WriteLine($"Analyst artifact  : {structured.Id:D}");
    Console.WriteLine($"  path            : {ArtifactPath(structured)}");
    Console.WriteLine($"  type            : {structured.Type}");
    PrintAgentTelemetry("Analyst", analystResult.Telemetry);
    Console.WriteLine();
    PauseBefore("PlannerAgent");

    // --- Planner ---
    Console.WriteLine("--- Step 2/4: PlannerAgent ---");
    var plannerResult = await planner.ExecuteAsync(
        new AgentExecutionContext(instance.Id, structured.Id),
        CancellationToken.None);

    var plan = plannerResult.OutputArtifact;
    Console.WriteLine($"ImplementationPlan: {plan.Id:D}");
    Console.WriteLine($"  path            : {ArtifactPath(plan)}");
    Console.WriteLine($"  type            : {plan.Type}");
    PrintAgentTelemetry("Planner", plannerResult.Telemetry);
    Console.WriteLine();
    PauseBefore("CoderAgent");

    // --- Coder ---
    Console.WriteLine("--- Step 3/4: CoderAgent ---");
    var coderResult = await coder.ExecuteAsync(
        new AgentExecutionContext(instance.Id, plan.Id),
        CancellationToken.None);

    var codePackage = coderResult.OutputArtifact;
    Console.WriteLine($"CodePackage       : {codePackage.Id:D}");
    Console.WriteLine($"  path            : {ArtifactPath(codePackage)}");
    Console.WriteLine($"  type            : {codePackage.Type}");
    PrintAgentTelemetry("Coder", coderResult.Telemetry);
    Console.WriteLine();
    PauseBefore("Publisher");

    // --- Publisher (deterministic) ---
    Console.WriteLine("--- Step 4/4: Publisher (materialize → build) ---");
    var publishResult = await publisher.PublishAsync(instance.Id, codePackage.Id, CancellationToken.None);

    Console.WriteLine($"Publish directory : {publishResult.PublishDirectory}");
    Console.WriteLine($"  manifest        : {publishResult.ManifestPath}");
    Console.WriteLine($"  build           : {(publishResult.BuildSucceeded ? "OK" : "FAIL")}");
    if (publishResult.FailureMessage is { } fail)
    {
        Console.WriteLine($"  failure         : {fail}");
    }

    Console.WriteLine("  functional test : deferred (Tester agent later)");
    Console.WriteLine();

    var combined = CombineTelemetry(analystResult.Telemetry, plannerResult.Telemetry, coderResult.Telemetry);
    await runner.PersistTelemetryAsync(instance.Id, combined, CancellationToken.None);

    if (!publishResult.Succeeded)
    {
        runner.MarkFailed(instance);
        WorkflowRunner.PrintRunSummary(new WorkflowResult(
            Instance: instance,
            InputArtifact: inputArtifact,
            OutputArtifact: codePackage,
            Telemetry: combined,
            FailureMessage: publishResult.FailureMessage));
        return 3;
    }

    WorkflowRunner.PrintRunSummary(new WorkflowResult(
        Instance: runner.MarkCompleted(instance),
        InputArtifact: inputArtifact,
        OutputArtifact: codePackage,
        Telemetry: combined));

    Console.WriteLine();
    Console.WriteLine($"Open Publish folder: {Path.GetFullPath(publishResult.PublishDirectory)}");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 1;
}

static void PauseBefore(string nextStep)
{
    Console.WriteLine($"Press Enter to continue to {nextStep} (or Ctrl+C to stop)...");
    Console.ReadLine();
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
