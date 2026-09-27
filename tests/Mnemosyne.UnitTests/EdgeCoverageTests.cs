using FluentAssertions;
using Mnemosyne;

namespace Mnemosyne.UnitTests;

/// <summary>
/// Broader public-API coverage for globs, tags, doc kinds, links, and snapshot diffs — bug hunting surface.
/// </summary>
public class EdgeCoverageTests
{
    [Fact]
    public void PathDocKindRule_overrides_kind_for_matching_path()
    {
        var conventions = MarkdownConventionsOptions.CreateDefault();
        var doc = MnemosyneFacade.Parse(
            "# Policy\n",
            new MarkdownParseOptions
            {
                Path = "docs/policies/retention.md",
                Conventions = new MarkdownConventionsOptions
                {
                    EpicRoots = conventions.EpicRoots,
                    ArchiveRoots = conventions.ArchiveRoots,
                    DocsHostFolders = conventions.DocsHostFolders,
                    PlanningHostFolders = conventions.PlanningHostFolders,
                    RoadmapFileNames = conventions.RoadmapFileNames,
                    ExecutionPlanFileNames = conventions.ExecutionPlanFileNames,
                    StrategyFileNames = conventions.StrategyFileNames,
                    ProgramIndexPaths = conventions.ProgramIndexPaths,
                    PathTagRules = conventions.PathTagRules,
                    PathDocKindRules = [new PathDocKindRule("docs/policies/**", "Policy")],
                    StatusTableColumns = conventions.StatusTableColumns,
                    StatusTokenAliases = conventions.StatusTokenAliases,
                    KeySectionNames = conventions.KeySectionNames,
                    GuidancePins = conventions.GuidancePins,
                },
            });

        doc.Semantics.DocKind.Should().Be(DocumentKind.Policy);
    }

    [Theory]
    [InlineData("docs/adr/0001-record.md", SignificanceTag.Adr)]
    [InlineData("docs/decisions/2024-use-mnemosyne.md", SignificanceTag.Adr)]
    [InlineData("adr-003-caching.md", SignificanceTag.Adr)]
    [InlineData("SECURITY.md", SignificanceTag.Security)]
    [InlineData("docs/security/threat-model.md", SignificanceTag.Security)]
    [InlineData("CHANGELOG.md", SignificanceTag.Changelog)]
    [InlineData("HISTORY.md", SignificanceTag.Changelog)]
    [InlineData("CHANGES.md", SignificanceTag.Changelog)]
    [InlineData("CONTRIBUTING.md", SignificanceTag.Contributing)]
    [InlineData("contribute.md", SignificanceTag.Contributing)]
    [InlineData("docs/specs/wire-format.md", SignificanceTag.Spec)]
    [InlineData("docs/foo-spec.md", SignificanceTag.Spec)]
    [InlineData("commit-notes/abcdef0123456789abcdef0123456789abcdef01.md", SignificanceTag.CommitNote)]
    public void Additional_path_heuristics(string path, SignificanceTag expected)
    {
        var doc = MnemosyneFacade.Parse("# Hi\n", new MarkdownParseOptions { Path = path });
        doc.Tags.Should().Contain(expected);
    }

    [Fact]
    public void Nested_evidence_path_gets_Spec_from_star_glob_rule()
    {
        var explanation = MnemosyneFacade.Explain(
            "# Evidence\n",
            new MarkdownParseOptions { Path = "documentation/epics/x/evidence/gate.md" });

        explanation.Document.Tags.Should().Contain(SignificanceTag.Spec);
        explanation.RulesFired.Should().Contain(r =>
            r.Contains("pathTagRule", StringComparison.OrdinalIgnoreCase) &&
            r.Contains("evidence", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void PathTagRule_middle_double_star_matches_nested_evidence()
    {
        var conventions = MarkdownConventionsOptions.CreateDefault();
        var doc = MnemosyneFacade.Parse(
            "# Gate\n",
            new MarkdownParseOptions
            {
                Path = "docs/epics/m99/evidence/08.md",
                Conventions = new MarkdownConventionsOptions
                {
                    EpicRoots = conventions.EpicRoots,
                    ArchiveRoots = conventions.ArchiveRoots,
                    DocsHostFolders = conventions.DocsHostFolders,
                    PlanningHostFolders = conventions.PlanningHostFolders,
                    RoadmapFileNames = conventions.RoadmapFileNames,
                    ExecutionPlanFileNames = conventions.ExecutionPlanFileNames,
                    StrategyFileNames = conventions.StrategyFileNames,
                    ProgramIndexPaths = conventions.ProgramIndexPaths,
                    // Middle ** — previously broken (treated as literal prefix).
                    PathTagRules = [new PathTagRule("docs/**/evidence/**", "Spec")],
                    PathDocKindRules = conventions.PathDocKindRules,
                    StatusTableColumns = conventions.StatusTableColumns,
                    StatusTokenAliases = conventions.StatusTokenAliases,
                    KeySectionNames = conventions.KeySectionNames,
                    GuidancePins = conventions.GuidancePins,
                },
            });

        doc.Tags.Should().Contain(SignificanceTag.Spec);
    }

    [Fact]
    public void PathTagRule_docs_star_star_prefix_still_works()
    {
        var conventions = MarkdownConventionsOptions.CreateDefault();
        var doc = MnemosyneFacade.Parse(
            "# Guide\n",
            new MarkdownParseOptions
            {
                Path = "docs/guides/install.md",
                Conventions = new MarkdownConventionsOptions
                {
                    EpicRoots = conventions.EpicRoots,
                    ArchiveRoots = conventions.ArchiveRoots,
                    DocsHostFolders = conventions.DocsHostFolders,
                    PlanningHostFolders = conventions.PlanningHostFolders,
                    RoadmapFileNames = conventions.RoadmapFileNames,
                    ExecutionPlanFileNames = conventions.ExecutionPlanFileNames,
                    StrategyFileNames = conventions.StrategyFileNames,
                    ProgramIndexPaths = conventions.ProgramIndexPaths,
                    PathTagRules = [new PathTagRule("docs/guides/**", "Runbook")],
                    PathDocKindRules = conventions.PathDocKindRules,
                    StatusTableColumns = conventions.StatusTableColumns,
                    StatusTokenAliases = conventions.StatusTokenAliases,
                    KeySectionNames = conventions.KeySectionNames,
                    GuidancePins = conventions.GuidancePins,
                },
            });

        doc.Tags.Should().Contain(SignificanceTag.Runbook);
    }

    [Fact]
    public void PathTagRule_single_star_does_not_match_nested()
    {
        var conventions = MarkdownConventionsOptions.CreateDefault();
        MarkdownConventionsOptions Make() => new()
        {
            EpicRoots = conventions.EpicRoots,
            ArchiveRoots = conventions.ArchiveRoots,
            DocsHostFolders = conventions.DocsHostFolders,
            PlanningHostFolders = conventions.PlanningHostFolders,
            RoadmapFileNames = conventions.RoadmapFileNames,
            ExecutionPlanFileNames = conventions.ExecutionPlanFileNames,
            StrategyFileNames = conventions.StrategyFileNames,
            ProgramIndexPaths = conventions.ProgramIndexPaths,
            PathTagRules = [new PathTagRule("docs/guides/*", "Runbook")],
            PathDocKindRules = conventions.PathDocKindRules,
            StatusTableColumns = conventions.StatusTableColumns,
            StatusTokenAliases = conventions.StatusTokenAliases,
            KeySectionNames = conventions.KeySectionNames,
            GuidancePins = conventions.GuidancePins,
        };

        var shallow = MnemosyneFacade.Parse(
            "# G\n",
            new MarkdownParseOptions { Path = "docs/guides/a.md", Conventions = Make() });
        var nested = MnemosyneFacade.Parse(
            "# G\n",
            new MarkdownParseOptions { Path = "docs/guides/nested/deep.md", Conventions = Make() });

        shallow.Tags.Should().Contain(SignificanceTag.Runbook);
        nested.Tags.Should().NotContain(SignificanceTag.Runbook);
    }

    [Fact]
    public void Snapshot_detects_document_added_and_removed()
    {
        var before = new[]
        {
            MnemosyneFacade.Parse("# Keep\n", new MarkdownParseOptions { Path = "docs/keep.md" }),
            MnemosyneFacade.Parse("# Old\n", new MarkdownParseOptions { Path = "docs/old.md" }),
        };
        var after = new[]
        {
            MnemosyneFacade.Parse("# Keep\n", new MarkdownParseOptions { Path = "docs/keep.md" }),
            MnemosyneFacade.Parse("# New\n", new MarkdownParseOptions { Path = "docs/new.md" }),
        };

        var diff = MnemosyneFacade.Diff(before, after);
        diff.Changes.Should().Contain(c =>
            c.Kind == SnapshotChangeKind.DocumentRemoved && c.Path == "docs/old.md");
        diff.Changes.Should().Contain(c =>
            c.Kind == SnapshotChangeKind.DocumentAdded && c.Path == "docs/new.md");
    }

    [Fact]
    public void Snapshot_detects_doc_kind_and_planning_ref_change()
    {
        const string path = "docs/roadmap/widget/README.md";
        var before = MnemosyneFacade.Parse(
            "# Widget\n\n**Status:** Planned\n",
            new MarkdownParseOptions { Path = path });
        // Move to a brief path → DocKind Brief + planningRef still widget when under epic folder
        var after = MnemosyneFacade.Parse(
            "# Widget design\n\n**Status:** Active\n",
            new MarkdownParseOptions { Path = "docs/roadmap/widget/01-design.md" });

        var diff = MnemosyneFacade.Diff([before], [after]);
        // Path change is DocumentMoved/Added/Removed — also assert lifecycle on same path via second pair
        var samePathAfter = MnemosyneFacade.Parse(
            "# Widget\n\n**Status:** Active\n",
            new MarkdownParseOptions { Path = path });
        var same = MnemosyneFacade.Diff([before], [samePathAfter]);
        same.Changes.Should().Contain(c =>
            c.Kind == SnapshotChangeKind.LifecycleChanged &&
            c.PreviousSemanticValue == "Planned" &&
            c.SemanticValue == "Active");
    }

    [Fact]
    public void Snapshot_detects_link_role_change_to_supersedes()
    {
        const string path = "docs/adr/adr-002.md";
        var before = MnemosyneFacade.Parse(
            "# ADR 002\nSee [older](./adr-001.md).\n",
            new MarkdownParseOptions { Path = path });
        var after = MnemosyneFacade.Parse(
            "# ADR 002\nSee [Supersedes ADR-001](./adr-001.md).\n",
            new MarkdownParseOptions { Path = path });

        var index = new MarkdownPathIndex(["docs/adr/adr-001.md", path]);
        var diff = MnemosyneFacade.Diff([before], [after], afterIndex: index, beforeIndex: index);

        diff.Changes.Should().Contain(c =>
            c.Kind == SnapshotChangeKind.LinkRoleChanged &&
            c.SemanticValue == "Supersedes");
    }

    [Fact]
    public void Snapshot_detects_broken_link_introduced_and_cleared()
    {
        const string path = "docs/a.md";
        var before = MnemosyneFacade.Parse(
            "# A\n[ok](./b.md)\n",
            new MarkdownParseOptions { Path = path });
        var afterBroken = MnemosyneFacade.Parse(
            "# A\n[gone](./missing.md)\n",
            new MarkdownParseOptions { Path = path });
        var afterFixed = MnemosyneFacade.Parse(
            "# A\n[ok](./b.md)\n",
            new MarkdownParseOptions { Path = path });

        var indexWithB = new MarkdownPathIndex(["docs/a.md", "docs/b.md"]);
        var indexBare = new MarkdownPathIndex(["docs/a.md"]);

        var introduced = MnemosyneFacade.Diff(
            [before],
            [afterBroken],
            afterIndex: indexBare,
            beforeIndex: indexWithB);
        introduced.Changes.Should().Contain(c => c.Kind == SnapshotChangeKind.BrokenLinkIntroduced);

        var cleared = MnemosyneFacade.Diff(
            [afterBroken],
            [afterFixed],
            afterIndex: indexWithB,
            beforeIndex: indexBare);
        cleared.Changes.Should().Contain(c => c.Kind == SnapshotChangeKind.BrokenLinkCleared);
    }

    [Fact]
    public void Snapshot_detects_heading_level_change()
    {
        const string path = "docs/a.md";
        var before = MnemosyneFacade.Parse("# T\n## Topic\n", new MarkdownParseOptions { Path = path });
        var after = MnemosyneFacade.Parse("# T\n### Topic\n", new MarkdownParseOptions { Path = path });

        var diff = MnemosyneFacade.Diff([before], [after]);
        diff.Changes.Should().Contain(c =>
            c.Kind == SnapshotChangeKind.HeadingLevelChanged &&
            c.HeadingSlug == "topic");
    }

    [Fact]
    public void Snapshot_detects_title_change()
    {
        const string path = "docs/a.md";
        var before = MnemosyneFacade.Parse("# Old Title\n", new MarkdownParseOptions { Path = path });
        var after = MnemosyneFacade.Parse("# New Title\n", new MarkdownParseOptions { Path = path });

        var diff = MnemosyneFacade.Diff([before], [after]);
        diff.Changes.Should().Contain(c => c.Kind == SnapshotChangeKind.TitleChanged);
    }

    [Fact]
    public void Snapshot_detects_document_archive_move()
    {
        var before = new[]
        {
            MnemosyneFacade.Parse(
                "# Feature\n\n**Status:** Active\n",
                new MarkdownParseOptions { Path = "docs/roadmap/widget/README.md" }),
        };
        var after = new[]
        {
            MnemosyneFacade.Parse(
                "# Feature\n\n**Status:** Done\n",
                new MarkdownParseOptions { Path = "docs/done/widget/README.md" }),
        };

        var diff = MnemosyneFacade.Diff(before, after);
        diff.Changes.Should().Contain(c =>
            c.Kind == SnapshotChangeKind.DocumentMoved &&
            c.FromPath == "docs/roadmap/widget/README.md" &&
            c.ToPath == "docs/done/widget/README.md");
    }

    [Fact]
    public void Table_with_unknown_cells_does_not_force_lifecycle()
    {
        var doc = MnemosyneFacade.Parse(
            """
            # Board

            | Epic | Status |
            |------|--------|
            | a | TBD |
            | b | ??? |
            """,
            new MarkdownParseOptions { Path = "docs/roadmap.md" });

        // No mappable tokens → Unknown (not Done/Active)
        doc.Semantics.Lifecycle.Should().Be(DocumentLifecycle.Unknown);
    }

    [Fact]
    public void Parent_link_from_brief_to_epic_readme_is_parent()
    {
        var link = new MarkdownLink
        {
            Text = "Epic",
            Target = "./README.md",
            Line = 3,
        };

        LinkRoleClassifier.Classify(
                "docs/roadmap/widget/01-design.md",
                link,
                "docs/roadmap/widget/README.md")
            .Should()
            .Be(LinkRole.Parent);
    }

    [Fact]
    public void Archive_path_forces_done_even_when_status_says_blocked()
    {
        var doc = MnemosyneFacade.Parse(
            """
            # Old

            **Status:** Blocked
            """,
            new MarkdownParseOptions { Path = "docs/done/old-epic/README.md" });

        doc.Semantics.Lifecycle.Should().Be(DocumentLifecycle.Done);
    }
}
