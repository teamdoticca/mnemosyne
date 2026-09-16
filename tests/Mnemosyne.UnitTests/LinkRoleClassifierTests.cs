using FluentAssertions;
using Mnemosyne;

namespace Mnemosyne.UnitTests;

public class LinkRoleClassifierTests
{
    [Fact]
    public void Classifies_program_roadmap_to_epic_as_plans()
    {
        var link = new MarkdownLink
        {
            Text = "Mnemosyne engine",
            Target = "./mnemosyne/",
            Line = 44,
        };

        LinkRoleClassifier.Classify(
                "docs/roadmap/ROADMAP.md",
                link,
                "docs/roadmap/mnemosyne/README.md")
            .Should()
            .Be(LinkRole.Plans);
    }

    [Fact]
    public void Classifies_epic_readme_to_brief_as_plans()
    {
        var link = new MarkdownLink
        {
            Text = "12 — Typed doc links",
            Target = "./12-typed-doc-links.md",
            Line = 40,
        };

        LinkRoleClassifier.Classify(
                "docs/roadmap/mnemosyne/README.md",
                link,
                "docs/roadmap/mnemosyne/12-typed-doc-links.md")
            .Should()
            .Be(LinkRole.Plans);
    }

    [Fact]
    public void Classifies_brief_to_epic_readme_as_parent()
    {
        var link = new MarkdownLink
        {
            Text = "mnemosyne",
            Target = "./README.md",
            Line = 4,
        };

        LinkRoleClassifier.Classify(
                "docs/roadmap/mnemosyne/12-typed-doc-links.md",
                link,
                "docs/roadmap/mnemosyne/README.md")
            .Should()
            .Be(LinkRole.Parent);
    }

    [Fact]
    public void Classifies_epic_to_program_roadmap_as_parent()
    {
        var link = new MarkdownLink
        {
            Text = "ROADMAP",
            Target = "../ROADMAP.md",
            Line = 4,
        };

        LinkRoleClassifier.Classify(
                "docs/roadmap/mnemosyne/README.md",
                link,
                "docs/roadmap/ROADMAP.md")
            .Should()
            .Be(LinkRole.Parent);
    }

    [Fact]
    public void Classifies_depends_on_text_as_depends()
    {
        var link = new MarkdownLink
        {
            Text = "Depends on semantics index",
            Target = "./08-semantics-index-and-api.md",
            Line = 44,
        };

        LinkRoleClassifier.Classify(
                "docs/roadmap/mnemosyne/12-typed-doc-links.md",
                link,
                "docs/roadmap/mnemosyne/08-semantics-index-and-api.md")
            .Should()
            .Be(LinkRole.DependsOn);
    }

    [Fact]
    public void Classifies_outside_roadmap_as_generic()
    {
        var link = new MarkdownLink
        {
            Text = "Product",
            Target = "../PRODUCT.md",
            Line = 6,
        };

        LinkRoleClassifier.Classify(
                "docs/roadmap/ROADMAP.md",
                link,
                "docs/PRODUCT.md")
            .Should()
            .Be(LinkRole.Generic);
    }
}
