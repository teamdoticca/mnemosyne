using FluentAssertions;
using Mnemosyne;

namespace Mnemosyne.UnitTests;

public class SnapshotDiffTests
{
    [Fact]
    public void Prefers_HeadingMoved_over_removed_plus_added()
    {
        var before = new[]
        {
            MnemosyneFacade.Parse("# File\n## Auth Flow\n", new MarkdownParseOptions { Path = "docs/old.md" }),
            MnemosyneFacade.Parse("# Keep\n", new MarkdownParseOptions { Path = "docs/keep.md" }),
        };
        var after = new[]
        {
            MnemosyneFacade.Parse("# Keep\n", new MarkdownParseOptions { Path = "docs/keep.md" }),
            MnemosyneFacade.Parse("# File\n## Auth Flow\n", new MarkdownParseOptions { Path = "docs/guides/auth.md" }),
        };

        var diff = MnemosyneFacade.Diff(before, after);

        diff.Changes.Should().Contain(c =>
            c.Kind == SnapshotChangeKind.HeadingMoved &&
            c.HeadingSlug == "auth-flow" &&
            c.FromPath == "docs/old.md" &&
            c.ToPath == "docs/guides/auth.md");

        diff.Changes.Should().NotContain(c =>
            c.Kind == SnapshotChangeKind.HeadingRemoved && c.HeadingSlug == "auth-flow");
        diff.Changes.Should().NotContain(c =>
            c.Kind == SnapshotChangeKind.HeadingAdded && c.HeadingSlug == "auth-flow");
    }

    [Fact]
    public void Detects_rename_vs_delete_on_same_path()
    {
        var before = new[]
        {
            MnemosyneFacade.Parse("# T\n## Old Name\n", new MarkdownParseOptions { Path = "a.md" }),
        };
        var after = new[]
        {
            MnemosyneFacade.Parse("# T\n## New Name\n", new MarkdownParseOptions { Path = "a.md" }),
        };

        var diff = MnemosyneFacade.Diff(before, after);

        diff.Changes.Should().Contain(c =>
            c.Kind == SnapshotChangeKind.HeadingRenamed &&
            c.PreviousHeadingSlug == "old-name" &&
            c.HeadingSlug == "new-name");
        diff.Changes.Should().NotContain(c => c.Kind == SnapshotChangeKind.HeadingMoved);
    }

    [Fact]
    public void Detects_plain_delete_without_replacement()
    {
        var before = new[]
        {
            MnemosyneFacade.Parse("# T\n## Gone\n## Stay\n", new MarkdownParseOptions { Path = "a.md" }),
        };
        var after = new[]
        {
            MnemosyneFacade.Parse("# T\n## Stay\n", new MarkdownParseOptions { Path = "a.md" }),
        };

        var diff = MnemosyneFacade.Diff(before, after);

        diff.Changes.Should().Contain(c =>
            c.Kind == SnapshotChangeKind.HeadingRemoved && c.HeadingSlug == "gone");
        diff.Changes.Should().NotContain(c => c.Kind == SnapshotChangeKind.HeadingMoved);
    }

    [Fact]
    public void Detects_lifecycle_change_on_execution_plan()
    {
        const string path = "docs/roadmap/EXECUTION_PLAN.md";
        var before = MnemosyneFacade.Parse(
            """
            # Mnemon Execution Plan

            **Status:** Planned — ide-companion-extension

            ## Active

            | Item | Notes |
            |------|-------|
            | ide-companion | work |
            """,
            new MarkdownParseOptions { Path = path });
        var after = MnemosyneFacade.Parse(
            """
            # Mnemon Execution Plan

            **Status:** Active — ide-companion-extension

            ## Active

            | Item | Notes |
            |------|-------|
            | ide-companion | work |
            """,
            new MarkdownParseOptions { Path = path });

        var diff = MnemosyneFacade.Diff([before], [after]);

        diff.Changes.Should().Contain(c =>
            c.Kind == SnapshotChangeKind.LifecycleChanged &&
            c.Path == path &&
            c.PreviousSemanticValue == "Planned" &&
            c.SemanticValue == "Active");
    }

    [Fact]
    public void Detects_key_section_added_and_tags_changed()
    {
        const string path = "docs/roadmap/mnemosyne/README.md";
        var before = MnemosyneFacade.Parse(
            "# Epic\n",
            new MarkdownParseOptions { Path = path });
        var after = MnemosyneFacade.Parse(
            """
            ---
            mnemosyne:
              tags: [Roadmap]
            ---

            # Epic

            ## Next

            | Item | Notes |
            |------|-------|
            | phase-4 | evolution |
            """,
            new MarkdownParseOptions { Path = path });

        var diff = MnemosyneFacade.Diff([before], [after]);

        diff.Changes.Should().Contain(c =>
            c.Kind == SnapshotChangeKind.KeySectionAdded &&
            c.SectionName == "Next");
        diff.Changes.Should().Contain(c => c.Kind == SnapshotChangeKind.TagsChanged);
    }
}
