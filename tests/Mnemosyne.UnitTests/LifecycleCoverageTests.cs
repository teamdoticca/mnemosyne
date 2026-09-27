using FluentAssertions;
using Mnemosyne;

namespace Mnemosyne.UnitTests;

/// <summary>
/// Extra lifecycle / table / link-role coverage so the 70% branch gate has headroom after gate features.
/// </summary>
public class LifecycleCoverageTests
{
    [Theory]
    [InlineData("Blocked until DNS", DocumentLifecycle.Blocked)]
    [InlineData("deferred pending Graph", DocumentLifecycle.Deferred)]
    [InlineData("passed", DocumentLifecycle.Done)]
    [InlineData("complete — shipped", DocumentLifecycle.Done)]
    [InlineData("Draft notes", DocumentLifecycle.Draft)]
    [InlineData("Planned for Q3", DocumentLifecycle.Planned)]
    [InlineData("mystery-token", DocumentLifecycle.Unknown)]
    public void Status_header_heuristic_maps_extended_tokens(string status, DocumentLifecycle expected)
    {
        var doc = MnemosyneFacade.Parse(
            $"""
            # Note

            **Status:** {status}
            """,
            new MarkdownParseOptions { Path = "docs/epics/m99/evidence/note.md" });

        doc.Semantics.Lifecycle.Should().Be(expected);
    }

    [Theory]
    [InlineData("blocked", DocumentLifecycle.Blocked)]
    [InlineData("block", DocumentLifecycle.Blocked)]
    [InlineData("deferred", DocumentLifecycle.Deferred)]
    [InlineData("defer", DocumentLifecycle.Deferred)]
    [InlineData("pass", DocumentLifecycle.Done)]
    [InlineData("passed", DocumentLifecycle.Done)]
    public void Status_file_body_token_maps_gate_lifecycles(string token, DocumentLifecycle expected)
    {
        var doc = MnemosyneFacade.Parse(
            $"""
            # Status

            `{token}`
            """,
            new MarkdownParseOptions { Path = "docs/epics/m99/status.md" });

        doc.Semantics.Lifecycle.Should().Be(expected);
        doc.Semantics.StatusRaw.Should().Be(token);
    }

    [Fact]
    public void Table_all_deferred_aggregates_deferred()
    {
        var doc = MnemosyneFacade.Parse(
            """
            # Board

            | Epic | Status |
            |------|--------|
            | a | deferred |
            | b | defer |
            """,
            new MarkdownParseOptions { Path = "docs/roadmap.md" });

        doc.Semantics.Lifecycle.Should().Be(DocumentLifecycle.Deferred);
        doc.Semantics.StatusRaw.Should().Be("deferred");
    }

    [Fact]
    public void Table_all_draft_aggregates_draft()
    {
        var doc = MnemosyneFacade.Parse(
            """
            # Board

            | Epic | State |
            |------|--------|
            | a | draft |
            | b | draft |
            """,
            new MarkdownParseOptions { Path = "docs/roadmap.md" });

        doc.Semantics.Lifecycle.Should().Be(DocumentLifecycle.Draft);
    }

    [Fact]
    public void Table_all_planned_aggregates_planned()
    {
        var doc = MnemosyneFacade.Parse(
            """
            # Board

            | Epic | Status |
            |------|--------|
            | a | planned |
            | b | planned |
            """,
            new MarkdownParseOptions { Path = "docs/roadmap.md" });

        doc.Semantics.Lifecycle.Should().Be(DocumentLifecycle.Planned);
    }

    [Fact]
    public void Table_pass_cell_counts_as_done_via_alias()
    {
        var doc = MnemosyneFacade.Parse(
            """
            # Board

            | Epic | Status |
            |------|--------|
            | a | pass |
            | b | passed |
            """,
            new MarkdownParseOptions { Path = "docs/roadmap.md" });

        doc.Semantics.Lifecycle.Should().Be(DocumentLifecycle.Done);
        doc.Semantics.StatusRaw.Should().Be("done");
    }

    [Fact]
    public void Table_blocked_prefix_cell_maps_blocked()
    {
        var doc = MnemosyneFacade.Parse(
            """
            # Board

            | Epic | Status |
            |------|--------|
            | a | blocked — waiting on CIAM |
            | b | done |
            """,
            new MarkdownParseOptions { Path = "docs/roadmap.md" });

        doc.Semantics.Lifecycle.Should().Be(DocumentLifecycle.Blocked);
    }

    [Fact]
    public void Explain_fires_status_alias_rule_for_blocked()
    {
        var explanation = MnemosyneFacade.Explain(
            """
            # Gate

            **Status:** blocked
            """,
            new MarkdownParseOptions { Path = "docs/epics/m99/evidence/08.md" });

        explanation.Document.Semantics.Lifecycle.Should().Be(DocumentLifecycle.Blocked);
        explanation.RulesFired.Should().Contain(r => r.Contains("statusAlias", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Custom_alias_maps_hold_to_blocked()
    {
        var conventions = MarkdownConventionsOptions.CreateDefault();
        var aliases = new Dictionary<string, string>(conventions.StatusTokenAliases, StringComparer.OrdinalIgnoreCase)
        {
            ["hold"] = "Blocked",
        };

        var doc = MnemosyneFacade.Parse(
            """
            # Gate

            **Status:** hold
            """,
            new MarkdownParseOptions
            {
                Path = "docs/epics/m99/evidence/hold.md",
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
                    PathDocKindRules = conventions.PathDocKindRules,
                    StatusTableColumns = conventions.StatusTableColumns,
                    StatusTokenAliases = aliases,
                    KeySectionNames = conventions.KeySectionNames,
                    GuidancePins = conventions.GuidancePins,
                },
            });

        doc.Semantics.Lifecycle.Should().Be(DocumentLifecycle.Blocked);
    }

    [Theory]
    [InlineData("Superseded by ADR-002", LinkRole.Supersedes)]
    [InlineData("This replaces the old brief", LinkRole.Supersedes)]
    [InlineData("depends on foundation", LinkRole.DependsOn)]
    [InlineData("see also handover", LinkRole.SeeAlso)]
    public void Link_text_classifies_roles(string text, LinkRole expected)
    {
        var link = new MarkdownLink
        {
            Text = text,
            Target = "./other.md",
            Line = 1,
        };

        LinkRoleClassifier.Classify("docs/epics/m99/README.md", link, "docs/epics/m99/other.md")
            .Should()
            .Be(expected);
    }

    [Fact]
    public void Evidence_path_tags_as_spec_via_builtin_rule()
    {
        var doc = MnemosyneFacade.Parse(
            "# Evidence note\n",
            new MarkdownParseOptions { Path = "docs/epics/m99/evidence/01-check.md" });

        doc.Tags.Should().Contain(SignificanceTag.Spec);
    }
}
