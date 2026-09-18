namespace Mnemosyne.Internal;

internal static class FrontMatterParser
{
    private static readonly HashSet<string> AllowedKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "title", "status", "tags", "owner", "commitSha",
        "mnemosyne.tags", "mnemosyne.clearBuiltIns",
        "mnemosyne.planningRef", "mnemosyne.pin", "mnemosyne.guidanceGroup",
    };

    public static (MarkdownFrontMatter Matter, string Body, int BodyStartLine) TryParse(string markdown)
    {
        var normalized = markdown.Replace("\r\n", "\n").Replace('\r', '\n');
        if (!normalized.StartsWith("---\n", StringComparison.Ordinal))
        {
            return (new MarkdownFrontMatter(), markdown, 1);
        }

        var end = normalized.IndexOf("\n---\n", 3, StringComparison.Ordinal);
        int yamlEndExclusive;
        int bodyStartIndex;
        if (end < 0)
        {
            if (normalized.EndsWith("\n---", StringComparison.Ordinal))
            {
                yamlEndExclusive = normalized.Length - 4;
                bodyStartIndex = normalized.Length;
            }
            else
            {
                return (new MarkdownFrontMatter(), markdown, 1);
            }
        }
        else
        {
            yamlEndExclusive = end;
            bodyStartIndex = end + "\n---\n".Length;
        }

        var yaml = normalized[4..Math.Max(4, yamlEndExclusive)];
        var map = ParseSimpleYaml(yaml);

        var matter = new MarkdownFrontMatter
        {
            Title = GetString(map, "title"),
            Status = GetString(map, "status"),
            Owner = GetString(map, "owner"),
            Tags = GetStringList(map, "tags"),
            CommitSha = GetString(map, "commitSha"),
            MnemosyneTags = ParseSignificanceTags(GetStringList(map, "mnemosyne.tags")),
            ClearBuiltInTags = GetBool(map, "mnemosyne.clearBuiltIns"),
            PlanningRef = GetString(map, "mnemosyne.planningRef"),
            Pin = GetBool(map, "mnemosyne.pin"),
            GuidanceGroup = GetString(map, "mnemosyne.guidanceGroup"),
        };

        var preambleLines = normalized[..bodyStartIndex].Count(c => c == '\n');
        var bodyStartLine = preambleLines + 1;
        var body = bodyStartIndex >= normalized.Length ? "" : normalized[bodyStartIndex..];
        return (matter, body, bodyStartLine);
    }

    private static Dictionary<string, string> ParseSimpleYaml(string yaml)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var rawLine in yaml.Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            var colon = line.IndexOf(':');
            if (colon <= 0)
            {
                continue;
            }

            var key = line[..colon].Trim();
            if (!AllowedKeys.Contains(key))
            {
                continue;
            }

            var value = line[(colon + 1)..].Trim().Trim('"').Trim('\'');
            map[key] = value;
        }

        return map;
    }

    private static string? GetString(Dictionary<string, string> map, string key) =>
        map.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v) ? v : null;

    private static bool GetBool(Dictionary<string, string> map, string key) =>
        map.TryGetValue(key, out var v) &&
        (string.Equals(v, "true", StringComparison.OrdinalIgnoreCase) || v == "1");

    private static IReadOnlyList<string> GetStringList(Dictionary<string, string> map, string key)
    {
        if (!map.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        raw = raw.Trim();
        if (raw.StartsWith('[') && raw.EndsWith(']'))
        {
            raw = raw[1..^1];
        }

        return raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => s.Trim('"').Trim('\''))
            .Where(s => s.Length > 0)
            .ToArray();
    }

    private static IReadOnlyList<SignificanceTag> ParseSignificanceTags(IReadOnlyList<string> names)
    {
        var list = new List<SignificanceTag>();
        foreach (var name in names)
        {
            if (Enum.TryParse<SignificanceTag>(name, ignoreCase: true, out var tag))
            {
                list.Add(tag);
            }
        }

        return list;
    }
}
