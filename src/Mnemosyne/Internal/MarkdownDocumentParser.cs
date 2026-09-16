using System.Text.RegularExpressions;

namespace Mnemosyne.Internal;

internal sealed class MarkdownDocumentParser : IMarkdownDocumentParser
{
    private static readonly Regex AtxHeading = new(
        @"^(#{1,6})\s+(.+?)\s*#*\s*$",
        RegexOptions.Compiled);

    private static readonly Regex MdLink = new(
        @"\[([^\]]*)\]\(([^)\s]+)(?:\s+""[^""]*"")?\)",
        RegexOptions.Compiled);

    private static readonly Regex WikiLink = new(
        @"\[\[([^\]|#]+)(?:\|[^\]]+)?(?:#([^\]]+))?\]\]",
        RegexOptions.Compiled);

    public MarkdownDocument Parse(string markdown, MarkdownParseOptions? options = null)
    {
        options ??= new MarkdownParseOptions();
        var (frontMatter, body, bodyStartLine) = FrontMatterParser.TryParse(markdown ?? "");
        var lines = body.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

        var headings = new List<MarkdownHeading>();
        var links = new List<MarkdownLink>();
        string? firstParagraph = null;
        var inCodeFence = false;

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var lineNo = bodyStartLine + i;

            if (line.StartsWith("```", StringComparison.Ordinal) ||
                line.StartsWith("~~~", StringComparison.Ordinal))
            {
                inCodeFence = !inCodeFence;
                continue;
            }

            if (inCodeFence)
            {
                continue;
            }

            var headingMatch = AtxHeading.Match(line);
            if (headingMatch.Success)
            {
                var level = headingMatch.Groups[1].Value.Length;
                if (level is >= 1 and <= 3)
                {
                    var text = headingMatch.Groups[2].Value.Trim();
                    headings.Add(new MarkdownHeading
                    {
                        Level = level,
                        Text = text,
                        Slug = HeadingSlug.FromHeadingText(text),
                        StartLine = lineNo,
                        EndLine = lineNo,
                    });
                }

                continue;
            }

            CollectLinks(line, lineNo, links);

            if (firstParagraph is null &&
                !string.IsNullOrWhiteSpace(line) &&
                !line.StartsWith('#') &&
                !line.StartsWith('>') &&
                !line.StartsWith('|') &&
                !line.StartsWith("- ", StringComparison.Ordinal) &&
                !line.StartsWith("* ", StringComparison.Ordinal) &&
                !IsBoldLabelPreambleLine(line))
            {
                firstParagraph = line.Trim();
            }
        }

        // Extend heading end lines to just before next heading.
        for (var h = 0; h < headings.Count; h++)
        {
            var end = h + 1 < headings.Count
                ? headings[h + 1].StartLine - 1
                : bodyStartLine + lines.Length - 1;
            if (end < headings[h].StartLine)
            {
                end = headings[h].StartLine;
            }

            headings[h] = new MarkdownHeading
            {
                Level = headings[h].Level,
                Text = headings[h].Text,
                Slug = headings[h].Slug,
                StartLine = headings[h].StartLine,
                EndLine = end,
            };
        }

        var title = frontMatter.Title
            ?? headings.FirstOrDefault(h => h.Level == 1)?.Text
            ?? headings.FirstOrDefault()?.Text;

        var blurbSource = firstParagraph ?? title ?? "";
        var blurb = Truncate(StripMarkdownInline(blurbSource), options.BlurbMaxLength);

        var path = options.Path is null ? null : MarkdownPathIndex.NormalizePath(options.Path);
        var conventions = options.Conventions ?? MarkdownConventionsOptions.CreateDefault();
        var tags = SignificanceTagger.Classify(path, frontMatter, conventions);
        var semantics = DocumentSemanticsClassifier.Classify(
            path,
            frontMatter,
            headings,
            lines,
            bodyStartLine,
            conventions);

        return new MarkdownDocument
        {
            Path = path,
            Title = title,
            Blurb = blurb,
            FrontMatter = frontMatter,
            Headings = headings,
            Links = links,
            Tags = tags,
            Semantics = semantics,
        };
    }

    public MarkdownClassificationExplanation Explain(string markdown, MarkdownParseOptions? options = null)
    {
        options ??= new MarkdownParseOptions();
        markdown ??= "";
        var (frontMatter, body, bodyStartLine) = FrontMatterParser.TryParse(markdown);
        var lines = body.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var conventions = options.Conventions ?? MarkdownConventionsOptions.CreateDefault();
        var path = options.Path is null ? null : MarkdownPathIndex.NormalizePath(options.Path);
        var rules = new List<string>();

        var doc = Parse(markdown, options);
        _ = SignificanceTagger.Classify(path, frontMatter, conventions, rules);
        _ = DocumentSemanticsClassifier.Classify(
            path,
            frontMatter,
            doc.Headings,
            lines,
            bodyStartLine,
            conventions,
            rules);

        if (frontMatter.Pin)
        {
            rules.Add("front-matter: mnemosyne.pin");
        }

        if (!string.IsNullOrWhiteSpace(frontMatter.GuidanceGroup))
        {
            rules.Add($"front-matter: mnemosyne.guidanceGroup={frontMatter.GuidanceGroup}");
        }

        return new MarkdownClassificationExplanation
        {
            Document = doc,
            RulesFired = rules,
        };
    }

    private static readonly Regex BoldLabelPreamble = new(
        @"^\*\*[^*]+:\*\*\s*",
        RegexOptions.Compiled);

    private static bool IsBoldLabelPreambleLine(string line) =>
        BoldLabelPreamble.IsMatch(line.TrimStart());

    private static void CollectLinks(string line, int lineNo, List<MarkdownLink> links)
    {
        foreach (Match m in MdLink.Matches(line))
        {
            var text = m.Groups[1].Value;
            var targetRaw = m.Groups[2].Value.Trim();
            var (target, anchor, external) = SplitTarget(targetRaw);
            links.Add(new MarkdownLink
            {
                Text = text,
                Target = target,
                Anchor = anchor,
                Line = lineNo,
                IsExternal = external,
            });
        }

        foreach (Match m in WikiLink.Matches(line))
        {
            var target = m.Groups[1].Value.Trim();
            var anchor = m.Groups[2].Success ? m.Groups[2].Value.Trim() : null;
            links.Add(new MarkdownLink
            {
                Text = target,
                Target = target,
                Anchor = string.IsNullOrEmpty(anchor) ? null : anchor,
                Line = lineNo,
                IsExternal = false,
            });
        }
    }

    private static (string Target, string? Anchor, bool External) SplitTarget(string raw)
    {
        if (raw.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            raw.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            raw.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
        {
            return (raw, null, true);
        }

        var hash = raw.IndexOf('#');
        if (hash < 0)
        {
            return (raw, null, false);
        }

        var path = raw[..hash];
        var anchor = raw[(hash + 1)..];
        return (path, string.IsNullOrEmpty(anchor) ? null : anchor, false);
    }

    private static string StripMarkdownInline(string text)
    {
        text = MdLink.Replace(text, "$1");
        text = WikiLink.Replace(text, "$1");
        text = text.Replace("**", "").Replace("__", "").Replace("*", "").Replace("_", "");
        text = text.Replace("`", "");
        return text.Trim();
    }

    private static string Truncate(string text, int max)
    {
        if (max <= 0 || text.Length <= max)
        {
            return text;
        }

        return text[..max].TrimEnd() + "…";
    }
}
