using System.Text;

namespace Athlon.Spike.Workflow;

/// <summary>
/// Minimal unified-diff applier for Spike_04 PatchPackage (modify / create / delete).
/// Supports standard @@ hunks with --- / +++ headers per coder-v1 conventions.
/// </summary>
internal static class UnifiedDiffApplier
{
    public static string ApplyToContent(string originalContent, string unifiedDiff)
    {
        ArgumentNullException.ThrowIfNull(unifiedDiff);

        var diffLines = SplitLines(unifiedDiff);
        if (diffLines.Count == 0)
        {
            throw new InvalidOperationException("unifiedDiff is empty.");
        }

        var i = 0;
        // Skip optional headers (--- / +++). Hunks may start immediately.
        while (i < diffLines.Count &&
               (diffLines[i].StartsWith("---", StringComparison.Ordinal) ||
                diffLines[i].StartsWith("+++", StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(diffLines[i])))
        {
            i++;
        }

        var oldLines = SplitLines(originalContent ?? string.Empty);
        var result = new List<string>(oldLines.Count);
        var oldIndex = 0; // 0-based cursor into oldLines

        while (i < diffLines.Count)
        {
            var line = diffLines[i];
            if (string.IsNullOrWhiteSpace(line))
            {
                i++;
                continue;
            }

            if (!line.StartsWith("@@", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Expected unified-diff hunk header starting with '@@', got: '{line}'.");
            }

            var (oldStart, oldCount, _, _) = ParseHunkHeader(line);
            i++;

            // Copy unchanged lines before this hunk (oldStart is 1-based; 0 means empty old file).
            var copyUntil = oldCount == 0 ? oldStart : oldStart - 1;
            if (copyUntil < oldIndex || copyUntil > oldLines.Count)
            {
                throw new InvalidOperationException(
                    $"Hunk old start {oldStart} is out of range for file with {oldLines.Count} lines.");
            }

            while (oldIndex < copyUntil)
            {
                result.Add(oldLines[oldIndex]);
                oldIndex++;
            }

            var oldConsumed = 0;
            while (i < diffLines.Count &&
                   !diffLines[i].StartsWith("@@", StringComparison.Ordinal) &&
                   !diffLines[i].StartsWith("---", StringComparison.Ordinal) &&
                   !diffLines[i].StartsWith("+++", StringComparison.Ordinal))
            {
                var hunkLine = diffLines[i];
                if (hunkLine.Length == 0)
                {
                    // Empty line in diff body is treated as context with empty content
                    // (rare); prefer requiring a prefix.
                    throw new InvalidOperationException(
                        "unifiedDiff hunk line is empty (missing ' ', '+', or '-' prefix).");
                }

                var prefix = hunkLine[0];
                var body = hunkLine.Length > 1 ? hunkLine[1..] : string.Empty;

                switch (prefix)
                {
                    case ' ':
                        if (oldIndex >= oldLines.Count ||
                            !string.Equals(oldLines[oldIndex], body, StringComparison.Ordinal))
                        {
                            throw new InvalidOperationException(
                                $"Context mismatch at line {oldIndex + 1}: expected '{body}'.");
                        }

                        result.Add(oldLines[oldIndex]);
                        oldIndex++;
                        oldConsumed++;
                        break;

                    case '-':
                        if (oldIndex >= oldLines.Count ||
                            !string.Equals(oldLines[oldIndex], body, StringComparison.Ordinal))
                        {
                            throw new InvalidOperationException(
                                $"Removal mismatch at line {oldIndex + 1}: expected '{body}'.");
                        }

                        oldIndex++;
                        oldConsumed++;
                        break;

                    case '+':
                        result.Add(body);
                        break;

                    case '\\':
                        // "\ No newline at end of file" — ignore
                        break;

                    default:
                        throw new InvalidOperationException(
                            $"Unexpected unified-diff line prefix '{prefix}' in: '{hunkLine}'.");
                }

                i++;
            }

            if (oldCount > 0 && oldConsumed != oldCount)
            {
                throw new InvalidOperationException(
                    $"Hunk claimed {oldCount} old lines but consumed {oldConsumed}.");
            }
        }

        while (oldIndex < oldLines.Count)
        {
            result.Add(oldLines[oldIndex]);
            oldIndex++;
        }

        return JoinLines(result, DetectNewLine(originalContent));
    }

    private static (int OldStart, int OldCount, int NewStart, int NewCount) ParseHunkHeader(string header)
    {
        // @@ -l,s +l,s @@ optional trailing text
        var at = header.IndexOf("@@", 2, StringComparison.Ordinal);
        var core = at > 0 ? header[2..at].Trim() : header.Trim(' ', '@');
        var parts = core.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2 ||
            !parts[0].StartsWith('-') ||
            !parts[1].StartsWith('+'))
        {
            throw new InvalidOperationException($"Malformed hunk header: '{header}'.");
        }

        var (oldStart, oldCount) = ParseRange(parts[0][1..]);
        var (newStart, newCount) = ParseRange(parts[1][1..]);
        return (oldStart, oldCount, newStart, newCount);
    }

    private static (int Start, int Count) ParseRange(string range)
    {
        var comma = range.IndexOf(',');
        if (comma < 0)
        {
            if (!int.TryParse(range, out var startOnly))
            {
                throw new InvalidOperationException($"Malformed hunk range: '{range}'.");
            }

            return (startOnly, 1);
        }

        if (!int.TryParse(range[..comma], out var start) ||
            !int.TryParse(range[(comma + 1)..], out var count))
        {
            throw new InvalidOperationException($"Malformed hunk range: '{range}'.");
        }

        return (start, count);
    }

    private static List<string> SplitLines(string text)
    {
        var lines = new List<string>();
        if (string.IsNullOrEmpty(text))
        {
            return lines;
        }

        using var reader = new StringReader(text);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            lines.Add(line);
        }

        // StringReader drops a final empty line after trailing newline; unified diffs
        // usually don't rely on that. Preserve no trailing empty for empty files.
        return lines;
    }

    private static string DetectNewLine(string? content)
    {
        if (content is not null && content.Contains("\r\n", StringComparison.Ordinal))
        {
            return "\r\n";
        }

        return "\n";
    }

    private static string JoinLines(IReadOnlyList<string> lines, string newLine)
    {
        if (lines.Count == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        for (var i = 0; i < lines.Count; i++)
        {
            if (i > 0)
            {
                sb.Append(newLine);
            }

            sb.Append(lines[i]);
        }

        // Preserve a trailing newline if the original had one — callers that need
        // exact EOF can include "\ No newline" handling; for Spike_04 fixtures we
        // always end with a newline when content is non-empty (dotnet-friendly).
        if (lines.Count > 0)
        {
            sb.Append(newLine);
        }

        return sb.ToString();
    }
}
