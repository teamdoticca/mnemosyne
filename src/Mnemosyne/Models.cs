namespace Mnemosyne;

/// <summary>Options for parsing a single markdown document.</summary>
public sealed class MarkdownParseOptions
{
    /// <summary>Repo-relative path (forward slashes preferred), used for tags and identity.</summary>
    public string? Path { get; init; }

    /// <summary>Max blurb length (default 240).</summary>
    public int BlurbMaxLength { get; init; } = 240;

    /// <summary>Recognition + guidance options (defaults = builtin heuristics).</summary>
    public MarkdownConventionsOptions? Conventions { get; init; }
}

/// <summary>Allowlisted front-matter fields (others ignored).</summary>
public sealed class MarkdownFrontMatter
{
    public string? Title { get; init; }
    public string? Status { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = [];
    public string? Owner { get; init; }

    /// <summary>Optional explicit Mnemosyne significance tags from front-matter (<c>mnemosyne.tags</c>).</summary>
    public IReadOnlyList<SignificanceTag> MnemosyneTags { get; init; } = [];

    /// <summary>When true, built-in path heuristics are skipped and only <see cref="MnemosyneTags"/> apply (plus Other if empty).</summary>
    public bool ClearBuiltInTags { get; init; }

    /// <summary>Optional planning slug override (<c>mnemosyne.planningRef</c>).</summary>
    public string? PlanningRef { get; init; }

    /// <summary>When true, prefer this module as a Repo guidance pin (<c>mnemosyne.pin</c>).</summary>
    public bool Pin { get; init; }

    /// <summary>Optional guidance group: planning | governance (<c>mnemosyne.guidanceGroup</c>).</summary>
    public string? GuidanceGroup { get; init; }

    /// <summary>Target commit SHA for commit notes (<c>commitSha</c>) — tags as <see cref="SignificanceTag.CommitNote"/>.</summary>
    public string? CommitSha { get; init; }
}

public sealed class MarkdownHeading
{
    public required int Level { get; init; }
    public required string Text { get; init; }
    public required string Slug { get; init; }
    public required int StartLine { get; init; }
    public required int EndLine { get; init; }
}

public sealed class MarkdownLink
{
    public required string Text { get; init; }
    public required string Target { get; init; }
    public required int Line { get; init; }
    public bool IsExternal { get; init; }
    public string? Anchor { get; init; }
}

public sealed class MarkdownDocument
{
    public string? Path { get; init; }
    public string? Title { get; init; }
    public string Blurb { get; init; } = "";
    public MarkdownFrontMatter FrontMatter { get; init; } = new();
    public IReadOnlyList<MarkdownHeading> Headings { get; init; } = [];
    public IReadOnlyList<MarkdownLink> Links { get; init; } = [];
    public IReadOnlyList<SignificanceTag> Tags { get; init; } = [];
    public DocumentSemantics Semantics { get; init; } = new();
}

/// <summary>Repo path set for pure link resolution (no I/O).</summary>
public sealed class MarkdownPathIndex
{
    private readonly HashSet<string> _paths;
    private readonly Dictionary<string, HashSet<string>> _anchorsByPath;

    public MarkdownPathIndex(
        IEnumerable<string> paths,
        IReadOnlyDictionary<string, IEnumerable<string>>? anchorsByPath = null)
    {
        _paths = new HashSet<string>(paths.Select(NormalizePath), StringComparer.OrdinalIgnoreCase);
        _anchorsByPath = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        if (anchorsByPath is not null)
        {
            foreach (var (path, anchors) in anchorsByPath)
            {
                _anchorsByPath[NormalizePath(path)] = new HashSet<string>(
                    anchors.Select(a => a.TrimStart('#')),
                    StringComparer.OrdinalIgnoreCase);
            }
        }
    }

    public bool ContainsPath(string path) => _paths.Contains(NormalizePath(path));

    public bool ContainsAnchor(string path, string anchor)
    {
        var key = NormalizePath(path);
        if (!_anchorsByPath.TryGetValue(key, out var set))
        {
            return false;
        }

        return set.Contains(anchor.TrimStart('#'));
    }

    /// <summary>
    /// First index hit for a resolved link path, including <c>.md</c> and folder README/index conventions.
    /// </summary>
    public string? ResolveExistingPath(string resolvedPath)
    {
        foreach (var candidate in ExpandLinkPathCandidates(resolvedPath))
        {
            if (ContainsPath(candidate))
            {
                return NormalizePath(candidate);
            }
        }

        return null;
    }

    public static string NormalizePath(string path) =>
        path.Replace('\\', '/').Trim().TrimStart('/').TrimEnd('/');

    /// <summary>
    /// Paths to try when a markdown link omits the extension or points at a folder
    /// (README / index convention — e.g. <c>docs/foo/</c> → <c>docs/foo/README.md</c>).
    /// </summary>
    public static IReadOnlyList<string> ExpandLinkPathCandidates(string path)
    {
        var normalized = NormalizePath(path);
        if (string.IsNullOrEmpty(normalized))
        {
            return [];
        }

        var candidates = new List<string> { normalized };
        if (HasMarkdownExtension(normalized))
        {
            return candidates;
        }

        candidates.Add(normalized + ".md");
        candidates.Add(normalized + ".mdc");
        candidates.Add(normalized + ".markdown");
        candidates.Add(normalized + "/README.md");
        candidates.Add(normalized + "/readme.md");
        candidates.Add(normalized + "/index.md");
        candidates.Add(normalized + "/Index.md");
        return candidates;
    }

    private static bool HasMarkdownExtension(string path) =>
        path.EndsWith(".md", StringComparison.OrdinalIgnoreCase) ||
        path.EndsWith(".mdc", StringComparison.OrdinalIgnoreCase) ||
        path.EndsWith(".markdown", StringComparison.OrdinalIgnoreCase);
}

public sealed class MarkdownLinkDiagnostic
{
    public required MarkdownLink Link { get; init; }
    public required LinkDiagnosticKind Kind { get; init; }
}

public sealed class SnapshotChange
{
    public required SnapshotChangeKind Kind { get; init; }
    public string? Path { get; init; }
    public string? FromPath { get; init; }
    public string? ToPath { get; init; }
    public string? HeadingText { get; init; }
    public string? HeadingSlug { get; init; }
    public string? PreviousHeadingText { get; init; }
    public string? PreviousHeadingSlug { get; init; }
    public int? Level { get; init; }
    public int? PreviousLevel { get; init; }
    public string? LinkTarget { get; init; }
    public string? Title { get; init; }
    public string? PreviousTitle { get; init; }
    public string? SemanticValue { get; init; }
    public string? PreviousSemanticValue { get; init; }
    public string? SectionName { get; init; }
}

public sealed class MarkdownSnapshotDiff
{
    public IReadOnlyList<SnapshotChange> Changes { get; init; } = [];
}
