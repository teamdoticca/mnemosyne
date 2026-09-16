using FluentAssertions;
using Mnemosyne;

namespace Mnemosyne.UnitTests;

public class MarkdownConventionsOptionsTests
{
    [Fact]
    public void Custom_archive_root_forces_done()
    {
        var custom = new MarkdownConventionsOptions
        {
            EpicRoots = ["notes/epics"],
            ArchiveRoots = ["notes/archive"],
            DocsHostFolders = ["notes"],
            PlanningHostFolders = ["plan"],
            RoadmapFileNames = ["roadmap.md"],
            ExecutionPlanFileNames = ["execution-plan.md"],
            StrategyFileNames = ["strategy.md"],
            ProgramIndexPaths = [new("notes/roadmap.md", "Roadmap")],
            PathTagRules = [],
            PathDocKindRules = [],
            StatusTableColumns = ["Status"],
            StatusTokenAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            KeySectionNames = ["Active"],
            GuidancePins = [new("Roadmap", "notes/roadmap.md", "planning", 0)],
            ShowActiveEpicsInGuidance = true,
            ShowDoneInGuidance = "count",
            PinExcludeGlobs = ["notes/archive/**"],
            NestedProgramPins = false,
        };

        var archived = MnemosyneFacade.Parse(
            "# Done epic\n",
            new MarkdownParseOptions
            {
                Path = "notes/archive/foo/README.md",
                Conventions = custom,
            });
        archived.Semantics.Lifecycle.Should().Be(DocumentLifecycle.Done);

        var epic = MnemosyneFacade.Parse(
            """
            # Open

            **Status:** in_progress
            """,
            new MarkdownParseOptions
            {
                Path = "notes/epics/foo/README.md",
                Conventions = custom,
            });
        epic.Semantics.DocKind.Should().Be(DocumentKind.Epic);
        epic.Semantics.Lifecycle.Should().Be(DocumentLifecycle.Active);
        epic.Semantics.PlanningRef.Should().Be("foo");
    }

    [Fact]
    public void Path_tag_rule_and_explain_report_rule()
    {
        var baseline = MarkdownConventionsOptions.CreateDefault();
        var conventions = new MarkdownConventionsOptions
        {
            EpicRoots = baseline.EpicRoots,
            ArchiveRoots = baseline.ArchiveRoots,
            DocsHostFolders = baseline.DocsHostFolders,
            PlanningHostFolders = baseline.PlanningHostFolders,
            RoadmapFileNames = baseline.RoadmapFileNames,
            ExecutionPlanFileNames = baseline.ExecutionPlanFileNames,
            StrategyFileNames = baseline.StrategyFileNames,
            ProgramIndexPaths = baseline.ProgramIndexPaths,
            PathTagRules = [new("docs/guides/**", "Spec")],
            PathDocKindRules = [],
            StatusTableColumns = baseline.StatusTableColumns,
            StatusTokenAliases = baseline.StatusTokenAliases,
            KeySectionNames = baseline.KeySectionNames,
            GuidancePins = baseline.GuidancePins,
            ShowActiveEpicsInGuidance = true,
            ShowDoneInGuidance = "count",
            PinExcludeGlobs = baseline.PinExcludeGlobs,
            NestedProgramPins = true,
        };

        var explanation = MnemosyneFacade.Explain(
            "# Guide\n",
            new MarkdownParseOptions
            {
                Path = "docs/guides/install.md",
                Conventions = conventions,
            });

        explanation.Document.Tags.Should().Contain(SignificanceTag.Spec);
        explanation.RulesFired.Should().Contain(r => r.Contains("pathTagRule", StringComparison.Ordinal));
    }

    [Fact]
    public void Program_index_path_override_tags_custom_file()
    {
        var baseline = MarkdownConventionsOptions.CreateDefault();
        var conventions = new MarkdownConventionsOptions
        {
            EpicRoots = baseline.EpicRoots,
            ArchiveRoots = baseline.ArchiveRoots,
            DocsHostFolders = baseline.DocsHostFolders,
            PlanningHostFolders = baseline.PlanningHostFolders,
            RoadmapFileNames = baseline.RoadmapFileNames,
            ExecutionPlanFileNames = baseline.ExecutionPlanFileNames,
            StrategyFileNames = baseline.StrategyFileNames,
            ProgramIndexPaths = [new("handbook/PLAN.md", "ExecutionPlan")],
            PathTagRules = [],
            PathDocKindRules = [],
            StatusTableColumns = ["Status"],
            StatusTokenAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            KeySectionNames = ["Active"],
            GuidancePins = [new("ExecutionPlan", "handbook/PLAN.md", "planning", 0)],
            ShowActiveEpicsInGuidance = false,
            ShowDoneInGuidance = "off",
            PinExcludeGlobs = [],
            NestedProgramPins = false,
        };

        var doc = MnemosyneFacade.Parse(
            "# Plan\n",
            new MarkdownParseOptions { Path = "handbook/PLAN.md", Conventions = conventions });

        doc.Tags.Should().Contain(SignificanceTag.ExecutionPlan);
        doc.Semantics.DocKind.Should().Be(DocumentKind.Index);
    }
}
