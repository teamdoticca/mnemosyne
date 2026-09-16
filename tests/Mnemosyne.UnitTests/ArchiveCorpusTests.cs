using FluentAssertions;
using Mnemosyne;

namespace Mnemosyne.UnitTests;

public class ArchiveCorpusTests
{
    [Fact]
    public void Done_epic_readme_classifies_as_epic_with_done_lifecycle()
    {
        var doc = MnemosyneFacade.Parse(
            """
            # Code Memory (complete)

            **Completed:** 2026-08-29
            """,
            new MarkdownParseOptions { Path = "docs/done/code-memory/README.md" });

        doc.Semantics.DocKind.Should().Be(DocumentKind.Epic);
        doc.Semantics.PlanningRef.Should().Be("code-memory");
        doc.Semantics.Lifecycle.Should().Be(DocumentLifecycle.Done);
    }

    [Fact]
    public void Docs_hub_classifies_as_reference()
    {
        var doc = MnemosyneFacade.Parse(
            "# Mnemon documentation\n",
            new MarkdownParseOptions { Path = "docs/README.md" });

        doc.Semantics.DocKind.Should().Be(DocumentKind.Reference);
    }

    [Fact]
    public void Archive_move_pairs_roadmap_to_done_paths()
    {
        var before = MnemosyneFacade.Parse(
            "# Code Memory\n",
            new MarkdownParseOptions { Path = "docs/roadmap/code-memory/README.md" });
        var after = MnemosyneFacade.Parse(
            "# Code Memory (complete)\n",
            new MarkdownParseOptions { Path = "docs/done/code-memory/README.md" });

        var diff = MnemosyneFacade.Diff([before], [after]);

        diff.Changes.Should().ContainSingle(c =>
            c.Kind == SnapshotChangeKind.DocumentMoved &&
            c.FromPath == "docs/roadmap/code-memory/README.md" &&
            c.ToPath == "docs/done/code-memory/README.md");
        diff.Changes.Should().NotContain(c =>
            c.Kind == SnapshotChangeKind.DocumentAdded ||
            c.Kind == SnapshotChangeKind.DocumentRemoved);
    }
}
