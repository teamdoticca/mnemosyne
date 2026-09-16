namespace Mnemosyne.Internal;

internal static class SignificanceTagger
{
    public static IReadOnlyList<SignificanceTag> Classify(
        string? path,
        MarkdownFrontMatter frontMatter,
        MarkdownConventionsOptions? conventions = null,
        List<string>? rulesFired = null)
    {
        conventions ??= MarkdownConventionsOptions.CreateDefault();
        var tags = new HashSet<SignificanceTag>();

        if (!frontMatter.ClearBuiltInTags)
        {
            foreach (var t in FromPath(path, conventions, rulesFired))
            {
                tags.Add(t);
            }

            foreach (var t in FromFileNameHints(frontMatter, rulesFired))
            {
                tags.Add(t);
            }
        }
        else
        {
            rulesFired?.Add("front-matter: mnemosyne.clearBuiltIns");
        }

        foreach (var t in frontMatter.MnemosyneTags)
        {
            tags.Add(t);
            rulesFired?.Add($"front-matter: mnemosyne.tags={t}");
        }

        if (!string.IsNullOrWhiteSpace(frontMatter.CommitSha))
        {
            tags.Add(SignificanceTag.CommitNote);
            rulesFired?.Add("front-matter: commitSha");
        }

        if (tags.Count == 0)
        {
            tags.Add(SignificanceTag.Other);
            rulesFired?.Add("fallback: Other");
        }

        return tags.OrderBy(t => t).ToArray();
    }

    private static IEnumerable<SignificanceTag> FromPath(
        string? path,
        MarkdownConventionsOptions conventions,
        List<string>? rulesFired)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            yield break;
        }

        var p = MarkdownPathIndex.NormalizePath(path);
        var file = Path.GetFileName(p);
        var lower = p.ToLowerInvariant();
        var fileLower = file.ToLowerInvariant();

        foreach (var rule in conventions.PathTagRules)
        {
            if (!PathConventionMatcher.MatchesGlob(lower, rule.Pattern))
            {
                continue;
            }

            if (Enum.TryParse<SignificanceTag>(rule.Tag, ignoreCase: true, out var tag))
            {
                rulesFired?.Add($"pathTagRule: {rule.Pattern} → {tag}");
                yield return tag;
            }
        }

        foreach (var index in conventions.ProgramIndexPaths)
        {
            if (!lower.Equals(
                    PathConventionMatcher.Normalize(index.Path).ToLowerInvariant(),
                    StringComparison.Ordinal))
            {
                continue;
            }

            if (Enum.TryParse<SignificanceTag>(index.Tag, ignoreCase: true, out var tag))
            {
                rulesFired?.Add($"programIndexPath: {index.Path} → {tag}");
                yield return tag;
            }
        }

        if (IsAgentControl(p, fileLower, lower))
        {
            rulesFired?.Add("path: AgentControl");
            yield return SignificanceTag.AgentControl;
        }

        if (IsBranchingControl(p, fileLower, lower))
        {
            rulesFired?.Add("path: BranchingControl");
            yield return SignificanceTag.BranchingControl;
        }

        if (fileLower is "contributing.md" or "contribute.md")
        {
            rulesFired?.Add("path: Contributing");
            yield return SignificanceTag.Contributing;
        }

        if (fileLower is "security.md" || lower.Contains("/security/", StringComparison.Ordinal))
        {
            rulesFired?.Add("path: Security");
            yield return SignificanceTag.Security;
        }

        if (fileLower is "changelog.md" or "history.md" or "changes.md")
        {
            rulesFired?.Add("path: Changelog");
            yield return SignificanceTag.Changelog;
        }

        if (fileLower is "readme.md" && IsRootLevelPath(lower))
        {
            rulesFired?.Add("path: Readme");
            yield return SignificanceTag.Readme;
        }

        if (!PathConventionMatcher.IsUnderAnyRoot(lower, conventions.ArchiveRoots))
        {
            if (IsShallowProgramDoc(lower, fileLower, conventions, conventions.RoadmapFileNames))
            {
                rulesFired?.Add("shallowProgramDoc: Roadmap");
                yield return SignificanceTag.Roadmap;
            }

            if (IsShallowProgramDoc(lower, fileLower, conventions, conventions.ExecutionPlanFileNames))
            {
                rulesFired?.Add("shallowProgramDoc: ExecutionPlan");
                yield return SignificanceTag.ExecutionPlan;
            }

            if (IsShallowProgramDoc(lower, fileLower, conventions, conventions.StrategyFileNames))
            {
                rulesFired?.Add("shallowProgramDoc: Strategy");
                yield return SignificanceTag.Strategy;
            }

            if (IsArchitectureFile(fileLower))
            {
                rulesFired?.Add("path: Architecture");
                yield return SignificanceTag.Architecture;
            }

            if (IsProductFile(fileLower))
            {
                rulesFired?.Add("path: Product");
                yield return SignificanceTag.Product;
            }
        }

        if (lower.Contains("/adr/", StringComparison.Ordinal) ||
            fileLower.StartsWith("adr-", StringComparison.Ordinal) ||
            RegexFileMatch(fileLower, @"^\d{4}-.*\.md$") && lower.Contains("/decisions/", StringComparison.Ordinal))
        {
            rulesFired?.Add("path: Adr");
            yield return SignificanceTag.Adr;
        }

        if (IsRunbookPath(fileLower, lower))
        {
            rulesFired?.Add("path: Runbook");
            yield return SignificanceTag.Runbook;
        }

        if (IsCommitNotePath(lower))
        {
            rulesFired?.Add("path: CommitNote");
            yield return SignificanceTag.CommitNote;
        }

        if (fileLower.Contains("spec", StringComparison.Ordinal) ||
            lower.Contains("/specs/", StringComparison.Ordinal))
        {
            rulesFired?.Add("path: Spec");
            yield return SignificanceTag.Spec;
        }
    }

    /// <summary>
    /// Default convention <c>docs/commit-notes/&lt;sha&gt;.md</c> (and any <c>…/commit-notes/**</c>) — third-party first.
    /// </summary>
    private static bool IsCommitNotePath(string lower) =>
        lower.Contains("/commit-notes/", StringComparison.Ordinal) ||
        lower.StartsWith("commit-notes/", StringComparison.Ordinal);

    /// <summary>
    /// Orientation / operate docs in the wild (third-party first): runbooks, handovers, onboarding, getting-started.
    /// </summary>
    private static bool IsRunbookPath(string fileLower, string lower)
    {
        if (fileLower.Contains("runbook", StringComparison.Ordinal) ||
            lower.Contains("/runbooks/", StringComparison.Ordinal))
        {
            return true;
        }

        if (fileLower.Contains("handover", StringComparison.Ordinal) ||
            fileLower.Contains("hand-over", StringComparison.Ordinal) ||
            fileLower.Contains("hand_over", StringComparison.Ordinal))
        {
            return true;
        }

        if (fileLower.Contains("onboarding", StringComparison.Ordinal) ||
            lower.Contains("/onboarding/", StringComparison.Ordinal))
        {
            return true;
        }

        if (fileLower.Contains("getting-started", StringComparison.Ordinal) ||
            fileLower.Contains("getting_started", StringComparison.Ordinal) ||
            fileLower is "gettingstarted.md")
        {
            return true;
        }

        // Common OSS / internal “how we develop here” runbooks (exact filenames — avoid /development/ trees).
        if (fileLower is "developing.md" or "development.md")
        {
            return true;
        }

        return false;
    }

    private static IEnumerable<SignificanceTag> FromFileNameHints(
        MarkdownFrontMatter fm,
        List<string>? rulesFired)
    {
        foreach (var t in fm.Tags)
        {
            if (Enum.TryParse<SignificanceTag>(t, ignoreCase: true, out var tag))
            {
                rulesFired?.Add($"front-matter tags: {tag}");
                yield return tag;
            }
        }
    }

    private static bool IsAgentControl(string path, string fileLower, string lower)
    {
        if (fileLower is "agents.md" or "claude.md" or "gemini.md" or "conventions.md" or
            ".cursorrules" or ".windsurfrules" or "skill.md")
        {
            return true;
        }

        if (lower.Contains("/.cursor/rules/", StringComparison.Ordinal) ||
            lower.StartsWith(".cursor/rules/", StringComparison.Ordinal) ||
            lower.Contains("/.cursor/skills/", StringComparison.Ordinal) ||
            lower.StartsWith(".cursor/skills/", StringComparison.Ordinal))
        {
            return true;
        }

        if (lower.Contains("/.vscode/rules/", StringComparison.Ordinal) ||
            lower.StartsWith(".vscode/rules/", StringComparison.Ordinal))
        {
            return true;
        }

        if (lower.Contains("/.github/copilot-instructions.md", StringComparison.Ordinal) ||
            fileLower == "copilot-instructions.md")
        {
            return true;
        }

        if (lower.Contains("/.github/instructions/", StringComparison.Ordinal))
        {
            return true;
        }

        if (lower.Contains("/.continue/rules/", StringComparison.Ordinal) ||
            lower.Contains("/.clinerules/", StringComparison.Ordinal) ||
            lower.Contains("/.claude/", StringComparison.Ordinal))
        {
            return true;
        }

        return path.EndsWith(".mdc", StringComparison.OrdinalIgnoreCase) &&
               lower.Contains("/.cursor/", StringComparison.Ordinal);
    }

    private static bool IsBranchingControl(string path, string fileLower, string lower)
    {
        if (fileLower is "branching.md" or "branches.md")
        {
            return true;
        }

        if (!lower.Contains("/.github/", StringComparison.Ordinal))
        {
            return false;
        }

        return fileLower.Contains("branch", StringComparison.Ordinal) ||
               fileLower.Contains("merge", StringComparison.Ordinal) ||
               fileLower.Contains("pull_request", StringComparison.Ordinal) ||
               fileLower.Contains("pull-request", StringComparison.Ordinal) ||
               fileLower is "codeowners";
    }

    private static bool RegexFileMatch(string fileLower, string pattern) =>
        System.Text.RegularExpressions.Regex.IsMatch(fileLower, pattern);

    private static bool IsRootLevelPath(string lower) => !lower.Contains('/');

    private static bool IsArchitectureFile(string fileLower) =>
        fileLower is "architecture.md" or "architecture-overview.md" or "system-architecture.md";

    private static bool IsProductFile(string fileLower) =>
        fileLower is "product.md" or "product-overview.md";

    private static bool IsShallowProgramDoc(
        string lower,
        string fileLower,
        MarkdownConventionsOptions conventions,
        IReadOnlyList<string> fileNames)
    {
        if (!PathConventionMatcher.FileNameEqualsAny(fileLower, fileNames) ||
            PathConventionMatcher.IsUnderAnyRoot(lower, conventions.ArchiveRoots))
        {
            return false;
        }

        var segments = lower.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length switch
        {
            1 => true,
            2 => PathConventionMatcher.SegmentEqualsAny(segments[0], conventions.DocsHostFolders),
            3 => PathConventionMatcher.SegmentEqualsAny(segments[0], conventions.DocsHostFolders) &&
                 PathConventionMatcher.SegmentEqualsAny(segments[1], conventions.PlanningHostFolders),
            _ => false,
        };
    }
}
