namespace Mnemosyne.Internal;

internal static class PathConventionMatcher
{
    public static string Normalize(string path) =>
        MarkdownPathIndex.NormalizePath(path).Trim().TrimStart('/');

    public static bool MatchesGlob(string path, string pattern)
    {
        var p = Normalize(path).ToLowerInvariant();
        var g = Normalize(pattern).ToLowerInvariant();
        if (g.Length == 0)
        {
            return false;
        }

        if (g.EndsWith("/**", StringComparison.Ordinal))
        {
            var prefix = g[..^3];
            return p == prefix || p.StartsWith(prefix + "/", StringComparison.Ordinal);
        }

        if (g.EndsWith("/*", StringComparison.Ordinal))
        {
            var prefix = g[..^2];
            if (!p.StartsWith(prefix + "/", StringComparison.Ordinal))
            {
                return false;
            }

            var rest = p[(prefix.Length + 1)..];
            return rest.Length > 0 && !rest.Contains('/');
        }

        if (g.Contains('*'))
        {
            var regex = "^" + System.Text.RegularExpressions.Regex.Escape(g)
                .Replace("\\*\\*", ".*", StringComparison.Ordinal)
                .Replace("\\*", "[^/]*", StringComparison.Ordinal) + "$";
            return System.Text.RegularExpressions.Regex.IsMatch(p, regex);
        }

        return p.Equals(g, StringComparison.Ordinal);
    }

    public static bool IsUnderAnyRoot(string lowerPath, IReadOnlyList<string> roots)
    {
        foreach (var root in roots)
        {
            var r = Normalize(root).ToLowerInvariant().TrimEnd('/');
            if (r.Length == 0)
            {
                continue;
            }

            if (lowerPath == r || lowerPath.StartsWith(r + "/", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    public static bool SegmentEqualsAny(string segment, IReadOnlyList<string> values)
    {
        foreach (var v in values)
        {
            if (segment.Equals(v, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public static bool FileNameEqualsAny(string fileLower, IReadOnlyList<string> names)
    {
        foreach (var n in names)
        {
            if (fileLower.Equals(n, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
