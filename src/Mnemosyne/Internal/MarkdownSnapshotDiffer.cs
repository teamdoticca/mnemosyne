namespace Mnemosyne.Internal;

internal sealed class MarkdownSnapshotDiffer : IMarkdownSnapshotDiffer
{
    public MarkdownSnapshotDiff Diff(
        IReadOnlyList<MarkdownDocument> before,
        IReadOnlyList<MarkdownDocument> after,
        MarkdownPathIndex? afterIndex = null,
        MarkdownPathIndex? beforeIndex = null)
    {
        var changes = new List<SnapshotChange>();
        var beforeByPath = IndexByPath(before);
        var afterByPath = IndexByPath(after);
        var pendingAdded = new List<string>();
        var pendingRemoved = new List<string>();

        foreach (var path in afterByPath.Keys.Except(beforeByPath.Keys, StringComparer.OrdinalIgnoreCase).ToList())
        {
            pendingAdded.Add(path);
        }

        foreach (var path in beforeByPath.Keys.Except(afterByPath.Keys, StringComparer.OrdinalIgnoreCase).ToList())
        {
            pendingRemoved.Add(path);
        }

        PairDocumentArchiveMoves(pendingRemoved, pendingAdded, beforeByPath, afterByPath, changes);

        foreach (var path in pendingAdded)
        {
            changes.Add(new SnapshotChange
            {
                Kind = SnapshotChangeKind.DocumentAdded,
                Path = path,
                Title = afterByPath[path].Title,
            });
        }

        foreach (var path in pendingRemoved)
        {
            changes.Add(new SnapshotChange
            {
                Kind = SnapshotChangeKind.DocumentRemoved,
                Path = path,
                Title = beforeByPath[path].Title,
            });
        }

        // HeadingMoved: same slug appears on different paths across the snapshot pair.
        var beforeHeadings = FlattenHeadings(beforeByPath);
        var afterHeadings = FlattenHeadings(afterByPath);
        var movedSlugs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (slug, beforeHits) in beforeHeadings)
        {
            if (!afterHeadings.TryGetValue(slug, out var afterHits))
            {
                continue;
            }

            var beforePaths = beforeHits.Select(h => h.Path).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var afterPaths = afterHits.Select(h => h.Path).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var removedPaths = beforePaths.Except(afterPaths, StringComparer.OrdinalIgnoreCase).ToList();
            var addedPaths = afterPaths.Except(beforePaths, StringComparer.OrdinalIgnoreCase).ToList();

            // Pair removals with additions as Moved when slug identity matches.
            var pairs = Math.Min(removedPaths.Count, addedPaths.Count);
            for (var i = 0; i < pairs; i++)
            {
                var from = removedPaths[i];
                var to = addedPaths[i];
                var sample = afterHits.First(h =>
                    string.Equals(h.Path, to, StringComparison.OrdinalIgnoreCase));
                changes.Add(new SnapshotChange
                {
                    Kind = SnapshotChangeKind.HeadingMoved,
                    FromPath = from,
                    ToPath = to,
                    HeadingSlug = slug,
                    HeadingText = sample.Heading.Text,
                    Level = sample.Heading.Level,
                });
                movedSlugs.Add($"{slug}|{from}");
                movedSlugs.Add($"{slug}|{to}");
            }
        }

        foreach (var path in beforeByPath.Keys.Intersect(afterByPath.Keys, StringComparer.OrdinalIgnoreCase))
        {
            var b = beforeByPath[path];
            var a = afterByPath[path];

            if (!string.Equals(b.Title, a.Title, StringComparison.Ordinal))
            {
                changes.Add(new SnapshotChange
                {
                    Kind = SnapshotChangeKind.TitleChanged,
                    Path = path,
                    Title = a.Title,
                    PreviousTitle = b.Title,
                });
            }

            DiffHeadingsOnPath(path, b, a, changes, movedSlugs);
            DiffLinksOnPath(path, b, a, changes, beforeIndex, afterIndex);
            DiffSemanticsOnPath(path, b, a, changes);
        }

        return new MarkdownSnapshotDiff { Changes = changes };
    }

    private static void DiffSemanticsOnPath(
        string path,
        MarkdownDocument before,
        MarkdownDocument after,
        List<SnapshotChange> changes)
    {
        var bSem = before.Semantics;
        var aSem = after.Semantics;

        var beforeKind = EnumName(bSem.DocKind);
        var afterKind = EnumName(aSem.DocKind);
        if (!string.Equals(beforeKind, afterKind, StringComparison.Ordinal))
        {
            changes.Add(new SnapshotChange
            {
                Kind = SnapshotChangeKind.DocKindChanged,
                Path = path,
                PreviousSemanticValue = beforeKind,
                SemanticValue = afterKind,
            });
        }

        var beforeLife = EnumName(bSem.Lifecycle);
        var afterLife = EnumName(aSem.Lifecycle);
        if (!string.Equals(beforeLife, afterLife, StringComparison.Ordinal))
        {
            changes.Add(new SnapshotChange
            {
                Kind = SnapshotChangeKind.LifecycleChanged,
                Path = path,
                PreviousSemanticValue = beforeLife,
                SemanticValue = afterLife,
            });
        }

        var beforeRef = NormalizeOptional(bSem.PlanningRef);
        var afterRef = NormalizeOptional(aSem.PlanningRef);
        if (!string.Equals(beforeRef, afterRef, StringComparison.OrdinalIgnoreCase))
        {
            changes.Add(new SnapshotChange
            {
                Kind = SnapshotChangeKind.PlanningRefChanged,
                Path = path,
                PreviousSemanticValue = beforeRef,
                SemanticValue = afterRef,
            });
        }

        DiffKeySections(path, bSem.KeySections, aSem.KeySections, changes);
        DiffTags(path, before.Tags, after.Tags, changes);
    }

    private static void DiffKeySections(
        string path,
        IReadOnlyList<DocumentKeySection> before,
        IReadOnlyList<DocumentKeySection> after,
        List<SnapshotChange> changes)
    {
        var beforeByName = before.ToDictionary(
            s => s.Name,
            StringComparer.OrdinalIgnoreCase);
        var afterByName = after.ToDictionary(
            s => s.Name,
            StringComparer.OrdinalIgnoreCase);

        foreach (var name in afterByName.Keys.Except(beforeByName.Keys, StringComparer.OrdinalIgnoreCase))
        {
            changes.Add(new SnapshotChange
            {
                Kind = SnapshotChangeKind.KeySectionAdded,
                Path = path,
                SectionName = name,
                SemanticValue = afterByName[name].StartLine.ToString(),
            });
        }

        foreach (var name in beforeByName.Keys.Except(afterByName.Keys, StringComparer.OrdinalIgnoreCase))
        {
            changes.Add(new SnapshotChange
            {
                Kind = SnapshotChangeKind.KeySectionRemoved,
                Path = path,
                SectionName = name,
                PreviousSemanticValue = beforeByName[name].StartLine.ToString(),
            });
        }

        foreach (var name in beforeByName.Keys.Intersect(afterByName.Keys, StringComparer.OrdinalIgnoreCase))
        {
            var b = beforeByName[name];
            var a = afterByName[name];
            if (b.StartLine != a.StartLine || b.EndLine != a.EndLine)
            {
                changes.Add(new SnapshotChange
                {
                    Kind = SnapshotChangeKind.KeySectionChanged,
                    Path = path,
                    SectionName = name,
                    PreviousSemanticValue = $"{b.StartLine}-{b.EndLine}",
                    SemanticValue = $"{a.StartLine}-{a.EndLine}",
                });
            }
        }
    }

    private static void DiffTags(
        string path,
        IReadOnlyList<SignificanceTag> before,
        IReadOnlyList<SignificanceTag> after,
        List<SnapshotChange> changes)
    {
        var beforeTags = FormatTags(before);
        var afterTags = FormatTags(after);
        if (string.Equals(beforeTags, afterTags, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        changes.Add(new SnapshotChange
        {
            Kind = SnapshotChangeKind.TagsChanged,
            Path = path,
            PreviousSemanticValue = beforeTags,
            SemanticValue = afterTags,
        });
    }

    private static string FormatTags(IReadOnlyList<SignificanceTag> tags) =>
        tags.Count == 0
            ? ""
            : string.Join(',', tags.Select(t => t.ToString()).Order(StringComparer.OrdinalIgnoreCase));

    private static string? EnumName<T>(T value) where T : struct, Enum =>
        Convert.ToInt32(value) == 0 ? null : value.ToString();

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void DiffHeadingsOnPath(
        string path,
        MarkdownDocument before,
        MarkdownDocument after,
        List<SnapshotChange> changes,
        HashSet<string> movedSlugs)
    {
        var beforeBySlug = before.Headings
            .GroupBy(h => h.Slug, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        var afterBySlug = after.Headings
            .GroupBy(h => h.Slug, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        foreach (var slug in afterBySlug.Keys.Except(beforeBySlug.Keys, StringComparer.OrdinalIgnoreCase))
        {
            if (movedSlugs.Contains($"{slug}|{path}"))
            {
                continue;
            }

            // Rename detection: unique text match with different slug on same path.
            var added = afterBySlug[slug];
            var renamedFrom = before.Headings.FirstOrDefault(h =>
                !afterBySlug.ContainsKey(h.Slug) &&
                string.Equals(h.Text, added.Text, StringComparison.OrdinalIgnoreCase));

            // Prefer rename when same level and similar — use leftover before slug with same level.
            var orphanBefore = before.Headings.FirstOrDefault(h =>
                !afterBySlug.ContainsKey(h.Slug) &&
                h.Level == added.Level &&
                !movedSlugs.Contains($"{h.Slug}|{path}"));

            if (orphanBefore is not null &&
                string.Equals(orphanBefore.Text, added.Text, StringComparison.Ordinal))
            {
                // same text different slug shouldn't happen; treat as level-only below
            }

            if (orphanBefore is not null &&
                !string.Equals(orphanBefore.Slug, added.Slug, StringComparison.OrdinalIgnoreCase) &&
                FuzzyRename(orphanBefore, added))
            {
                changes.Add(new SnapshotChange
                {
                    Kind = SnapshotChangeKind.HeadingRenamed,
                    Path = path,
                    HeadingSlug = added.Slug,
                    HeadingText = added.Text,
                    PreviousHeadingSlug = orphanBefore.Slug,
                    PreviousHeadingText = orphanBefore.Text,
                    Level = added.Level,
                    PreviousLevel = orphanBefore.Level,
                });
                continue;
            }

            _ = renamedFrom;
            changes.Add(new SnapshotChange
            {
                Kind = SnapshotChangeKind.HeadingAdded,
                Path = path,
                HeadingSlug = added.Slug,
                HeadingText = added.Text,
                Level = added.Level,
            });
        }

        foreach (var slug in beforeBySlug.Keys.Except(afterBySlug.Keys, StringComparer.OrdinalIgnoreCase))
        {
            if (movedSlugs.Contains($"{slug}|{path}"))
            {
                continue;
            }

            // Skip if consumed as rename target above (matched as PreviousHeadingSlug).
            if (changes.Any(c =>
                    c.Kind == SnapshotChangeKind.HeadingRenamed &&
                    string.Equals(c.Path, path, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(c.PreviousHeadingSlug, slug, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var removed = beforeBySlug[slug];
            changes.Add(new SnapshotChange
            {
                Kind = SnapshotChangeKind.HeadingRemoved,
                Path = path,
                HeadingSlug = removed.Slug,
                HeadingText = removed.Text,
                Level = removed.Level,
            });
        }

        foreach (var slug in beforeBySlug.Keys.Intersect(afterBySlug.Keys, StringComparer.OrdinalIgnoreCase))
        {
            var b = beforeBySlug[slug];
            var a = afterBySlug[slug];
            if (b.Level != a.Level)
            {
                changes.Add(new SnapshotChange
                {
                    Kind = SnapshotChangeKind.HeadingLevelChanged,
                    Path = path,
                    HeadingSlug = slug,
                    HeadingText = a.Text,
                    Level = a.Level,
                    PreviousLevel = b.Level,
                });
            }
            else if (!string.Equals(b.Text, a.Text, StringComparison.Ordinal))
            {
                changes.Add(new SnapshotChange
                {
                    Kind = SnapshotChangeKind.HeadingRenamed,
                    Path = path,
                    HeadingSlug = a.Slug,
                    HeadingText = a.Text,
                    PreviousHeadingSlug = b.Slug,
                    PreviousHeadingText = b.Text,
                    Level = a.Level,
                });
            }
        }
    }

    private static bool FuzzyRename(MarkdownHeading before, MarkdownHeading after) =>
        before.Level == after.Level &&
        !string.Equals(before.Slug, after.Slug, StringComparison.OrdinalIgnoreCase);

    private static void DiffLinksOnPath(
        string path,
        MarkdownDocument before,
        MarkdownDocument after,
        List<SnapshotChange> changes,
        MarkdownPathIndex? beforeIndex,
        MarkdownPathIndex? afterIndex)
    {
        var beforeRoles = ClassifyLinkRoles(path, before, beforeIndex);
        var afterRoles = ClassifyLinkRoles(path, after, afterIndex);

        foreach (var (target, role) in afterRoles)
        {
            if (!beforeRoles.TryGetValue(target, out var previousRole))
            {
                changes.Add(new SnapshotChange
                {
                    Kind = SnapshotChangeKind.LinkAdded,
                    Path = path,
                    LinkTarget = target,
                    SemanticValue = LinkRoleClassifier.ToKindString(role),
                });
                continue;
            }

            if (previousRole != role)
            {
                changes.Add(new SnapshotChange
                {
                    Kind = SnapshotChangeKind.LinkRoleChanged,
                    Path = path,
                    LinkTarget = target,
                    SemanticValue = LinkRoleClassifier.ToKindString(role),
                    PreviousSemanticValue = LinkRoleClassifier.ToKindString(previousRole),
                });
            }
        }

        foreach (var (target, role) in beforeRoles)
        {
            if (!afterRoles.ContainsKey(target))
            {
                changes.Add(new SnapshotChange
                {
                    Kind = SnapshotChangeKind.LinkRemoved,
                    Path = path,
                    LinkTarget = target,
                    PreviousSemanticValue = LinkRoleClassifier.ToKindString(role),
                });
            }
        }

        if (beforeIndex is not null && afterIndex is not null)
        {
            var resolver = new MarkdownLinkResolver();
            var beforeBroken = resolver.Resolve(before, beforeIndex)
                .Select(d => LinkKey(d.Link) + "|" + d.Kind)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var afterBroken = resolver.Resolve(after, afterIndex)
                .Select(d => LinkKey(d.Link) + "|" + d.Kind)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var b in afterBroken.Except(beforeBroken, StringComparer.OrdinalIgnoreCase))
            {
                changes.Add(new SnapshotChange
                {
                    Kind = SnapshotChangeKind.BrokenLinkIntroduced,
                    Path = path,
                    LinkTarget = b,
                });
            }

            foreach (var b in beforeBroken.Except(afterBroken, StringComparer.OrdinalIgnoreCase))
            {
                changes.Add(new SnapshotChange
                {
                    Kind = SnapshotChangeKind.BrokenLinkCleared,
                    Path = path,
                    LinkTarget = b,
                });
            }
        }
    }

    private static void PairDocumentArchiveMoves(
        List<string> pendingRemoved,
        List<string> pendingAdded,
        Dictionary<string, MarkdownDocument> beforeByPath,
        Dictionary<string, MarkdownDocument> afterByPath,
        List<SnapshotChange> changes)
    {
        for (var i = pendingRemoved.Count - 1; i >= 0; i--)
        {
            var fromPath = pendingRemoved[i];
            if (!TryParseRoadmapEpicTail(fromPath, out var slug, out var tail))
            {
                continue;
            }

            var matchIndex = pendingAdded.FindIndex(toPath =>
                TryParseDoneEpicTail(toPath, out var doneSlug, out var doneTail) &&
                string.Equals(slug, doneSlug, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(tail, doneTail, StringComparison.OrdinalIgnoreCase));

            if (matchIndex < 0)
            {
                continue;
            }

            var toPath = pendingAdded[matchIndex];
            changes.Add(new SnapshotChange
            {
                Kind = SnapshotChangeKind.DocumentMoved,
                FromPath = fromPath,
                ToPath = toPath,
                Path = toPath,
                Title = afterByPath[toPath].Title,
                PreviousTitle = beforeByPath[fromPath].Title,
            });
            pendingRemoved.RemoveAt(i);
            pendingAdded.RemoveAt(matchIndex);
        }
    }

    private static bool TryParseRoadmapEpicTail(string path, out string slug, out string tail)
    {
        slug = "";
        tail = "";
        var normalized = MarkdownPathIndex.NormalizePath(path);
        const string prefix = "docs/roadmap/";
        if (!normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var remainder = normalized[prefix.Length..];
        var slash = remainder.IndexOf('/');
        if (slash <= 0)
        {
            return false;
        }

        slug = remainder[..slash];
        tail = remainder[(slash + 1)..];
        return true;
    }

    private static bool TryParseDoneEpicTail(string path, out string slug, out string tail)
    {
        slug = "";
        tail = "";
        var normalized = MarkdownPathIndex.NormalizePath(path);
        const string prefix = "docs/done/";
        if (!normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var remainder = normalized[prefix.Length..];
        var slash = remainder.IndexOf('/');
        if (slash <= 0)
        {
            return false;
        }

        slug = remainder[..slash];
        tail = remainder[(slash + 1)..];
        return true;
    }

    private static string LinkKey(MarkdownLink link) =>
        string.IsNullOrEmpty(link.Anchor) ? link.Target : $"{link.Target}#{link.Anchor}";

    private static Dictionary<string, LinkRole> ClassifyLinkRoles(
        string path,
        MarkdownDocument document,
        MarkdownPathIndex? index)
    {
        var map = new Dictionary<string, LinkRole>(StringComparer.OrdinalIgnoreCase);
        foreach (var link in document.Links.Where(l => !l.IsExternal))
        {
            var key = LinkKey(link);
            var resolved = index is null ? null : ResolveLinkTargetPath(path, link, index);
            var role = LinkRoleClassifier.Classify(path, link, resolved);
            map[key] = role;
        }

        return map;
    }

    private static string? ResolveLinkTargetPath(
        string sourcePath,
        MarkdownLink link,
        MarkdownPathIndex index)
    {
        var targetPath = ResolveRelativeLinkPath(sourcePath, link.Target);
        return targetPath is null ? null : index.ResolveExistingPath(targetPath);
    }

    private static string? ResolveRelativeLinkPath(string fromRelative, string target)
    {
        var t = MarkdownPathIndex.NormalizePath(target);
        if (string.IsNullOrEmpty(t) || t.StartsWith('#'))
        {
            return null;
        }

        if (t.StartsWith("./", StringComparison.Ordinal))
        {
            t = t[2..];
        }

        var hash = t.IndexOf('#');
        if (hash >= 0)
        {
            t = t[..hash];
        }

        if (string.IsNullOrEmpty(t))
        {
            return null;
        }

        var baseDir = GetDirectory(fromRelative);
        var combined = string.IsNullOrEmpty(baseDir) ? t : $"{baseDir}/{t}";
        var parts = new List<string>();
        foreach (var segment in combined.Split('/', StringSplitOptions.RemoveEmptyEntries))
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

        return parts.Count == 0 ? null : string.Join('/', parts);
    }

    private static string GetDirectory(string relative) =>
        relative.Replace('\\', '/').Contains('/')
            ? relative.Replace('\\', '/')[..relative.Replace('\\', '/').LastIndexOf('/')]
            : "";

    private static Dictionary<string, MarkdownDocument> IndexByPath(IReadOnlyList<MarkdownDocument> docs)
    {
        var map = new Dictionary<string, MarkdownDocument>(StringComparer.OrdinalIgnoreCase);
        foreach (var doc in docs)
        {
            var path = doc.Path ?? "";
            map[path] = doc;
        }

        return map;
    }

    private static Dictionary<string, List<(string Path, MarkdownHeading Heading)>> FlattenHeadings(
        Dictionary<string, MarkdownDocument> byPath)
    {
        var map = new Dictionary<string, List<(string, MarkdownHeading)>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (path, doc) in byPath)
        {
            foreach (var h in doc.Headings)
            {
                if (!map.TryGetValue(h.Slug, out var list))
                {
                    list = [];
                    map[h.Slug] = list;
                }

                list.Add((path, h));
            }
        }

        return map;
    }
}
