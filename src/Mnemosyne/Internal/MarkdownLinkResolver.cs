namespace Mnemosyne.Internal;

internal sealed class MarkdownLinkResolver : IMarkdownLinkResolver
{
    public IReadOnlyList<MarkdownLinkDiagnostic> Resolve(
        MarkdownDocument document,
        MarkdownPathIndex index)
    {
        var diagnostics = new List<MarkdownLinkDiagnostic>();
        var baseDir = document.Path is null
            ? ""
            : MarkdownPathIndex.NormalizePath(Path.GetDirectoryName(document.Path) ?? "");

        foreach (var link in document.Links)
        {
            if (link.IsExternal)
            {
                continue;
            }

            var target = link.Target.Trim();
            if (string.IsNullOrEmpty(target) || target.StartsWith('#'))
            {
                // Same-doc anchor only.
                if (!string.IsNullOrEmpty(link.Anchor ?? target.TrimStart('#')))
                {
                    var anchor = (link.Anchor ?? target.TrimStart('#')).TrimStart('#');
                    var docPath = document.Path ?? "";
                    var known = document.Headings.Any(h =>
                        string.Equals(h.Slug, anchor, StringComparison.OrdinalIgnoreCase));
                    if (!known &&
                        (string.IsNullOrEmpty(docPath) || !index.ContainsAnchor(docPath, anchor)))
                    {
                        diagnostics.Add(new MarkdownLinkDiagnostic
                        {
                            Link = link,
                            Kind = LinkDiagnosticKind.MissingAnchor,
                        });
                    }
                }

                continue;
            }

            var resolvedPath = ResolveRelative(baseDir, target);
            var pathKey = index.ResolveExistingPath(resolvedPath);
            if (pathKey is null)
            {
                diagnostics.Add(new MarkdownLinkDiagnostic
                {
                    Link = link,
                    Kind = LinkDiagnosticKind.MissingTarget,
                });
                continue;
            }

            if (!string.IsNullOrEmpty(link.Anchor) &&
                !index.ContainsAnchor(pathKey, link.Anchor) &&
                !HeadingExistsOnDocument(document, pathKey, link.Anchor))
            {
                diagnostics.Add(new MarkdownLinkDiagnostic
                {
                    Link = link,
                    Kind = LinkDiagnosticKind.MissingAnchor,
                });
            }
        }

        return diagnostics;
    }

    private static bool HeadingExistsOnDocument(
        MarkdownDocument document,
        string pathKey,
        string anchor) =>
        string.Equals(document.Path, pathKey, StringComparison.OrdinalIgnoreCase) &&
        document.Headings.Any(h =>
            string.Equals(h.Slug, anchor.TrimStart('#'), StringComparison.OrdinalIgnoreCase));

    private static string ResolveRelative(string baseDir, string target)
    {
        var t = MarkdownPathIndex.NormalizePath(target);
        if (t.StartsWith("./", StringComparison.Ordinal))
        {
            t = t[2..];
        }

        if (string.IsNullOrEmpty(baseDir) || !t.StartsWith("../", StringComparison.Ordinal) && !t.Contains('/'))
        {
            // relative file in same dir
            if (!t.Contains('/') && !string.IsNullOrEmpty(baseDir))
            {
                return MarkdownPathIndex.NormalizePath($"{baseDir}/{t}");
            }
        }

        if (t.StartsWith('/'))
        {
            return MarkdownPathIndex.NormalizePath(t);
        }

        var combined = string.IsNullOrEmpty(baseDir) ? t : $"{baseDir}/{t}";
        return NormalizeDotSegments(combined);
    }

    private static string NormalizeDotSegments(string path)
    {
        var parts = new List<string>();
        foreach (var segment in MarkdownPathIndex.NormalizePath(path).Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            if (segment == ".")
            {
                continue;
            }

            if (segment == "..")
            {
                if (parts.Count > 0)
                {
                    parts.RemoveAt(parts.Count - 1);
                }

                continue;
            }

            parts.Add(segment);
        }

        return string.Join('/', parts);
    }
}
