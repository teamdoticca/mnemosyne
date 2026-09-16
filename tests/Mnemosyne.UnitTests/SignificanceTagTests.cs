using FluentAssertions;
using Mnemosyne;

namespace Mnemosyne.UnitTests;

public class SignificanceTagTests
{
    [Theory]
    [InlineData(".cursor/rules/foo.mdc", SignificanceTag.AgentControl)]
    [InlineData(".vscode/rules/team.md", SignificanceTag.AgentControl)]
    [InlineData(".github/copilot-instructions.md", SignificanceTag.AgentControl)]
    [InlineData("AGENTS.md", SignificanceTag.AgentControl)]
    [InlineData(".cursor/skills/argos-architecture/SKILL.md", SignificanceTag.AgentControl)]
    [InlineData("BRANCHING.md", SignificanceTag.BranchingControl)]
    [InlineData("README.md", SignificanceTag.Readme)]
    [InlineData("docs/roadmap/ROADMAP.md", SignificanceTag.Roadmap)]
    [InlineData("docs/roadmap.md", SignificanceTag.Roadmap)]
    [InlineData("docs/roadmap/EXECUTION_PLAN.md", SignificanceTag.ExecutionPlan)]
    [InlineData("docs/execution-plan.md", SignificanceTag.ExecutionPlan)]
    [InlineData("docs/roadmap/COMPANION_STRATEGY.md", SignificanceTag.Strategy)]
    [InlineData("docs/ARCHITECTURE.md", SignificanceTag.Architecture)]
    [InlineData("docs/architecture.md", SignificanceTag.Architecture)]
    [InlineData("docs/PRODUCT.md", SignificanceTag.Product)]
    [InlineData("docs/runbooks/deploy.md", SignificanceTag.Runbook)]
    [InlineData("ops/incident-runbook.md", SignificanceTag.Runbook)]
    [InlineData("docs/PROJECT_HANDOVER.md", SignificanceTag.Runbook)]
    [InlineData("HANDOVER.md", SignificanceTag.Runbook)]
    [InlineData("docs/handover.md", SignificanceTag.Runbook)]
    [InlineData("ONBOARDING.md", SignificanceTag.Runbook)]
    [InlineData("docs/onboarding.md", SignificanceTag.Runbook)]
    [InlineData("docs/onboarding/engineers.md", SignificanceTag.Runbook)]
    [InlineData("docs/getting-started.md", SignificanceTag.Runbook)]
    [InlineData("GETTING_STARTED.md", SignificanceTag.Runbook)]
    [InlineData("getting_started.md", SignificanceTag.Runbook)]
    [InlineData("DEVELOPING.md", SignificanceTag.Runbook)]
    [InlineData("docs/development.md", SignificanceTag.Runbook)]
    [InlineData("docs/commit-notes/abcdef0123456789abcdef0123456789abcdef01.md", SignificanceTag.CommitNote)]
    [InlineData("documentation/commit-notes/abcdef0123456789abcdef0123456789abcdef01.md", SignificanceTag.CommitNote)]
    public void Path_heuristics(string path, SignificanceTag expected)
    {
        var doc = MnemosyneFacade.Parse("# Hi\n", new MarkdownParseOptions { Path = path });
        doc.Tags.Should().Contain(expected);
    }

    [Theory]
    [InlineData("docs/SETUP.md")]
    [InlineData("setup.md")]
    [InlineData("docs/install.md")]
    public void Setup_install_paths_are_not_Runbook_by_default(string path)
    {
        var doc = MnemosyneFacade.Parse("# Hi\n", new MarkdownParseOptions { Path = path });
        doc.Tags.Should().NotContain(SignificanceTag.Runbook);
    }

    [Fact]
    public void Root_readme_is_not_also_Runbook()
    {
        var doc = MnemosyneFacade.Parse("# Hi\n", new MarkdownParseOptions { Path = "README.md" });
        doc.Tags.Should().Contain(SignificanceTag.Readme);
        doc.Tags.Should().NotContain(SignificanceTag.Runbook);
    }

    [Theory]
    [InlineData("docs/roadmap/mnemosyne/ROADMAP.md")]
    [InlineData("docs/roadmap/api-connectivity-ux/README.md")]
    [InlineData("docs/roadmap/mnemosyne/06-planning-tags-and-front-matter.md")]
    [InlineData("docs/done/_program-phases-1-3/EXECUTION_PLAN.md")]
    [InlineData("docs/done/_program-phases-1-3/ROADMAP.md")]
    public void Epic_and_archived_paths_do_not_get_program_planning_tags(string path)
    {
        var doc = MnemosyneFacade.Parse("# Hi\n", new MarkdownParseOptions { Path = path });
        doc.Tags.Should().NotContain(SignificanceTag.Roadmap);
        doc.Tags.Should().NotContain(SignificanceTag.ExecutionPlan);
        doc.Tags.Should().NotContain(SignificanceTag.Strategy);
        doc.Tags.Should().NotContain(SignificanceTag.Readme);
    }

    [Fact]
    public void Front_matter_can_clear_built_ins()
    {
        var md = """
            ---
            mnemosyne.clearBuiltIns: true
            mnemosyne.tags: [Runbook]
            ---
            # X
            """;

        var doc = MnemosyneFacade.Parse(md, new MarkdownParseOptions { Path = "README.md" });
        doc.Tags.Should().Equal(SignificanceTag.Runbook);
    }

    [Fact]
    public void CommitSha_front_matter_tags_CommitNote_without_Mnemon_only_fm()
    {
        var md = """
            ---
            commitSha: abcdef0123456789abcdef0123456789abcdef01
            title: Why we capped seed
            ---
            # Note
            """;

        var doc = MnemosyneFacade.Parse(md, new MarkdownParseOptions { Path = "notes/why.md" });
        doc.FrontMatter.CommitSha.Should().Be("abcdef0123456789abcdef0123456789abcdef01");
        doc.Tags.Should().Contain(SignificanceTag.CommitNote);
    }

    [Fact]
    public void Commit_notes_path_without_front_matter_is_CommitNote()
    {
        var doc = MnemosyneFacade.Parse(
            "# Why\n",
            new MarkdownParseOptions
            {
                Path = "docs/commit-notes/abcdef0123456789abcdef0123456789abcdef01.md",
            });
        doc.Tags.Should().Contain(SignificanceTag.CommitNote);
        doc.Tags.Should().NotContain(SignificanceTag.Other);
    }
}
