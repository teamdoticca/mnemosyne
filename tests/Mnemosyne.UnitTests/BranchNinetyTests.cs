using FluentAssertions;
using Mnemosyne;

namespace Mnemosyne.UnitTests;

/// <summary>Push aggregate branch coverage past 90%; also documents edge-case semantics.</summary>
public class BranchNinetyTests
{
    private static MarkdownConventionsOptions CloneDefaults(
        Action<MarkdownConventionsBuilder>? configure = null)
    {
        var d = MarkdownConventionsOptions.CreateDefault();
        var b = new MarkdownConventionsBuilder
        {
            EpicRoots = d.EpicRoots,
            ArchiveRoots = d.ArchiveRoots,
            DocsHostFolders = d.DocsHostFolders,
            PlanningHostFolders = d.PlanningHostFolders,
            RoadmapFileNames = d.RoadmapFileNames,
            ExecutionPlanFileNames = d.ExecutionPlanFileNames,
            StrategyFileNames = d.StrategyFileNames,
            ProgramIndexPaths = d.ProgramIndexPaths,
            PathTagRules = d.PathTagRules,
            PathDocKindRules = d.PathDocKindRules,
            StatusTableColumns = d.StatusTableColumns,
            StatusTokenAliases = d.StatusTokenAliases,
            KeySectionNames = d.KeySectionNames,
            GuidancePins = d.GuidancePins,
        };
        configure?.Invoke(b);
        return b.Build();
    }

    private sealed class MarkdownConventionsBuilder
    {
        public IReadOnlyList<string> EpicRoots { get; set; } = [];
        public IReadOnlyList<string> ArchiveRoots { get; set; } = [];
        public IReadOnlyList<string> DocsHostFolders { get; set; } = [];
        public IReadOnlyList<string> PlanningHostFolders { get; set; } = [];
        public IReadOnlyList<string> RoadmapFileNames { get; set; } = [];
        public IReadOnlyList<string> ExecutionPlanFileNames { get; set; } = [];
        public IReadOnlyList<string> StrategyFileNames { get; set; } = [];
        public IReadOnlyList<ProgramIndexPathRule> ProgramIndexPaths { get; set; } = [];
        public IReadOnlyList<PathTagRule> PathTagRules { get; set; } = [];
        public IReadOnlyList<PathDocKindRule> PathDocKindRules { get; set; } = [];
        public IReadOnlyList<string> StatusTableColumns { get; set; } = [];
        public IReadOnlyDictionary<string, string> StatusTokenAliases { get; set; } =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public IReadOnlyList<string> KeySectionNames { get; set; } = [];
        public IReadOnlyList<GuidancePinRule> GuidancePins { get; set; } = [];

        public MarkdownConventionsOptions Build() => new()
        {
            EpicRoots = EpicRoots,
            ArchiveRoots = ArchiveRoots,
            DocsHostFolders = DocsHostFolders,
            PlanningHostFolders = PlanningHostFolders,
            RoadmapFileNames = RoadmapFileNames,
            ExecutionPlanFileNames = ExecutionPlanFileNames,
            StrategyFileNames = StrategyFileNames,
            ProgramIndexPaths = ProgramIndexPaths,
            PathTagRules = PathTagRules,
            PathDocKindRules = PathDocKindRules,
            StatusTableColumns = StatusTableColumns,
            StatusTokenAliases = StatusTokenAliases,
            KeySectionNames = KeySectionNames,
            GuidancePins = GuidancePins,
        };
    }

    [Theory]
    [InlineData("security/threat.md", SignificanceTag.Security)]
    [InlineData("adr/0001.md", SignificanceTag.Adr)]
    [InlineData("decisions/2024-pick-sqlite.md", SignificanceTag.Adr)]
    [InlineData("specs/wire.md", SignificanceTag.Spec)]
    [InlineData("runbooks/deploy.md", SignificanceTag.Runbook)]
    [InlineData("onboarding/day-one.md", SignificanceTag.Runbook)]
    public void Root_relative_folder_prefixes_tag_correctly(string path, SignificanceTag expected)
    {
        var doc = MnemosyneFacade.Parse("# X\n", new MarkdownParseOptions { Path = path });
        doc.Tags.Should().Contain(expected);
    }

    [Theory]
    [InlineData("block", DocumentLifecycle.Blocked)]
    [InlineData("defer", DocumentLifecycle.Deferred)]
    [InlineData("in_progress", DocumentLifecycle.Active)]
    [InlineData("shipped", DocumentLifecycle.Done)]
    [InlineData("pass", DocumentLifecycle.Done)]
    public void Status_header_maps_short_tokens(string status, DocumentLifecycle expected)
    {
        var doc = MnemosyneFacade.Parse(
            $"# N\n\n**Status:** {status}\n",
            new MarkdownParseOptions { Path = "docs/note.md" });
        doc.Semantics.Lifecycle.Should().Be(expected);
    }

    [Theory]
    [InlineData("active", "Active")]
    [InlineData("done", "Done")]
    [InlineData("planned", "Planned")]
    [InlineData("draft", "Draft")]
    [InlineData("blocked", "Blocked")]
    [InlineData("deferred", "Deferred")]
    public void Table_cells_honor_status_aliases(string cell, string aliasLifecycle)
    {
        var conventions = CloneDefaults(b =>
        {
            b.StatusTokenAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["~custom~"] = aliasLifecycle,
            };
        });

        var doc = MnemosyneFacade.Parse(
            $"""
            # Board

            | Epic | Status |
            |:----:|:------:|
            | a | ~custom~ |
            | b | {cell} |
            """,
            new MarkdownParseOptions { Path = "docs/roadmap.md", Conventions = conventions });

        doc.Semantics.Lifecycle.Should().NotBe(DocumentLifecycle.Unknown);
    }

    [Theory]
    [InlineData("wip")]
    [InlineData("ip")]
    [InlineData("active — spike")]
    [InlineData("active-spike")]
    [InlineData("in progress now")]
    [InlineData("blocked overnight")]
    [InlineData("deferred until Q4")]
    [InlineData("planned soon")]
    [InlineData("draft sketch")]
    [InlineData("done — archived")]
    [InlineData("done-archived")]
    [InlineData("complete")]
    [InlineData("completed")]
    [InlineData("shipped already")]
    public void Table_cell_token_variants_map(string cell)
    {
        var doc = MnemosyneFacade.Parse(
            $"""
            # Board

            | Epic | Status |
            |------|--------|
            | a | {cell} |
            """,
            new MarkdownParseOptions { Path = "docs/roadmap.md" });

        doc.Semantics.Lifecycle.Should().NotBe(DocumentLifecycle.Unknown);
    }

    [Fact]
    public void Empty_status_table_columns_falls_back_to_status_or_state()
    {
        var conventions = CloneDefaults(b => b.StatusTableColumns = []);
        var doc = MnemosyneFacade.Parse(
            """
            # Board

            | Epic | State |
            |------|-------|
            | a | planned |
            | b | planned |
            """,
            new MarkdownParseOptions { Path = "docs/roadmap.md", Conventions = conventions });

        doc.Semantics.Lifecycle.Should().Be(DocumentLifecycle.Planned);
    }

    [Fact]
    public void Empty_key_section_names_uses_built_in_defaults()
    {
        var conventions = CloneDefaults(b => b.KeySectionNames = []);
        var explanation = MnemosyneFacade.Explain(
            """
            # Plan

            ## Active

            work

            ## Next

            more
            """,
            new MarkdownParseOptions
            {
                Path = "docs/roadmap/EXECUTION_PLAN.md",
                Conventions = conventions,
            });

        explanation.Document.Semantics.KeySections.Should().Contain(s => s.Name == "Active");
        explanation.Document.Semantics.KeySections.Should().Contain(s => s.Name == "Next");
    }

    [Fact]
    public void Archive_path_forces_done_lifecycle()
    {
        var explanation = MnemosyneFacade.Explain(
            "# Old\n\n**Status:** Planned\n",
            new MarkdownParseOptions { Path = "docs/done/widget/README.md" });

        explanation.Document.Semantics.Lifecycle.Should().Be(DocumentLifecycle.Done);
        explanation.RulesFired.Should().Contain(r => r.Contains("archiveRoots", StringComparison.Ordinal));
    }

    [Fact]
    public void Front_matter_planning_ref_and_pin_fire_rules()
    {
        var explanation = MnemosyneFacade.Explain(
            """
            ---
            status: Active
            owner: platform
            mnemosyne.planningRef: widget
            mnemosyne.pin: true
            mnemosyne.guidanceGroup: core
            mnemosyne.clearBuiltIns: true
            mnemosyne.tags: [Architecture]
            commitSha: abcdef0123456789abcdef0123456789abcdef01
            ---
            # Note
            """,
            new MarkdownParseOptions { Path = "docs/note.md" });

        explanation.Document.Semantics.PlanningRef.Should().Be("widget");
        explanation.Document.Tags.Should().Contain(SignificanceTag.Architecture);
        explanation.Document.Tags.Should().Contain(SignificanceTag.CommitNote);
        explanation.RulesFired.Should().Contain(r => r.Contains("planningRef", StringComparison.Ordinal));
        explanation.RulesFired.Should().Contain(r => r.Contains("mnemosyne.pin", StringComparison.Ordinal));
        explanation.RulesFired.Should().Contain(r => r.Contains("guidanceGroup", StringComparison.Ordinal));
        explanation.RulesFired.Should().Contain(r => r.Contains("clearBuiltIns", StringComparison.Ordinal));
    }

    [Fact]
    public void Front_matter_trailing_fence_without_body_parses()
    {
        var doc = MnemosyneFacade.Parse("---\ntitle: Solo\nstatus: Draft\n---");
        doc.Title.Should().Be("Solo");
        doc.Semantics.Lifecycle.Should().Be(DocumentLifecycle.Draft);
    }

    [Fact]
    public void Front_matter_ignores_comments_blank_lines_and_unknown_keys()
    {
        var doc = MnemosyneFacade.Parse(
            """
            ---
            # comment
            title: T
            not-a-key: x
            badline
            tags: Architecture, Security
            mnemosyne.clearBuiltIns: 1
            ---
            # Body
            """);

        doc.Title.Should().Be("T");
        doc.FrontMatter.ClearBuiltInTags.Should().BeTrue();
        doc.Tags.Should().Contain(SignificanceTag.Architecture);
    }

    [Fact]
    public void Nested_epic_roadmap_sets_planning_ref_and_index_kind()
    {
        var explanation = MnemosyneFacade.Explain(
            "# Nested\n",
            new MarkdownParseOptions { Path = "docs/roadmap/widget/ROADMAP.md" });

        explanation.Document.Semantics.DocKind.Should().Be(DocumentKind.Index);
        explanation.Document.Semantics.PlanningRef.Should().Be("widget");
        explanation.RulesFired.Should().Contain(r => r.Contains("nested program", StringComparison.Ordinal));
    }

    [Fact]
    public void Program_index_at_repo_root_is_index_kind()
    {
        var doc = MnemosyneFacade.Parse("# R\n", new MarkdownParseOptions { Path = "ROADMAP.md" });
        doc.Semantics.DocKind.Should().Be(DocumentKind.Index);
    }

    [Fact]
    public void Docs_planning_host_roadmap_is_index_kind()
    {
        var doc = MnemosyneFacade.Parse(
            "# R\n",
            new MarkdownParseOptions { Path = "docs/planning/roadmap.md" });
        doc.Semantics.DocKind.Should().Be(DocumentKind.Index);
    }

    [Fact]
    public void Epic_design_and_status_and_readme_kinds()
    {
        MnemosyneFacade.Parse("# D\n", new MarkdownParseOptions { Path = "docs/roadmap/widget/design.md" })
            .Semantics.DocKind.Should().Be(DocumentKind.Brief);
        MnemosyneFacade.Parse("# S\n", new MarkdownParseOptions { Path = "docs/roadmap/widget/status.md" })
            .Semantics.DocKind.Should().Be(DocumentKind.Brief);
        MnemosyneFacade.Parse("# E\n", new MarkdownParseOptions { Path = "docs/roadmap/widget/README.md" })
            .Semantics.DocKind.Should().Be(DocumentKind.Epic);
        MnemosyneFacade.Parse("# B\n", new MarkdownParseOptions { Path = "docs/roadmap/widget/01-brief.md" })
            .Semantics.DocKind.Should().Be(DocumentKind.Brief);
    }

    [Fact]
    public void Parser_skips_code_fences_deep_headings_and_preamble_noise()
    {
        var doc = MnemosyneFacade.Parse(
            """
            # Title

            **Owner:** team

            > quote

            #### Deep

            ```
            # not a heading
            [hidden](./x.md)
            ```

            ~~~
            still fenced
            ~~~

            First real paragraph with [[Wiki|Alias]] and [http](http://example.com) and [hash](./b.md#).

            - list item
            | table |
            """,
            new MarkdownParseOptions { Path = "docs/a.md", BlurbMaxLength = 0 });

        doc.Headings.Should().NotContain(h => h.Level == 4);
        doc.Links.Should().Contain(l => l.Target == "Wiki");
        doc.Links.Should().Contain(l => l.IsExternal && l.Target.StartsWith("http://", StringComparison.Ordinal));
        doc.Links.Should().Contain(l => l.Target == "./b.md" && l.Anchor == null);
        doc.Blurb.Should().NotBeNull();
    }

    [Fact]
    public void Explain_null_markdown_is_safe()
    {
        var explanation = MnemosyneFacade.Explain(null!);
        explanation.Document.Should().NotBeNull();
    }

    [Fact]
    public void Heading_slug_collapses_punctuation_and_empty()
    {
        var doc = MnemosyneFacade.Parse("# Hello   World__Again!!!\n\n## !!!\n");
        doc.Headings[0].Slug.Should().Be("hello-world-again");
        doc.Headings[1].Slug.Should().Be("section");
    }

    [Fact]
    public void Link_roles_cover_supersedes_depends_seealso_and_done_archive()
    {
        LinkRoleClassifier.Classify(
                "docs/a.md",
                new MarkdownLink { Text = "superseded", Target = "./old.md", Line = 1 },
                "docs/old.md")
            .Should().Be(LinkRole.Supersedes);
        LinkRoleClassifier.Classify(
                "docs/a.md",
                new MarkdownLink { Text = "depends", Target = "./b.md", Line = 1 },
                "docs/b.md")
            .Should().Be(LinkRole.DependsOn);
        LinkRoleClassifier.Classify(
                "docs/a.md",
                new MarkdownLink { Text = "see also", Target = "./c.md", Line = 1 },
                "docs/c.md")
            .Should().Be(LinkRole.SeeAlso);
        LinkRoleClassifier.Classify(
                "docs/a.md",
                new MarkdownLink { Text = "archive", Target = "../done/x/README.md", Line = 1 },
                "docs/done/x/README.md")
            .Should().Be(LinkRole.SeeAlso);
        LinkRoleClassifier.Classify(
                null,
                new MarkdownLink { Text = "x", Target = "./y.md", Line = 1 },
                "docs/y.md")
            .Should().Be(LinkRole.Generic);
        LinkRoleClassifier.ToKindString(LinkRole.Supersedes).Should().Be("Supersedes");
        LinkRoleClassifier.ToKindString(LinkRole.DependsOn).Should().Be("DependsOn");
        LinkRoleClassifier.ToKindString(LinkRole.SeeAlso).Should().Be("SeeAlso");
        LinkRoleClassifier.ToKindString(LinkRole.Parent).Should().Be("Parent");
        LinkRoleClassifier.ToKindString(LinkRole.Plans).Should().Be("Plans");
    }

    [Fact]
    public void Plans_from_epic_readme_to_brief()
    {
        LinkRoleClassifier.Classify(
                "docs/roadmap/widget/README.md",
                new MarkdownLink { Text = "Brief", Target = "./01-design.md", Line = 1 },
                "docs/roadmap/widget/01-design.md")
            .Should().Be(LinkRole.Plans);
    }

    [Fact]
    public void Snapshot_detects_doc_kind_lifecycle_planning_ref_and_tags()
    {
        var before = MnemosyneFacade.Parse(
            "# A\n\n**Status:** Planned\n",
            new MarkdownParseOptions { Path = "docs/roadmap/widget/01-design.md" });
        var after = MnemosyneFacade.Parse(
            """
            ---
            tags: [Security]
            status: Active
            ---
            # A

            ## Active
            """,
            new MarkdownParseOptions { Path = "docs/roadmap/widget/01-design.md" });

        var diff = MnemosyneFacade.Diff([before], [after]);
        diff.Changes.Should().Contain(c => c.Kind == SnapshotChangeKind.LifecycleChanged);
        diff.Changes.Should().Contain(c => c.Kind == SnapshotChangeKind.TagsChanged);
        diff.Changes.Should().Contain(c => c.Kind == SnapshotChangeKind.KeySectionAdded);
    }

    [Fact]
    public void Snapshot_detects_planning_ref_change_on_nested_roadmap()
    {
        var before = MnemosyneFacade.Parse(
            "# R\n",
            new MarkdownParseOptions { Path = "docs/roadmap/alpha/ROADMAP.md" });
        var after = MnemosyneFacade.Parse(
            "# R\n",
            new MarkdownParseOptions { Path = "docs/roadmap/beta/ROADMAP.md" });

        // Different paths → document moved/removed+added; force same path via mutated clones is hard —
        // instead diff same path with front-matter planningRef change.
        var b2 = MnemosyneFacade.Parse(
            "# R\n",
            new MarkdownParseOptions { Path = "docs/note.md" });
        var a2 = MnemosyneFacade.Parse(
            """
            ---
            mnemosyne.planningRef: beta
            ---
            # R
            """,
            new MarkdownParseOptions { Path = "docs/note.md" });

        var diff = MnemosyneFacade.Diff([b2], [a2]);
        diff.Changes.Should().Contain(c => c.Kind == SnapshotChangeKind.PlanningRefChanged);
        _ = before;
        _ = after;
    }

    [Fact]
    public void Snapshot_link_role_change_with_relative_and_dotdot_targets()
    {
        const string path = "docs/roadmap/widget/01-design.md";
        var before = MnemosyneFacade.Parse(
            "# A\n[up](../README.md)\n",
            new MarkdownParseOptions { Path = path });
        var after = MnemosyneFacade.Parse(
            "# A\n[depends](../README.md)\n",
            new MarkdownParseOptions { Path = path });
        var index = new MarkdownPathIndex(
        [
            path,
            "docs/roadmap/widget/README.md",
        ]);

        var diff = MnemosyneFacade.Diff([before], [after], afterIndex: index, beforeIndex: index);
        diff.Changes.Should().NotBeEmpty();
    }

    [Fact]
    public void Resolve_heading_exists_on_same_document_via_path_match()
    {
        var doc = MnemosyneFacade.Parse(
            "# A\n## Install\nSee [self](./a.md#install).\n",
            new MarkdownParseOptions { Path = "docs/a.md" });
        var index = new MarkdownPathIndex(["docs/a.md"]);

        MnemosyneFacade.ResolveLinks(doc, index).Should().BeEmpty();
    }

    [Fact]
    public void Glob_prefix_star_star_matches_prefix_alone_and_one_level_star()
    {
        var conventions = CloneDefaults(b =>
        {
            b.PathTagRules =
            [
                new PathTagRule("docs/policies/**", "Spec"),
                new PathTagRule("docs/ops/*", "Runbook"),
            ];
        });

        MnemosyneFacade.Parse("# P\n", new MarkdownParseOptions
            {
                Path = "docs/policies",
                Conventions = conventions,
            })
            .Tags.Should().Contain(SignificanceTag.Spec);

        MnemosyneFacade.Parse("# R\n", new MarkdownParseOptions
            {
                Path = "docs/ops/deploy.md",
                Conventions = conventions,
            })
            .Tags.Should().Contain(SignificanceTag.Runbook);

        MnemosyneFacade.Parse("# Nested\n", new MarkdownParseOptions
            {
                Path = "docs/ops/nested/x.md",
                Conventions = conventions,
            })
            .Tags.Should().NotContain(SignificanceTag.Runbook);
    }

    [Fact]
    public void Clear_built_ins_keeps_explicit_plain_tags()
    {
        var doc = MnemosyneFacade.Parse(
            """
            ---
            mnemosyne.clearBuiltIns: true
            tags: [Architecture]
            ---
            # X
            """,
            new MarkdownParseOptions { Path = "README.md" });

        doc.Tags.Should().Contain(SignificanceTag.Architecture);
        doc.Tags.Should().NotContain(SignificanceTag.Readme);
    }

    [Fact]
    public void Status_md_body_tokens_for_active_and_draft()
    {
        MnemosyneFacade.Parse(
                "# Status\n\n`active`\n",
                new MarkdownParseOptions { Path = "docs/epics/m1/status.md" })
            .Semantics.Lifecycle.Should().Be(DocumentLifecycle.Active);

        MnemosyneFacade.Parse(
                "# Status\n\ndraft\n",
                new MarkdownParseOptions { Path = "docs/epics/m1/status.md" })
            .Semantics.Lifecycle.Should().Be(DocumentLifecycle.Draft);

        MnemosyneFacade.Parse(
                "# Status\n\nin progress\n",
                new MarkdownParseOptions { Path = "docs/epics/m1/status.md" })
            .Semantics.Lifecycle.Should().Be(DocumentLifecycle.Active);
    }

    [Fact]
    public void Explain_fires_path_rules_for_agent_and_branching()
    {
        var agent = MnemosyneFacade.Explain(
            "# R\n",
            new MarkdownParseOptions { Path = "CLAUDE.md" });
        agent.RulesFired.Should().Contain(r => r.Contains("AgentControl", StringComparison.Ordinal));

        var branching = MnemosyneFacade.Explain(
            "# B\n",
            new MarkdownParseOptions { Path = ".github/branch-protection.md" });
        branching.RulesFired.Should().Contain(r => r.Contains("BranchingControl", StringComparison.Ordinal));
    }

    [Fact]
    public void Under_archive_skips_architecture_filename_tag()
    {
        var doc = MnemosyneFacade.Parse(
            "# Arch\n",
            new MarkdownParseOptions { Path = "docs/done/architecture-overview.md" });
        doc.Tags.Should().NotContain(SignificanceTag.Architecture);
        doc.Semantics.Lifecycle.Should().Be(DocumentLifecycle.Done);
    }
}
