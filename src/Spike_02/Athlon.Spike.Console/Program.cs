using Athlon.Spike.Agents;
using Athlon.Spike.Artifacts;
using Athlon.Spike.Contracts;
using Athlon.Spike.Llm;
using Athlon.Spike.Workflow;

return await ProgramEntry.RunAsync(args);

internal static class ProgramEntry
{
    public static async Task<int> RunAsync(string[] args)
    {
        try
        {
            if (args.Any(arg => arg is "--help" or "-h"))
            {
                PrintUsage();
                return 0;
            }

            var options = CliOptions.Parse(args);

            var spikeRoot = SpikePaths.ResolveRoot();
            EnvFile.Load(Path.Combine(spikeRoot, ".env"));

            var artifactsRoot = Environment.GetEnvironmentVariable("ATHLON_ARTIFACTS_PATH")
                ?? Path.Combine(spikeRoot, "artifacts");

            var store = new FileArtifactStore(artifactsRoot);

            if (options.LoadArtifactId is Guid loadId)
            {
                return await LoadArtifactAsync(store, loadId).ConfigureAwait(false);
            }

            var requirementText = await ResolveRequirementTextAsync(options).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(requirementText))
            {
                Console.Error.WriteLine("No business requirement provided.");
                PrintUsage();
                return 1;
            }

            using var llm = new OpenRouterProvider();
            var agent = new DeveloperAgent(
                llm,
                store,
                SpikePaths.PromptTemplate(spikeRoot),
                SpikePaths.ImplementationSchema(spikeRoot));

            var runner = new WorkflowRunner(store, artifactRoot: artifactsRoot);
            var workflow = new RequirementToImplementationWorkflow(
                runner,
                agent,
                approvalHandler: ct => PromptApprovalAsync(options.AutoApprove, ct));

            Console.WriteLine($"Artifacts root : {artifactsRoot}");
            Console.WriteLine($"Starting workflow: {RequirementToImplementationWorkflow.WorkflowNameValue}");
            Console.WriteLine();

            var result = await workflow.RunAsync(
                new WorkflowInput(requirementText, options.AutoApprove),
                CancellationToken.None).ConfigureAwait(false);

            PrintWorkflowOutcome(result, artifactsRoot);
            return result.Succeeded ? 0 : 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private static async Task<int> LoadArtifactAsync(IArtifactStore store, Guid id)
    {
        var artifact = await store.LoadAsync(id).ConfigureAwait(false);
        if (artifact is null)
        {
            Console.Error.WriteLine($"Artifact '{id:D}' was not found.");
            return 1;
        }

        var path = Path.Combine(
            ((FileArtifactStore)store).RootPath,
            artifact.WorkflowInstanceId.ToString("D"),
            $"{artifact.Id:D}.json");

        Console.WriteLine("Loaded artifact");
        Console.WriteLine($"  Id                 : {artifact.Id:D}");
        Console.WriteLine($"  Type               : {artifact.Type}");
        Console.WriteLine($"  Version            : {artifact.Version}");
        Console.WriteLine($"  Producer           : {artifact.Producer}");
        Console.WriteLine($"  WorkflowInstanceId : {artifact.WorkflowInstanceId:D}");
        Console.WriteLine($"  CreatedUtc         : {artifact.CreatedUtc:O}");
        Console.WriteLine($"  File path          : {path}");
        Console.WriteLine();
        Console.WriteLine(artifact.PayloadJson);
        return 0;
    }

    private static async Task<string> ResolveRequirementTextAsync(CliOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.Text))
        {
            return options.Text.Trim();
        }

        if (!string.IsNullOrWhiteSpace(options.InputFile))
        {
            var path = Path.GetFullPath(options.InputFile);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Input file not found: {path}", path);
            }

            return (await File.ReadAllTextAsync(path).ConfigureAwait(false)).Trim();
        }

        return ReadInteractiveRequirement();
    }

    private static string ReadInteractiveRequirement()
    {
        Console.WriteLine("Enter business requirement (blank line to finish):");
        var lines = new List<string>();

        while (true)
        {
            var line = Console.ReadLine();
            if (line is null || line.Length == 0)
            {
                break;
            }

            lines.Add(line);
        }

        return string.Join(Environment.NewLine, lines).Trim();
    }

    private static Task<bool> PromptApprovalAsync(bool autoApprove, CancellationToken cancellationToken)
    {
        if (autoApprove)
        {
            Console.WriteLine("Auto-approve enabled.");
            return Task.FromResult(true);
        }

        cancellationToken.ThrowIfCancellationRequested();
        Console.Write("Approve? [y/N] ");
        var answer = Console.ReadLine();
        var approved = string.Equals(answer?.Trim(), "y", StringComparison.OrdinalIgnoreCase)
            || string.Equals(answer?.Trim(), "yes", StringComparison.OrdinalIgnoreCase);

        return Task.FromResult(approved);
    }

    private static void PrintWorkflowOutcome(WorkflowResult result, string artifactsRoot)
    {
        Console.WriteLine();
        Console.WriteLine($"Workflow id       : {result.Instance.Id:D}");
        Console.WriteLine($"Workflow status   : {result.Instance.Status}");
        Console.WriteLine($"Input artifact    : {result.InputArtifact.Id:D}");

        if (result.OutputArtifact is not null)
        {
            var outputPath = Path.Combine(
                artifactsRoot,
                result.OutputArtifact.WorkflowInstanceId.ToString("D"),
                $"{result.OutputArtifact.Id:D}.json");

            Console.WriteLine($"Output artifact   : {result.OutputArtifact.Id:D}");
            Console.WriteLine($"Output file path  : {outputPath}");
        }

        if (!result.Succeeded)
        {
            Console.WriteLine($"Failure           : {result.FailureMessage}");
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine("""
            Usage:
              dotnet run --project Athlon.Spike.Console -- --text "As an employee I want..."
              dotnet run --project Athlon.Spike.Console -- --input examples/meal-allowance-requirement.txt
              dotnet run --project Athlon.Spike.Console -- --load <artifact-guid>
              Options: --auto-approve
            """);
    }
}

internal sealed class CliOptions
{
    public string? Text { get; private init; }
    public string? InputFile { get; private init; }
    public Guid? LoadArtifactId { get; private init; }
    public bool AutoApprove { get; private init; }

    public static CliOptions Parse(string[] args)
    {
        string? text = null;
        string? inputFile = null;
        Guid? loadId = null;
        var autoApprove = false;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            switch (arg)
            {
                case "--text":
                    text = RequireValue(args, ref i, "--text");
                    break;
                case "--input":
                    inputFile = RequireValue(args, ref i, "--input");
                    break;
                case "--load":
                    var raw = RequireValue(args, ref i, "--load");
                    if (!Guid.TryParse(raw, out var parsed))
                    {
                        throw new ArgumentException($"Invalid artifact GUID for --load: '{raw}'.");
                    }

                    loadId = parsed;
                    break;
                case "--auto-approve":
                    autoApprove = true;
                    break;
                default:
                    throw new ArgumentException($"Unknown argument: {arg}");
            }
        }

        if (loadId is not null && (text is not null || inputFile is not null))
        {
            throw new ArgumentException("--load cannot be combined with --text or --input.");
        }

        if (text is not null && inputFile is not null)
        {
            throw new ArgumentException("Use either --text or --input, not both.");
        }

        return new CliOptions
        {
            Text = text,
            InputFile = inputFile,
            LoadArtifactId = loadId,
            AutoApprove = autoApprove
        };
    }

    private static string RequireValue(string[] args, ref int index, string name)
    {
        if (index + 1 >= args.Length || args[index + 1].StartsWith("--", StringComparison.Ordinal))
        {
            throw new ArgumentException($"Missing value for {name}.");
        }

        index++;
        return args[index];
    }
}

internal static class SpikePaths
{
    public static string ResolveRoot()
    {
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var dir = new DirectoryInfo(start);
            while (dir is not null)
            {
                var schema = Path.Combine(dir.FullName, "schemas", "implementation-artifact.schema.json");
                if (File.Exists(schema))
                {
                    return dir.FullName;
                }

                dir = dir.Parent;
            }
        }

        throw new DirectoryNotFoundException(
            "Could not locate Spike_01 root (expected schemas/implementation-artifact.schema.json).");
    }

    public static string PromptTemplate(string root) =>
        Path.Combine(root, "prompts", "developer-v1.txt");

    public static string ImplementationSchema(string root) =>
        Path.Combine(root, "schemas", "implementation-artifact.schema.json");
}

internal static class EnvFile
{
    /// <summary>
    /// Loads KEY=VALUE pairs into process environment if the variable is not already set.
    /// Existing env vars win (so CI / shell overrides still work).
    /// </summary>
    public static void Load(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        foreach (var rawLine in File.ReadAllLines(path))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#') || !line.Contains('='))
            {
                continue;
            }

            var separator = line.IndexOf('=');
            var key = line[..separator].Trim();
            var value = line[(separator + 1)..].Trim().Trim('"').Trim('\'');

            if (key.Length == 0)
            {
                continue;
            }

            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }
}
