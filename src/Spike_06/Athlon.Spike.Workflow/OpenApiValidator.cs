using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Athlon.Spike.Workflow;

/// <summary>
/// Validates OpenAPI specifications and checks operation coverage by contract tests.
/// Phase 2 proof gate: OpenAPI valid + declared operations covered by tests.
/// </summary>
public static partial class OpenApiValidator
{
    private static readonly string[] OpenApiFileNames = ["openapi.yaml", "openapi.yml", "openapi.json"];

    /// <summary>
    /// Validates the OpenAPI spec in the publish directory and checks operation coverage.
    /// Returns (succeeded, output) where output contains validation details or error message.
    /// </summary>
    public static (bool Succeeded, string Output) ValidateAndCheckCoverage(
        string publishDirectory,
        string? contractTestProjectPath = null)
    {
        var output = new StringBuilder();

        var openapiPath = FindOpenApiFile(publishDirectory);
        if (openapiPath is null)
        {
            output.AppendLine("OpenAPI file not found in publish directory.");
            output.AppendLine($"Searched for: {string.Join(", ", OpenApiFileNames)}");
            return (false, output.ToString());
        }

        output.AppendLine($"OpenAPI file: {Path.GetRelativePath(publishDirectory, openapiPath)}");

        var (parseSucceeded, operations, parseError) = ParseOpenApiOperations(openapiPath);
        if (!parseSucceeded)
        {
            output.AppendLine($"OpenAPI parse failed: {parseError}");
            return (false, output.ToString());
        }

        output.AppendLine($"OpenAPI valid: {operations.Count} operation(s) declared");
        foreach (var op in operations)
        {
            output.AppendLine($"  - {op.Method.ToUpperInvariant()} {op.Path} ({op.OperationId ?? "no operationId"})");
        }

        var contractTestDir = contractTestProjectPath is not null
            ? Path.GetDirectoryName(contractTestProjectPath)
            : FindContractTestDirectory(publishDirectory);

        if (contractTestDir is null || !Directory.Exists(contractTestDir))
        {
            output.AppendLine("Contract test directory not found; skipping coverage check.");
            return (true, output.ToString());
        }

        var (coverageOk, coverageDetails) = CheckOperationCoverage(operations, contractTestDir);
        output.AppendLine();
        output.AppendLine("Operation coverage check:");
        output.Append(coverageDetails);

        if (!coverageOk)
        {
            output.AppendLine();
            output.AppendLine("FAIL: Not all operations are covered by contract tests.");
            return (false, output.ToString());
        }

        output.AppendLine();
        output.AppendLine("PASS: All declared operations have test coverage.");
        return (true, output.ToString());
    }

    private static string? FindOpenApiFile(string directory)
    {
        foreach (var fileName in OpenApiFileNames)
        {
            var path = Path.Combine(directory, fileName);
            if (File.Exists(path))
            {
                return path;
            }
        }

        return null;
    }

    private static (bool Succeeded, List<OpenApiOperation> Operations, string? Error) ParseOpenApiOperations(
        string filePath)
    {
        try
        {
            var content = File.ReadAllText(filePath);
            var extension = Path.GetExtension(filePath).ToLowerInvariant();

            if (extension == ".json")
            {
                return ParseJsonOpenApi(content);
            }

            return ParseYamlOpenApi(content);
        }
        catch (Exception ex)
        {
            return (false, [], ex.Message);
        }
    }

    private static (bool Succeeded, List<OpenApiOperation> Operations, string? Error) ParseYamlOpenApi(string content)
    {
        try
        {
            var yaml = new YamlStream();
            using var reader = new StringReader(content);
            yaml.Load(reader);

            if (yaml.Documents.Count == 0)
            {
                return (false, [], "Empty YAML document.");
            }

            var root = yaml.Documents[0].RootNode as YamlMappingNode;
            if (root is null)
            {
                return (false, [], "YAML root is not a mapping.");
            }

            if (!root.Children.ContainsKey("openapi") && !root.Children.ContainsKey("swagger"))
            {
                return (false, [], "Missing 'openapi' or 'swagger' version field.");
            }

            var operations = new List<OpenApiOperation>();

            if (root.Children.TryGetValue("paths", out var pathsNode) && pathsNode is YamlMappingNode paths)
            {
                foreach (var pathEntry in paths.Children)
                {
                    var pathKey = ((YamlScalarNode)pathEntry.Key).Value ?? "";
                    if (pathEntry.Value is not YamlMappingNode pathItem)
                    {
                        continue;
                    }

                    foreach (var methodEntry in pathItem.Children)
                    {
                        var method = ((YamlScalarNode)methodEntry.Key).Value ?? "";
                        if (!IsHttpMethod(method))
                        {
                            continue;
                        }

                        string? operationId = null;
                        if (methodEntry.Value is YamlMappingNode opNode &&
                            opNode.Children.TryGetValue("operationId", out var opIdNode) &&
                            opIdNode is YamlScalarNode opIdScalar)
                        {
                            operationId = opIdScalar.Value;
                        }

                        operations.Add(new OpenApiOperation(pathKey, method, operationId));
                    }
                }
            }

            return (true, operations, null);
        }
        catch (YamlException ex)
        {
            return (false, [], $"YAML parse error: {ex.Message}");
        }
    }

    private static (bool Succeeded, List<OpenApiOperation> Operations, string? Error) ParseJsonOpenApi(string content)
    {
        try
        {
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            if (!root.TryGetProperty("openapi", out _) && !root.TryGetProperty("swagger", out _))
            {
                return (false, [], "Missing 'openapi' or 'swagger' version field.");
            }

            var operations = new List<OpenApiOperation>();

            if (root.TryGetProperty("paths", out var paths))
            {
                foreach (var pathEntry in paths.EnumerateObject())
                {
                    var pathKey = pathEntry.Name;
                    foreach (var methodEntry in pathEntry.Value.EnumerateObject())
                    {
                        var method = methodEntry.Name;
                        if (!IsHttpMethod(method))
                        {
                            continue;
                        }

                        string? operationId = null;
                        if (methodEntry.Value.TryGetProperty("operationId", out var opIdProp))
                        {
                            operationId = opIdProp.GetString();
                        }

                        operations.Add(new OpenApiOperation(pathKey, method, operationId));
                    }
                }
            }

            return (true, operations, null);
        }
        catch (JsonException ex)
        {
            return (false, [], $"JSON parse error: {ex.Message}");
        }
    }

    private static bool IsHttpMethod(string method)
    {
        return method.ToLowerInvariant() switch
        {
            "get" or "post" or "put" or "patch" or "delete" or "head" or "options" => true,
            _ => false
        };
    }

    private static string? FindContractTestDirectory(string publishDirectory)
    {
        var testProjects = Directory.GetFiles(publishDirectory, "*.ContractTests.csproj", SearchOption.AllDirectories);
        if (testProjects.Length > 0)
        {
            return Path.GetDirectoryName(testProjects[0]);
        }

        testProjects = Directory.GetFiles(publishDirectory, "*Tests.csproj", SearchOption.AllDirectories);
        return testProjects.Length > 0 ? Path.GetDirectoryName(testProjects[0]) : null;
    }

    /// <summary>
    /// Checks if declared OpenAPI operations are covered by contract tests.
    /// Coverage algorithm: For each operation, check if test files contain references to the path
    /// (exact path string, operationId, or path segments that identify the endpoint).
    /// </summary>
    private static (bool AllCovered, string Details) CheckOperationCoverage(
        List<OpenApiOperation> operations,
        string contractTestDir)
    {
        var details = new StringBuilder();
        var testFiles = Directory.GetFiles(contractTestDir, "*.cs", SearchOption.AllDirectories);
        var testContent = new StringBuilder();

        foreach (var testFile in testFiles)
        {
            testContent.AppendLine(File.ReadAllText(testFile));
        }

        var allTestContent = testContent.ToString();
        var allCovered = true;

        foreach (var op in operations)
        {
            var covered = IsOperationCovered(op, allTestContent);
            var status = covered ? "✓" : "✗";
            details.AppendLine($"  {status} {op.Method.ToUpperInvariant()} {op.Path}");

            if (!covered)
            {
                allCovered = false;
            }
        }

        return (allCovered, details.ToString());
    }

    /// <summary>
    /// Determines if an operation is covered by test content.
    /// Heuristic: Check for path string, operationId, or normalized path pattern in test code.
    /// </summary>
    private static bool IsOperationCovered(OpenApiOperation op, string testContent)
    {
        if (!string.IsNullOrEmpty(op.OperationId) &&
            testContent.Contains(op.OperationId, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (testContent.Contains($"\"{op.Path}\"", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var pathPattern = NormalizePathForSearch(op.Path);
        if (Regex.IsMatch(testContent, pathPattern, RegexOptions.IgnoreCase))
        {
            return true;
        }

        var pathWithoutParams = PathParamRegex().Replace(op.Path, "/");
        pathWithoutParams = pathWithoutParams.TrimEnd('/');
        if (!string.IsNullOrEmpty(pathWithoutParams) &&
            testContent.Contains($"\"{pathWithoutParams}", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Normalizes an OpenAPI path for regex search in test content.
    /// Converts /path/{param} to /path/[^"]+
    /// </summary>
    private static string NormalizePathForSearch(string path)
    {
        var escaped = Regex.Escape(path);
        var pattern = PathParamEscapedRegex().Replace(escaped, "[^\"]+");
        return $"\"{pattern}\"?";
    }

    [GeneratedRegex(@"/\{[^}]+\}")]
    private static partial Regex PathParamRegex();

    [GeneratedRegex(@"/\\{[^}]+\\}")]
    private static partial Regex PathParamEscapedRegex();
}

/// <summary>
/// Represents a declared OpenAPI operation.
/// </summary>
public sealed record OpenApiOperation(string Path, string Method, string? OperationId);
