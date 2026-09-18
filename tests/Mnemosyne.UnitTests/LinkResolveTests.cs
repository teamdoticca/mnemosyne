using FluentAssertions;
using Mnemosyne;

namespace Mnemosyne.UnitTests;

public class LinkResolveTests
{
    [Theory]
    [InlineData("/README.md", "README.md")]
    [InlineData("/guides/../README.md", "README.md")]
    [InlineData("../README.md", "README.md")]
    [InlineData("./guide.md", "docs/guide.md")]
    [InlineData("guide.md", "docs/guide.md")]
    [InlineData(".\\guide.md", "docs/guide.md")]
    [InlineData("../guides/./install.md", "guides/install.md")]
    public void Resolves_root_relative_and_dot_segment_paths(string target, string indexedPath)
    {
        var document = MnemosyneFacade.Parse(
            $"# Document\n[guide]({target}#install)",
            new MarkdownParseOptions { Path = "docs/README.md" });
        var index = new MarkdownPathIndex(
            [indexedPath],
            new Dictionary<string, IEnumerable<string>> { [indexedPath] = ["install"] });

        MnemosyneFacade.ResolveLinks(document, index).Should().BeEmpty();
    }

    [Fact]
    public void Classifies_missing_target_and_anchor_skips_external()
    {
        var md = """
            # Doc
            See [ok](./other.md#section-one) and [gone](./missing.md) and [bad](./other.md#nope).
            Also [web](https://example.com).
            """;

        var doc = MnemosyneFacade.Parse(md, new MarkdownParseOptions { Path = "docs/a.md" });
        var other = MnemosyneFacade.Parse(
            "# Other\n## Section One\n",
            new MarkdownParseOptions { Path = "docs/other.md" });

        var index = new MarkdownPathIndex(
            ["docs/a.md", "docs/other.md"],
            new Dictionary<string, IEnumerable<string>>
            {
                ["docs/other.md"] = other.Headings.Select(h => h.Slug),
            });

        var diags = MnemosyneFacade.ResolveLinks(doc, index);

        diags.Should().Contain(d =>
            d.Kind == LinkDiagnosticKind.MissingTarget && d.Link.Target.Contains("missing"));
        diags.Should().Contain(d =>
            d.Kind == LinkDiagnosticKind.MissingAnchor && d.Link.Anchor == "nope");
        diags.Should().NotContain(d => d.Link.IsExternal);
        diags.Should().NotContain(d =>
            d.Link.Target.Contains("other.md") && d.Link.Anchor == "section-one");
    }

    [Fact]
    public void Resolves_relative_folder_links_to_readme()
    {
        var md = """
            # MCP
            See [parent shas](../commit-parent-shas/) and [bare](../local-daemon-discovery).
            Also [missing folder](../does-not-exist/).
            """;

        var doc = MnemosyneFacade.Parse(
            md,
            new MarkdownParseOptions { Path = "docs/roadmap/mcp-agent-surface/README.md" });

        var index = new MarkdownPathIndex(
        [
            "docs/roadmap/mcp-agent-surface/README.md",
            "docs/roadmap/commit-parent-shas/README.md",
            "docs/roadmap/local-daemon-discovery/README.md",
        ]);

        var diags = MnemosyneFacade.ResolveLinks(doc, index);

        diags.Should().NotContain(d =>
            d.Kind == LinkDiagnosticKind.MissingTarget &&
            d.Link.Target.Contains("commit-parent-shas"));
        diags.Should().NotContain(d =>
            d.Kind == LinkDiagnosticKind.MissingTarget &&
            d.Link.Target.Contains("local-daemon-discovery"));
        diags.Should().ContainSingle(d =>
            d.Kind == LinkDiagnosticKind.MissingTarget &&
            d.Link.Target.Contains("does-not-exist"));
    }

    [Fact]
    public void ExpandLinkPathCandidates_includes_readme_and_md_suffix()
    {
        MarkdownPathIndex.ExpandLinkPathCandidates("docs/roadmap/foo/")
            .Should()
            .Contain(["docs/roadmap/foo", "docs/roadmap/foo.md", "docs/roadmap/foo/README.md"]);

        MarkdownPathIndex.ExpandLinkPathCandidates("docs/a.md")
            .Should()
            .Equal("docs/a.md");
    }
}
