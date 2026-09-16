using System.Text.RegularExpressions;

namespace Mnemosyne;

/// <summary>Planning intent for a resolved internal markdown link.</summary>
public enum LinkRole
{
    Generic = 0,
    Plans = 1,
    DependsOn = 2,
    Parent = 3,
    SeeAlso = 4,
}

/// <summary>Deterministic link-role heuristics (no LLM).</summary>
public static class LinkRoleClassifier
{
    private static readonly Regex EpicBriefPath = new(
        @"^docs/roadmap/([^/]+)/(\d{2}-.+\.md)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex EpicReadmePath = new(
        @"^docs/roadmap/([^/]+)/readme\.md$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Classify a resolved internal link using source path, link text, raw target, and resolved target path.
    /// </summary>
    public static LinkRole Classify(
        string? sourcePath,
        MarkdownLink link,
        string? resolvedTargetPath)
    {
        if (link.IsExternal)
        {
            return LinkRole.Generic;
        }

        var textLower = link.Text.Trim().ToLowerInvariant();
        if (textLower.Contains("depends on", StringComparison.Ordinal) ||
            textLower.Contains("depends", StringComparison.Ordinal))
        {
            return LinkRole.DependsOn;
        }

        if (textLower.Contains("see also", StringComparison.Ordinal))
        {
            return LinkRole.SeeAlso;
        }

        var source = Normalize(sourcePath);
        var target = Normalize(resolvedTargetPath);
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target))
        {
            return LinkRole.Generic;
        }

        if (target.StartsWith("docs/done/", StringComparison.OrdinalIgnoreCase))
        {
            return LinkRole.SeeAlso;
        }

        if (IsParentLink(source, target, link.Target))
        {
            return LinkRole.Parent;
        }

        if (IsPlanningLink(source, target))
        {
            return LinkRole.Plans;
        }

        return LinkRole.Generic;
    }

    public static string ToKindString(LinkRole role) => role switch
    {
        LinkRole.Plans => "Plans",
        LinkRole.DependsOn => "DependsOn",
        LinkRole.Parent => "Parent",
        LinkRole.SeeAlso => "SeeAlso",
        _ => "Generic",
    };

    private static bool IsParentLink(string source, string target, string rawTarget)
    {
        var raw = rawTarget.Trim();
        if (raw.Contains("..", StringComparison.Ordinal))
        {
            if (target.EndsWith("/roadmap.md", StringComparison.OrdinalIgnoreCase) ||
                target.Equals("docs/roadmap/roadmap.md", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (EpicReadmePath.IsMatch(target) &&
                EpicBriefPath.IsMatch(source) &&
                string.Equals(
                    EpicReadmePath.Match(target).Groups[1].Value,
                    EpicBriefPath.Match(source).Groups[1].Value,
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        if (EpicBriefPath.IsMatch(source) &&
            EpicReadmePath.IsMatch(target) &&
            string.Equals(
                EpicBriefPath.Match(source).Groups[1].Value,
                EpicReadmePath.Match(target).Groups[1].Value,
                StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private static bool IsPlanningLink(string source, string target)
    {
        if (!target.StartsWith("docs/roadmap/", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (source.Equals("docs/roadmap/roadmap.md", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (source.EndsWith("/roadmap.md", StringComparison.OrdinalIgnoreCase) &&
            source.StartsWith("docs/roadmap/", StringComparison.OrdinalIgnoreCase) &&
            !source.Equals("docs/roadmap/roadmap.md", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var epicReadme = EpicReadmePath.Match(source);
        if (epicReadme.Success)
        {
            var epic = epicReadme.Groups[1].Value;
            if (target.StartsWith($"docs/roadmap/{epic}/", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string? Normalize(string? path) =>
        string.IsNullOrWhiteSpace(path) ? null : MarkdownPathIndex.NormalizePath(path);
}
