using FluentAssertions;
using Mnemosyne;

namespace Mnemosyne.UnitTests;

/// <summary>Targeted cases to push aggregate branch coverage ≥ 85%.</summary>
public class BranchHeadroomTests
{
    [Theory]
    [InlineData("CLAUDE.md", SignificanceTag.AgentControl)]
    [InlineData("GEMINI.md", SignificanceTag.AgentControl)]
    [InlineData("conventions.md", SignificanceTag.AgentControl)]
    [InlineData(".cursorrules", SignificanceTag.AgentControl)]
    [InlineData(".windsurfrules", SignificanceTag.AgentControl)]
    [InlineData("SKILL.md", SignificanceTag.AgentControl)]
    [InlineData(".github/instructions/team.md", SignificanceTag.AgentControl)]
    [InlineData(".continue/rules/style.md", SignificanceTag.AgentControl)]
    [InlineData(".clinerules/base.md", SignificanceTag.AgentControl)]
    [InlineData(".claude/rules.md", SignificanceTag.AgentControl)]
    [InlineData(".cursor/extra/note.mdc", SignificanceTag.AgentControl)]
    [InlineData("BRANCHES.md", SignificanceTag.BranchingControl)]
    [InlineData(".github/branch-protection.md", SignificanceTag.BranchingControl)]
    [InlineData(".github/MERGE_STRATEGY.md", SignificanceTag.BranchingControl)]
    [InlineData(".github/pull_request_template.md", SignificanceTag.BranchingControl)]
    [InlineData(".github/pull-request-guide.md", SignificanceTag.BranchingControl)]
    [InlineData(".github/CODEOWNERS", SignificanceTag.BranchingControl)]
    [InlineData("docs/architecture-overview.md", SignificanceTag.Architecture)]
    [InlineData("docs/system-architecture.md", SignificanceTag.Architecture)]
    [InlineData("docs/product-overview.md", SignificanceTag.Product)]
    [InlineData("docs/hand-over.md", SignificanceTag.Runbook)]
    [InlineData("docs/hand_over.md", SignificanceTag.Runbook)]
    [InlineData("gettingstarted.md", SignificanceTag.Runbook)]
    [InlineData("documentation/roadmap.md", SignificanceTag.Roadmap)]
    [InlineData("docs/planning/roadmap.md", SignificanceTag.Roadmap)]
    [InlineData("docs/plan/execution-plan.md", SignificanceTag.ExecutionPlan)]
    [InlineData("strategy.md", SignificanceTag.Strategy)]
    public void Covers_tagger_filename_and_shallow_program_branches(string path, SignificanceTag expected)
    {
        var doc = MnemosyneFacade.Parse("# Hi\n", new MarkdownParseOptions { Path = path });
        doc.Tags.Should().Contain(expected);
    }

    [Fact]
    public void Front_matter_plain_tags_array_maps_significance()
    {
        var md = """
            ---
            tags: [Architecture, Security]
            ---
            # X
            """;

        var explanation = MnemosyneFacade.Explain(md, new MarkdownParseOptions { Path = "notes.md" });
        explanation.Document.Tags.Should().Contain(SignificanceTag.Architecture);
        explanation.Document.Tags.Should().Contain(SignificanceTag.Security);
        explanation.RulesFired.Should().Contain(r => r.Contains("front-matter tags", StringComparison.Ordinal));
    }

    [Fact]
    public void Docs_readme_is_reference_kind()
    {
        var doc = MnemosyneFacade.Parse("# Docs\n", new MarkdownParseOptions { Path = "docs/README.md" });
        doc.Semantics.DocKind.Should().Be(DocumentKind.Reference);
    }

    [Fact]
    public void Epic_roots_readme_is_index_kind()
    {
        var doc = MnemosyneFacade.Parse("# Roadmap root\n", new MarkdownParseOptions { Path = "docs/roadmap/README.md" });
        doc.Semantics.DocKind.Should().Be(DocumentKind.Index);
    }

    [Fact]
    public void Nested_epic_roadmap_is_index_kind()
    {
        var doc = MnemosyneFacade.Parse("# Nested\n", new MarkdownParseOptions { Path = "docs/roadmap/widget/ROADMAP.md" });
        doc.Semantics.DocKind.Should().Be(DocumentKind.Index);
    }

    [Fact]
    public void Invalid_pathDocKind_token_is_ignored()
    {
        var conventions = MarkdownConventionsOptions.CreateDefault();
        var doc = MnemosyneFacade.Parse(
            "# X\n",
            new MarkdownParseOptions
            {
                Path = "docs/weird.md",
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
                    PathDocKindRules = [new PathDocKindRule("docs/weird.md", "NotAKind")],
                    StatusTableColumns = conventions.StatusTableColumns,
                    StatusTokenAliases = conventions.StatusTokenAliases,
                    KeySectionNames = conventions.KeySectionNames,
                    GuidancePins = conventions.GuidancePins,
                },
            });

        doc.Semantics.DocKind.Should().Be(DocumentKind.Unknown);
    }

    [Fact]
    public void Parses_wiki_links_with_and_without_anchors()
    {
        var doc = MnemosyneFacade.Parse(
            "# T\nSee [[Other Page]] and [[Other Page#Section]].\n",
            new MarkdownParseOptions { Path = "docs/a.md" });

        doc.Links.Should().Contain(l => l.Target == "Other Page" && l.Anchor == null);
        doc.Links.Should().Contain(l => l.Target == "Other Page" && l.Anchor == "Section");
    }

    [Fact]
    public void Parent_via_dotdot_to_program_roadmap()
    {
        var link = new MarkdownLink
        {
            Text = "Roadmap",
            Target = "../ROADMAP.md",
            Line = 2,
        };

        LinkRoleClassifier.Classify(
                "docs/roadmap/widget/01-design.md",
                link,
                "docs/roadmap/ROADMAP.md")
            .Should()
            .Be(LinkRole.Parent);
    }

    [Fact]
    public void Parent_via_dotdot_to_same_epic_readme()
    {
        var link = new MarkdownLink
        {
            Text = "Up",
            Target = "../README.md",
            Line = 2,
        };

        LinkRoleClassifier.Classify(
                "docs/roadmap/widget/01-design.md",
                link,
                "docs/roadmap/widget/README.md")
            .Should()
            .Be(LinkRole.Parent);
    }

    [Fact]
    public void Plans_from_nested_epic_roadmap_file()
    {
        var link = new MarkdownLink
        {
            Text = "Brief",
            Target = "./01-design.md",
            Line = 4,
        };

        LinkRoleClassifier.Classify(
                "docs/roadmap/widget/ROADMAP.md",
                link,
                "docs/roadmap/widget/01-design.md")
            .Should()
            .Be(LinkRole.Plans);
    }

    [Fact]
    public void Plans_from_program_roadmap_lowercase_path()
    {
        var link = new MarkdownLink
        {
            Text = "Epic",
            Target = "./widget/",
            Line = 4,
        };

        LinkRoleClassifier.Classify(
                "docs/roadmap/roadmap.md",
                link,
                "docs/roadmap/widget/README.md")
            .Should()
            .Be(LinkRole.Plans);
    }

    [Fact]
    public void External_link_stays_generic_role()
    {
        var link = new MarkdownLink
        {
            Text = "Web",
            Target = "https://example.com",
            Line = 1,
            IsExternal = true,
        };

        LinkRoleClassifier.Classify("docs/a.md", link, null).Should().Be(LinkRole.Generic);
        LinkRoleClassifier.ToKindString(LinkRole.Generic).Should().Be("Generic");
    }

    [Fact]
    public void Snapshot_detects_key_section_removed_and_changed()
    {
        const string path = "docs/roadmap/EXECUTION_PLAN.md";
        var before = MnemosyneFacade.Parse(
            """
            # Plan

            **Status:** Active

            ## Next

            | Item | Notes |
            |------|-------|
            | a | one |

            ## Active

            work
            """,
            new MarkdownParseOptions { Path = path });
        var after = MnemosyneFacade.Parse(
            """
            # Plan

            **Status:** Active

            ## Active

            | Item | Notes |
            |------|-------|
            | a | two |
            """,
            new MarkdownParseOptions { Path = path });

        var diff = MnemosyneFacade.Diff([before], [after]);
        diff.Changes.Should().Contain(c =>
            c.Kind == SnapshotChangeKind.KeySectionRemoved && c.SectionName == "Next");
    }

    [Fact]
    public void Snapshot_detects_link_added_and_removed()
    {
        const string path = "docs/a.md";
        var before = MnemosyneFacade.Parse(
            "# A\n[old](./old.md)\n",
            new MarkdownParseOptions { Path = path });
        var after = MnemosyneFacade.Parse(
            "# A\n[new](./new.md)\n",
            new MarkdownParseOptions { Path = path });
        var index = new MarkdownPathIndex(["docs/a.md", "docs/old.md", "docs/new.md"]);

        var diff = MnemosyneFacade.Diff([before], [after], afterIndex: index, beforeIndex: index);
        diff.Changes.Should().Contain(c => c.Kind == SnapshotChangeKind.LinkRemoved);
        diff.Changes.Should().Contain(c => c.Kind == SnapshotChangeKind.LinkAdded);
    }

    [Fact]
    public void Resolve_hash_only_and_dot_slash_targets()
    {
        var doc = MnemosyneFacade.Parse(
            "# A\n## Install\nSee [here](#install) and [rel](./b.md).\n",
            new MarkdownParseOptions { Path = "docs/a.md" });
        var index = new MarkdownPathIndex(
            ["docs/a.md", "docs/b.md"],
            new Dictionary<string, IEnumerable<string>> { ["docs/a.md"] = ["install"] });

        MnemosyneFacade.ResolveLinks(doc, index).Should().BeEmpty();
    }

    [Fact]
    public void Resolve_reports_missing_same_doc_anchor_and_rooted_target()
    {
        var doc = MnemosyneFacade.Parse(
            "# A\nSee [missing](#nope) and [abs](/docs/b.md#gone) and [up](../../outside.md).\n",
            new MarkdownParseOptions { Path = "docs/nested/a.md" });
        var index = new MarkdownPathIndex(
            ["docs/nested/a.md", "docs/b.md"],
            new Dictionary<string, IEnumerable<string>>
            {
                ["docs/nested/a.md"] = ["a"],
                ["docs/b.md"] = ["intro"],
            });

        var diags = MnemosyneFacade.ResolveLinks(doc, index);
        diags.Should().Contain(d =>
            d.Kind == LinkDiagnosticKind.MissingAnchor && d.Link.Target == "");
        diags.Should().Contain(d =>
            d.Kind == LinkDiagnosticKind.MissingAnchor && d.Link.Anchor == "gone");
        diags.Should().Contain(d =>
            d.Kind == LinkDiagnosticKind.MissingTarget && d.Link.Target.Contains("outside"));
    }

    [Fact]
    public void Resolve_skips_external_and_empty_targets()
    {
        var doc = MnemosyneFacade.Parse(
            "# A\n[web](https://example.com) [mail](mailto:a@b.c) [empty]().\n",
            new MarkdownParseOptions { Path = "docs/a.md" });
        var index = new MarkdownPathIndex(["docs/a.md"]);

        MnemosyneFacade.ResolveLinks(doc, index).Should().BeEmpty();
    }

    [Fact]
    public void Resolve_pathless_document_still_checks_heading_anchors()
    {
        var doc = MnemosyneFacade.Parse("# Title\n## Known\n[bad](#missing)\n");
        var index = new MarkdownPathIndex([]);

        var diags = MnemosyneFacade.ResolveLinks(doc, index);
        diags.Should().ContainSingle(d => d.Kind == LinkDiagnosticKind.MissingAnchor);
    }

    [Fact]
    public void Nested_repo_github_paths_still_tag_branching_and_agent()
    {
        var branching = MnemosyneFacade.Parse(
            "# P\n",
            new MarkdownParseOptions { Path = "src/.github/branch-protection.md" });
        branching.Tags.Should().Contain(SignificanceTag.BranchingControl);

        var agent = MnemosyneFacade.Parse(
            "# R\n",
            new MarkdownParseOptions { Path = "tools/.cursor/rules/style.mdc" });
        agent.Tags.Should().Contain(SignificanceTag.AgentControl);
    }

    [Fact]
    public void Empty_path_parse_leaves_unknown_semantics()
    {
        var doc = MnemosyneFacade.Parse("# Untitled\n");
        doc.Semantics.DocKind.Should().Be(DocumentKind.Unknown);
        doc.Semantics.Lifecycle.Should().Be(DocumentLifecycle.Unknown);
    }

    [Fact]
    public void Owner_and_status_from_front_matter()
    {
        var md = """
            ---
            status: Draft
            owner: platform
            ---
            # Note
            """;

        var doc = MnemosyneFacade.Parse(md, new MarkdownParseOptions { Path = "docs/note.md" });
        doc.Semantics.Lifecycle.Should().Be(DocumentLifecycle.Draft);
        doc.Semantics.Owner.Should().Be("platform");
    }

    [Fact]
    public void Table_without_pipe_edges_still_aggregates()
    {
        var doc = MnemosyneFacade.Parse(
            """
            # Board

            Epic | Status
            --- | ---
            a | active
            b | done
            """,
            new MarkdownParseOptions { Path = "docs/roadmap.md" });

        doc.Semantics.Lifecycle.Should().Be(DocumentLifecycle.Active);
        doc.Semantics.StatusRaw.Should().Be("active");
    }

    [Fact]
    public void PathTagRule_empty_pattern_does_not_match()
    {
        var conventions = MarkdownConventionsOptions.CreateDefault();
        var doc = MnemosyneFacade.Parse(
            "# X\n",
            new MarkdownParseOptions
            {
                Path = "docs/x.md",
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
                    PathTagRules = [new PathTagRule("", "Spec")],
                    PathDocKindRules = conventions.PathDocKindRules,
                    StatusTableColumns = conventions.StatusTableColumns,
                    StatusTokenAliases = conventions.StatusTokenAliases,
                    KeySectionNames = conventions.KeySectionNames,
                    GuidancePins = conventions.GuidancePins,
                },
            });

        doc.Tags.Should().NotContain(SignificanceTag.Spec);
    }

    [Fact]
    public void Exotic_double_star_glob_falls_through_regex()
    {
        var conventions = MarkdownConventionsOptions.CreateDefault();
        var doc = MnemosyneFacade.Parse(
            "# X\n",
            new MarkdownParseOptions
            {
                Path = "docs/foo/bar/baz.md",
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
                    PathTagRules = [new PathTagRule("docs/**/ba*/**", "Spec")],
                    PathDocKindRules = conventions.PathDocKindRules,
                    StatusTableColumns = conventions.StatusTableColumns,
                    StatusTokenAliases = conventions.StatusTokenAliases,
                    KeySectionNames = conventions.KeySectionNames,
                    GuidancePins = conventions.GuidancePins,
                },
            });

        doc.Tags.Should().Contain(SignificanceTag.Spec);
    }
}
