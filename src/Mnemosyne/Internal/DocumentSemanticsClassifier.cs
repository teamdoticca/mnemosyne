using System.Text.RegularExpressions;

namespace Mnemosyne.Internal;

internal static class DocumentSemanticsClassifier
{
    private static readonly Regex StatusHeaderLine = new(
        @"^\*\*Status:\*\*\s*(.+)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static DocumentSemantics Classify(
        string? path,
        MarkdownFrontMatter frontMatter,
        IReadOnlyList<MarkdownHeading> headings,
        IReadOnlyList<string> bodyLines,
        int bodyStartLine,
        MarkdownConventionsOptions? conventions = null,
        List<string>? rulesFired = null)
    {
        _ = bodyStartLine;
        conventions ??= MarkdownConventionsOptions.CreateDefault();
        var normalizedPath = string.IsNullOrWhiteSpace(path)
            ? null
            : MarkdownPathIndex.NormalizePath(path);
        var lower = normalizedPath?.ToLowerInvariant();

        var docKind = ClassifyDocKind(lower, conventions, rulesFired);

        var statusRaw = string.IsNullOrWhiteSpace(frontMatter.Status)
            ? TryParseStatusHeader(bodyLines)
            : frontMatter.Status.Trim();
        if (!string.IsNullOrWhiteSpace(frontMatter.Status))
        {
            rulesFired?.Add("front-matter: status");
        }
        else if (!string.IsNullOrWhiteSpace(statusRaw))
        {
            rulesFired?.Add("body: **Status:** header");
        }

        if (string.IsNullOrWhiteSpace(statusRaw) &&
            lower is not null &&
            IsEpicLeaf(lower, conventions, "status.md"))
        {
            statusRaw = TryParseStatusFileBody(bodyLines);
            if (!string.IsNullOrWhiteSpace(statusRaw))
            {
                rulesFired?.Add("body: status.md token");
            }
        }

        if (string.IsNullOrWhiteSpace(statusRaw) && docKind == DocumentKind.Index)
        {
            statusRaw = TryInferAggregateStatusFromTables(bodyLines, conventions);
            if (!string.IsNullOrWhiteSpace(statusRaw))
            {
                rulesFired?.Add($"table-aggregate: {statusRaw}");
            }
        }

        var lifecycle = NormalizeLifecycle(statusRaw, lower, conventions, rulesFired);
        var planningRef = !string.IsNullOrWhiteSpace(frontMatter.PlanningRef)
            ? frontMatter.PlanningRef.Trim()
            : ExtractPlanningRef(lower, docKind, conventions);
        if (!string.IsNullOrWhiteSpace(frontMatter.PlanningRef))
        {
            rulesFired?.Add($"front-matter: mnemosyne.planningRef={planningRef}");
        }

        var keySections = DetectKeySections(headings, conventions);

        return new DocumentSemantics
        {
            DocKind = docKind,
            Lifecycle = lifecycle,
            PlanningRef = planningRef,
            KeySections = keySections,
            Owner = string.IsNullOrWhiteSpace(frontMatter.Owner) ? null : frontMatter.Owner.Trim(),
            StatusRaw = string.IsNullOrWhiteSpace(statusRaw) ? null : statusRaw,
        };
    }

    private static DocumentKind ClassifyDocKind(
        string? lower,
        MarkdownConventionsOptions conventions,
        List<string>? rulesFired)
    {
        if (string.IsNullOrEmpty(lower))
        {
            return DocumentKind.Unknown;
        }

        foreach (var rule in conventions.PathDocKindRules)
        {
            if (!PathConventionMatcher.MatchesGlob(lower, rule.Pattern))
            {
                continue;
            }

            if (Enum.TryParse<DocumentKind>(rule.DocKind, ignoreCase: true, out var kind) &&
                kind != DocumentKind.Unknown)
            {
                rulesFired?.Add($"pathDocKindRule: {rule.Pattern} → {kind}");
                return kind;
            }
        }

        foreach (var index in conventions.ProgramIndexPaths)
        {
            if (lower.Equals(
                    PathConventionMatcher.Normalize(index.Path).ToLowerInvariant(),
                    StringComparison.Ordinal))
            {
                rulesFired?.Add($"programIndexPath: Index ({index.Path})");
                return DocumentKind.Index;
            }
        }

        if (IsProgramDocsIndex(lower, conventions))
        {
            rulesFired?.Add("shallowProgramDoc: Index");
            return DocumentKind.Index;
        }

        if (lower is "docs/readme.md" or "documentation/readme.md")
        {
            rulesFired?.Add("path: Reference");
            return DocumentKind.Reference;
        }

        if (IsEpicRootReadme(lower, conventions))
        {
            rulesFired?.Add("path: Index (epic roots readme)");
            return DocumentKind.Index;
        }

        if (IsNumberedBrief(lower, conventions) || IsEpicLeaf(lower, conventions, "design.md"))
        {
            rulesFired?.Add("path: Brief");
            return DocumentKind.Brief;
        }

        if (IsEpicLeaf(lower, conventions, "status.md"))
        {
            rulesFired?.Add("path: Brief (status.md)");
            return DocumentKind.Brief;
        }

        if (IsEpicLeaf(lower, conventions, "readme.md"))
        {
            rulesFired?.Add("path: Epic");
            return DocumentKind.Epic;
        }

        if (IsNestedProgramRoadmap(lower, conventions))
        {
            rulesFired?.Add("path: nested program Index");
            return DocumentKind.Index;
        }

        if (PathConventionMatcher.IsUnderAnyRoot(lower, conventions.ArchiveRoots) ||
            PathConventionMatcher.IsUnderAnyRoot(lower, conventions.EpicRoots))
        {
            return DocumentKind.Unknown;
        }

        return DocumentKind.Unknown;
    }

    private static bool IsProgramDocsIndex(string lower, MarkdownConventionsOptions conventions)
    {
        var file = Path.GetFileName(lower);
        var segments = lower.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var isPlanningFile =
            PathConventionMatcher.FileNameEqualsAny(file, conventions.RoadmapFileNames) ||
            PathConventionMatcher.FileNameEqualsAny(file, conventions.ExecutionPlanFileNames) ||
            PathConventionMatcher.FileNameEqualsAny(file, conventions.StrategyFileNames);

        if (!isPlanningFile)
        {
            return false;
        }

        return segments.Length switch
        {
            1 => true,
            2 => PathConventionMatcher.SegmentEqualsAny(segments[0], conventions.DocsHostFolders),
            3 => PathConventionMatcher.SegmentEqualsAny(segments[0], conventions.DocsHostFolders) &&
                 PathConventionMatcher.SegmentEqualsAny(segments[1], conventions.PlanningHostFolders),
            _ => false,
        };
    }

    private static bool IsNestedProgramRoadmap(string lower, MarkdownConventionsOptions conventions)
    {
        if (!lower.EndsWith("/roadmap.md", StringComparison.Ordinal))
        {
            return false;
        }

        foreach (var root in conventions.EpicRoots)
        {
            var prefix = PathConventionMatcher.Normalize(root).ToLowerInvariant().TrimEnd('/') + "/";
            if (!lower.StartsWith(prefix, StringComparison.Ordinal) || lower == prefix + "roadmap.md")
            {
                continue;
            }

            var remainder = lower[prefix.Length..];
            var slash = remainder.IndexOf('/');
            if (slash > 0 && remainder[(slash + 1)..] == "roadmap.md")
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsEpicRootReadme(string lower, MarkdownConventionsOptions conventions)
    {
        foreach (var root in conventions.EpicRoots)
        {
            var r = PathConventionMatcher.Normalize(root).ToLowerInvariant().TrimEnd('/');
            if (lower == r + "/readme.md")
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsNumberedBrief(string lower, MarkdownConventionsOptions conventions)
    {
        foreach (var root in conventions.EpicRoots)
        {
            var prefix = PathConventionMatcher.Normalize(root).ToLowerInvariant().TrimEnd('/') + "/";
            if (!lower.StartsWith(prefix, StringComparison.Ordinal))
            {
                continue;
            }

            var remainder = lower[prefix.Length..];
            var slash = remainder.IndexOf('/');
            if (slash <= 0)
            {
                continue;
            }

            var file = remainder[(slash + 1)..];
            if (Regex.IsMatch(file, @"^\d{2}-.+\.md$", RegexOptions.IgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsEpicLeaf(string lower, MarkdownConventionsOptions conventions, string fileName)
    {
        foreach (var root in conventions.EpicRoots)
        {
            var prefix = PathConventionMatcher.Normalize(root).ToLowerInvariant().TrimEnd('/') + "/";
            if (!lower.StartsWith(prefix, StringComparison.Ordinal))
            {
                continue;
            }

            var remainder = lower[prefix.Length..];
            var slash = remainder.IndexOf('/');
            if (slash <= 0)
            {
                continue;
            }

            if (remainder[(slash + 1)..].Equals(fileName, StringComparison.OrdinalIgnoreCase) &&
                remainder.IndexOf('/', slash + 1) < 0)
            {
                return true;
            }
        }

        return false;
    }

    private static string? ExtractPlanningRef(
        string? lower,
        DocumentKind kind,
        MarkdownConventionsOptions conventions)
    {
        if (string.IsNullOrEmpty(lower))
        {
            return null;
        }

        if (kind is DocumentKind.Unknown or DocumentKind.Index or DocumentKind.Reference)
        {
            if (IsNestedProgramRoadmap(lower, conventions))
            {
                foreach (var root in conventions.EpicRoots)
                {
                    var prefix = PathConventionMatcher.Normalize(root).ToLowerInvariant().TrimEnd('/') + "/";
                    if (!lower.StartsWith(prefix, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var remainder = lower[prefix.Length..];
                    var slash = remainder.IndexOf('/');
                    return slash > 0 ? remainder[..slash] : null;
                }
            }

            return null;
        }

        foreach (var root in conventions.EpicRoots)
        {
            var prefix = PathConventionMatcher.Normalize(root).ToLowerInvariant().TrimEnd('/') + "/";
            if (!lower.StartsWith(prefix, StringComparison.Ordinal))
            {
                continue;
            }

            var remainder = lower[prefix.Length..];
            var idx = remainder.IndexOf('/');
            return idx > 0 ? remainder[..idx] : null;
        }

        return null;
    }

    private static DocumentLifecycle NormalizeLifecycle(
        string? statusRaw,
        string? lower,
        MarkdownConventionsOptions conventions,
        List<string>? rulesFired)
    {
        if (!string.IsNullOrEmpty(lower) &&
            PathConventionMatcher.IsUnderAnyRoot(lower, conventions.ArchiveRoots))
        {
            rulesFired?.Add("path: archiveRoots → Done");
            return DocumentLifecycle.Done;
        }

        if (string.IsNullOrWhiteSpace(statusRaw))
        {
            return DocumentLifecycle.Unknown;
        }

        var s = statusRaw.Trim();
        if (conventions.StatusTokenAliases.TryGetValue(s, out var alias) &&
            Enum.TryParse<DocumentLifecycle>(alias, ignoreCase: true, out var aliased) &&
            aliased != DocumentLifecycle.Unknown)
        {
            rulesFired?.Add($"statusAlias: {s} → {aliased}");
            return aliased;
        }

        if (s.Contains("active", StringComparison.OrdinalIgnoreCase) ||
            s.Contains("in progress", StringComparison.OrdinalIgnoreCase) ||
            s.Equals("in_progress", StringComparison.OrdinalIgnoreCase))
        {
            return DocumentLifecycle.Active;
        }

        if (s.Contains("planned", StringComparison.OrdinalIgnoreCase))
        {
            return DocumentLifecycle.Planned;
        }

        if (s.Contains("draft", StringComparison.OrdinalIgnoreCase))
        {
            return DocumentLifecycle.Draft;
        }

        if (s.Contains("done", StringComparison.OrdinalIgnoreCase) ||
            s.Contains("complete", StringComparison.OrdinalIgnoreCase) ||
            s.Contains("shipped", StringComparison.OrdinalIgnoreCase))
        {
            return DocumentLifecycle.Done;
        }

        return DocumentLifecycle.Unknown;
    }

    private static string? TryParseStatusHeader(IReadOnlyList<string> bodyLines)
    {
        foreach (var line in bodyLines.Take(40))
        {
            var match = StatusHeaderLine.Match(line.Trim());
            if (match.Success)
            {
                return match.Groups[1].Value.Trim();
            }
        }

        return null;
    }

    private static string? TryParseStatusFileBody(IReadOnlyList<string> bodyLines)
    {
        foreach (var raw in bodyLines.Take(30))
        {
            var line = raw.Trim().Trim('`');
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
            {
                continue;
            }

            if (line.Equals("done", StringComparison.OrdinalIgnoreCase) ||
                line.Equals("planned", StringComparison.OrdinalIgnoreCase) ||
                line.Equals("in_progress", StringComparison.OrdinalIgnoreCase) ||
                line.Equals("in progress", StringComparison.OrdinalIgnoreCase) ||
                line.Equals("draft", StringComparison.OrdinalIgnoreCase) ||
                line.Equals("active", StringComparison.OrdinalIgnoreCase))
            {
                return line;
            }
        }

        return null;
    }

    private static string? TryInferAggregateStatusFromTables(
        IReadOnlyList<string> bodyLines,
        MarkdownConventionsOptions conventions)
    {
        int? statusCol = null;
        var tokens = new List<string>();
        var columns = conventions.StatusTableColumns.Count > 0
            ? conventions.StatusTableColumns
            : ["Status", "State"];

        foreach (var raw in bodyLines)
        {
            var line = raw.Trim();
            if (!line.StartsWith('|'))
            {
                statusCol = null;
                continue;
            }

            var cells = SplitMarkdownTableCells(line);
            if (cells.Count == 0 || cells.All(IsMarkdownTableSeparatorCell))
            {
                continue;
            }

            if (statusCol is null)
            {
                for (var i = 0; i < cells.Count; i++)
                {
                    if (PathConventionMatcher.SegmentEqualsAny(cells[i], columns))
                    {
                        statusCol = i;
                        break;
                    }
                }

                continue;
            }

            if (statusCol.Value >= cells.Count)
            {
                continue;
            }

            if (TryMapStatusCell(cells[statusCol.Value], conventions, out var token))
            {
                tokens.Add(token);
            }
        }

        if (tokens.Count == 0)
        {
            return null;
        }

        if (tokens.Exists(t => t is "active"))
        {
            return "active";
        }

        if (tokens.Exists(t => t is "planned"))
        {
            return "planned";
        }

        if (tokens.Exists(t => t is "draft"))
        {
            return "draft";
        }

        if (tokens.TrueForAll(t => t is "done"))
        {
            return "done";
        }

        return null;
    }

    private static List<string> SplitMarkdownTableCells(string line)
    {
        var trimmed = line.Trim();
        if (trimmed.StartsWith('|'))
        {
            trimmed = trimmed[1..];
        }

        if (trimmed.EndsWith('|'))
        {
            trimmed = trimmed[..^1];
        }

        return trimmed.Split('|').Select(static c => c.Trim()).ToList();
    }

    private static bool IsMarkdownTableSeparatorCell(string cell) =>
        cell.Length > 0 && cell.All(static ch => ch is '-' or ':' or ' ');

    private static bool TryMapStatusCell(
        string cell,
        MarkdownConventionsOptions conventions,
        out string token)
    {
        token = "";
        var s = cell.Trim().Trim('`').Trim('*');
        if (string.IsNullOrWhiteSpace(s))
        {
            return false;
        }

        if (conventions.StatusTokenAliases.TryGetValue(s, out var alias))
        {
            if (alias.Equals("Active", StringComparison.OrdinalIgnoreCase))
            {
                token = "active";
                return true;
            }

            if (alias.Equals("Done", StringComparison.OrdinalIgnoreCase))
            {
                token = "done";
                return true;
            }

            if (alias.Equals("Planned", StringComparison.OrdinalIgnoreCase))
            {
                token = "planned";
                return true;
            }

            if (alias.Equals("Draft", StringComparison.OrdinalIgnoreCase))
            {
                token = "draft";
                return true;
            }
        }

        var lower = s.ToLowerInvariant();
        if (lower.StartsWith("in progress", StringComparison.Ordinal) ||
            lower.StartsWith("in_progress", StringComparison.Ordinal) ||
            lower.Equals("active", StringComparison.Ordinal) ||
            lower.Equals("wip", StringComparison.Ordinal) ||
            lower.Equals("ip", StringComparison.Ordinal) ||
            lower.StartsWith("active ", StringComparison.Ordinal) ||
            lower.StartsWith("active—", StringComparison.Ordinal) ||
            lower.StartsWith("active-", StringComparison.Ordinal))
        {
            token = "active";
            return true;
        }

        if (lower.Equals("planned", StringComparison.Ordinal) ||
            lower.StartsWith("planned ", StringComparison.Ordinal))
        {
            token = "planned";
            return true;
        }

        if (lower.Equals("draft", StringComparison.Ordinal) ||
            lower.StartsWith("draft ", StringComparison.Ordinal))
        {
            token = "draft";
            return true;
        }

        if (lower.Equals("done", StringComparison.Ordinal) ||
            lower.Equals("complete", StringComparison.Ordinal) ||
            lower.Equals("completed", StringComparison.Ordinal) ||
            lower.Equals("shipped", StringComparison.Ordinal) ||
            lower.StartsWith("done ", StringComparison.Ordinal) ||
            lower.StartsWith("done—", StringComparison.Ordinal) ||
            lower.StartsWith("done-", StringComparison.Ordinal) ||
            lower.StartsWith("complete ", StringComparison.Ordinal) ||
            lower.StartsWith("shipped ", StringComparison.Ordinal))
        {
            token = "done";
            return true;
        }

        return false;
    }

    private static IReadOnlyList<DocumentKeySection> DetectKeySections(
        IReadOnlyList<MarkdownHeading> headings,
        MarkdownConventionsOptions conventions)
    {
        var names = new HashSet<string>(
            conventions.KeySectionNames.Count > 0
                ? conventions.KeySectionNames
                : ["Active", "Next", "Exit criteria", "Sequencing", "Completed", "Completed (archived / living)"],
            StringComparer.OrdinalIgnoreCase);

        var sections = new List<DocumentKeySection>();
        foreach (var heading in headings)
        {
            if (heading.Level != 2 || !names.Contains(heading.Text.Trim()))
            {
                continue;
            }

            sections.Add(new DocumentKeySection
            {
                Name = heading.Text.Trim(),
                StartLine = heading.StartLine,
                EndLine = heading.EndLine,
            });
        }

        return sections;
    }
}
