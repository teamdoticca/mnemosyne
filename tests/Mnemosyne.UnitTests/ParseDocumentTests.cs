using FluentAssertions;
using Mnemosyne;

namespace Mnemosyne.UnitTests;

public class ParseDocumentTests
{
    [Theory]
    [InlineData("")]
    [InlineData("---")]
    [InlineData("---\n")]
    [InlineData("---\n---")]
    [InlineData("---\n---\n")]
    [InlineData("---\ntitle: Unclosed")]
    public void Empty_or_incomplete_front_matter_does_not_throw(string markdown)
    {
        var document = MnemosyneFacade.Parse(markdown);

        document.FrontMatter.Title.Should().BeNull();
        document.Headings.Should().BeEmpty();
    }

    [Theory]
    [InlineData("\n")]
    [InlineData("\r\n")]
    [InlineData("\r")]
    public void Empty_front_matter_preserves_body_and_line_numbers(string newline)
    {
        var document = MnemosyneFacade.Parse(string.Join(newline, "---", "---", "# Title", "## Details"));

        document.Title.Should().Be("Title");
        document.Headings.Select(heading => heading.StartLine).Should().Equal(3, 4);
    }

    [Theory]
    [InlineData("```")]
    [InlineData("~~~")]
    public void Fenced_examples_do_not_emit_headings_or_links(string fence)
    {
        var document = MnemosyneFacade.Parse($"# Title\n{fence}\n## Example\n[example](missing.md)\n{fence}\n## Real");

        document.Headings.Select(heading => heading.Text).Should().Equal("Title", "Real");
        document.Links.Should().BeEmpty();
    }

    [Fact]
    public void Parses_h1_to_h3_with_slugs_and_ignores_h4()
    {
        var md = """
            # Title

            Intro paragraph for blurb.

            ## Section One
            ### Nested
            #### Too deep
            """;

        var doc = MnemosyneFacade.Parse(md, new MarkdownParseOptions { Path = "README.md" });

        doc.Title.Should().Be("Title");
        doc.Headings.Select(h => (h.Level, h.Slug)).Should().Equal(
            (1, "title"),
            (2, "section-one"),
            (3, "nested"));
        doc.Blurb.Should().StartWith("Intro paragraph");
        doc.Tags.Should().Contain(SignificanceTag.Readme);
    }

    [Fact]
    public void Blurb_skips_bold_status_preamble_lines()
    {
        var md = """
            # Desktop install (Velopack)

            **Status:** Active — pack script landed
            **Roadmap:** **#34**
            **Parent:** [../ROADMAP.md](../ROADMAP.md)

            User installs Mnemon via Velopack.
            """;

        var doc = MnemosyneFacade.Parse(
            md,
            new MarkdownParseOptions { Path = "docs/roadmap/desktop-install-velopack/README.md" });

        doc.Semantics.StatusRaw.Should().Contain("Active");
        doc.Blurb.Should().StartWith("User installs Mnemon");
        doc.Blurb.Should().NotContain("Status:");
    }

    [Fact]
    public void Front_matter_is_allowlisted_and_caps_blurb()
    {
        var md = """
            ---
            title: From Yaml
            status: draft
            tags: [alpha, beta]
            owner: fotis
            ignored: nope
            mnemosyne.tags: [Spec]
            ---
            # Ignored H1 for title when yaml title set

            """ + new string('x', 400);

        var doc = MnemosyneFacade.Parse(md, new MarkdownParseOptions { BlurbMaxLength = 240 });

        doc.Title.Should().Be("From Yaml");
        doc.FrontMatter.Status.Should().Be("draft");
        doc.FrontMatter.Owner.Should().Be("fotis");
        doc.FrontMatter.Tags.Should().Equal("alpha", "beta");
        doc.Blurb.Length.Should().BeLessThanOrEqualTo(241); // 240 + ellipsis possible
        doc.Tags.Should().Contain(SignificanceTag.Spec);
    }
}
