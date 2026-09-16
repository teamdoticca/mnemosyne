using FluentAssertions;
using Mnemosyne;

namespace Mnemosyne.UnitTests;

public class DocumentSemanticsTests
{
    [Fact]
    public void Epic_readme_classifies_from_path()
    {
        var doc = MnemosyneFacade.Parse(
            """
            # Mnemosyne — program index

            **Status:** v1 **Done** · v2 program **Active**
            """,
            new MarkdownParseOptions { Path = "docs/roadmap/mnemosyne/README.md" });

        doc.Semantics.DocKind.Should().Be(DocumentKind.Epic);
        doc.Semantics.PlanningRef.Should().Be("mnemosyne");
        doc.Semantics.Lifecycle.Should().Be(DocumentLifecycle.Active);
        doc.Semantics.StatusRaw.Should().Contain("Active");
    }

    [Fact]
    public void Brief_classifies_from_numbered_path()
    {
        var doc = MnemosyneFacade.Parse(
            "# 06 — Planning tags\n",
            new MarkdownParseOptions
            {
                Path = "docs/roadmap/mnemosyne/06-planning-tags-and-front-matter.md",
            });

        doc.Semantics.DocKind.Should().Be(DocumentKind.Brief);
        doc.Semantics.PlanningRef.Should().Be("mnemosyne");
    }

    [Fact]
    public void Program_roadmap_is_index()
    {
        var doc = MnemosyneFacade.Parse(
            "# Mnemon Strategic Roadmap\n",
            new MarkdownParseOptions { Path = "docs/roadmap/ROADMAP.md" });

        doc.Semantics.DocKind.Should().Be(DocumentKind.Index);
    }

    [Fact]
    public void Nested_program_roadmap_is_index_with_planning_ref()
    {
        var doc = MnemosyneFacade.Parse(
            "# Mnemosyne v2 — program roadmap\n",
            new MarkdownParseOptions { Path = "docs/roadmap/mnemosyne/ROADMAP.md" });

        doc.Semantics.DocKind.Should().Be(DocumentKind.Index);
        doc.Semantics.PlanningRef.Should().Be("mnemosyne");
    }

    [Fact]
    public void Completed_program_roadmap_status_is_done_not_active()
    {
        var doc = MnemosyneFacade.Parse(
            """
            # Mnemosyne v2 — program roadmap

            **Status:** Done — Phases 1–8 ✓ · v2 program complete
            """,
            new MarkdownParseOptions { Path = "docs/roadmap/mnemosyne/ROADMAP.md" });

        doc.Semantics.Lifecycle.Should().Be(DocumentLifecycle.Done);
        doc.Semantics.StatusRaw.Should().Contain("Done");
    }

    [Fact]
    public void Execution_plan_detects_key_sections()
    {
        var md = """
            # Mnemon Execution Plan

            **Status:** Active — ide-companion-extension

            ## Active

            | Item | Notes |
            |------|-------|
            | ide-companion | work |

            ## Next

            | Item | Notes |
            |------|-------|
            | commit-parent-shas | eval |

            ## Completed (archived / living)

            See done/.
            """;

        var doc = MnemosyneFacade.Parse(
            md,
            new MarkdownParseOptions { Path = "docs/roadmap/EXECUTION_PLAN.md" });

        doc.Semantics.Lifecycle.Should().Be(DocumentLifecycle.Active);
        doc.Semantics.KeySections.Should().Contain(s =>
            s.Name == "Active" && s.StartLine > 0 && s.EndLine >= s.StartLine);
        doc.Semantics.KeySections.Should().Contain(s => s.Name == "Next");
        doc.Semantics.KeySections.Should().Contain(s =>
            s.Name == "Completed (archived / living)");
    }

    [Fact]
    public void Front_matter_status_maps_to_lifecycle()
    {
        var doc = MnemosyneFacade.Parse(
            """
            ---
            status: Planned
            owner: team
            ---
            # X
            """,
            new MarkdownParseOptions { Path = "docs/roadmap/foo/README.md" });

        doc.Semantics.Lifecycle.Should().Be(DocumentLifecycle.Planned);
        doc.Semantics.Owner.Should().Be("team");
        doc.Semantics.StatusRaw.Should().Be("Planned");
    }

    [Fact]
    public void Flat_docs_roadmap_is_program_index()
    {
        var doc = MnemosyneFacade.Parse(
            "# Argos roadmap\n",
            new MarkdownParseOptions { Path = "docs/roadmap.md" });

        doc.Semantics.DocKind.Should().Be(DocumentKind.Index);
        doc.Tags.Should().Contain(SignificanceTag.Roadmap);
    }

    [Fact]
    public void Roadmap_table_all_done_is_done()
    {
        var doc = MnemosyneFacade.Parse(
            """
            # Argos roadmap

            | Epic | Path | Status |
            |------|------|--------|
            | m00 Foundation | [done/m00-foundation](done/m00-foundation/) | done |
            | m01 Topology | [done/m01-topology-discovery](done/m01-topology-discovery/) | done |
            """,
            new MarkdownParseOptions { Path = "docs/roadmap.md" });

        doc.Semantics.DocKind.Should().Be(DocumentKind.Index);
        doc.Semantics.Lifecycle.Should().Be(DocumentLifecycle.Done);
        doc.Semantics.StatusRaw.Should().Be("done");
    }

    [Fact]
    public void Roadmap_table_with_open_epic_is_active()
    {
        var doc = MnemosyneFacade.Parse(
            """
            # Argos roadmap

            | Epic | Path | Status |
            |------|------|--------|
            | m00 Foundation | [done/m00-foundation](done/m00-foundation/) | done |
            | m15 Watchman | [epics/m15-watchman](epics/m15-watchman/) | in_progress |
            """,
            new MarkdownParseOptions { Path = "docs/roadmap.md" });

        doc.Semantics.Lifecycle.Should().Be(DocumentLifecycle.Active);
        doc.Semantics.StatusRaw.Should().Be("active");
    }

    [Fact]
    public void Explicit_status_header_wins_over_table_aggregate()
    {
        var doc = MnemosyneFacade.Parse(
            """
            # Roadmap

            **Status:** Active — shipping

            | Epic | Status |
            |------|--------|
            | a | done |
            | b | done |
            """,
            new MarkdownParseOptions { Path = "docs/roadmap.md" });

        doc.Semantics.Lifecycle.Should().Be(DocumentLifecycle.Active);
        doc.Semantics.StatusRaw.Should().Contain("Active");
    }

    [Fact]
    public void Flat_docs_execution_plan_is_program_index()
    {
        var doc = MnemosyneFacade.Parse(
            "# Argos execution plan\n",
            new MarkdownParseOptions { Path = "docs/execution-plan.md" });

        doc.Semantics.DocKind.Should().Be(DocumentKind.Index);
        doc.Tags.Should().Contain(SignificanceTag.ExecutionPlan);
    }

    [Fact]
    public void Epics_folder_readme_is_epic()
    {
        var doc = MnemosyneFacade.Parse(
            """
            # Foundation

            **Status:** in_progress
            """,
            new MarkdownParseOptions { Path = "docs/epics/m00-foundation/README.md" });

        doc.Semantics.DocKind.Should().Be(DocumentKind.Epic);
        doc.Semantics.PlanningRef.Should().Be("m00-foundation");
        doc.Semantics.Lifecycle.Should().Be(DocumentLifecycle.Active);
    }

    [Fact]
    public void Done_epic_design_is_brief()
    {
        var doc = MnemosyneFacade.Parse(
            "# Design — Foundation\n",
            new MarkdownParseOptions { Path = "docs/done/m00-foundation/design.md" });

        doc.Semantics.DocKind.Should().Be(DocumentKind.Brief);
        doc.Semantics.PlanningRef.Should().Be("m00-foundation");
        doc.Semantics.Lifecycle.Should().Be(DocumentLifecycle.Done);
    }

    [Fact]
    public void Epic_status_file_reads_body_token()
    {
        var doc = MnemosyneFacade.Parse(
            """
            # Status

            `done`
            """,
            new MarkdownParseOptions { Path = "docs/done/m00-foundation/status.md" });

        doc.Semantics.DocKind.Should().Be(DocumentKind.Brief);
        doc.Semantics.Lifecycle.Should().Be(DocumentLifecycle.Done);
        doc.Semantics.StatusRaw.Should().Be("done");
    }
}
