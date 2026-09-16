namespace Mnemosyne;

/// <summary>Deterministic markdown recognition + Repo guidance presentation options.</summary>
public sealed class MarkdownConventionsOptions
{
    public IReadOnlyList<string> EpicRoots { get; init; } = [];

    public IReadOnlyList<string> ArchiveRoots { get; init; } = [];

    public IReadOnlyList<string> DocsHostFolders { get; init; } = [];

    public IReadOnlyList<string> PlanningHostFolders { get; init; } = [];

    public IReadOnlyList<string> RoadmapFileNames { get; init; } = [];

    public IReadOnlyList<string> ExecutionPlanFileNames { get; init; } = [];

    public IReadOnlyList<string> StrategyFileNames { get; init; } = [];

    /// <summary>Explicit repo-relative paths treated as program indexes (tag = Roadmap|ExecutionPlan|…).</summary>
    public IReadOnlyList<ProgramIndexPathRule> ProgramIndexPaths { get; init; } = [];

    public IReadOnlyList<PathTagRule> PathTagRules { get; init; } = [];

    public IReadOnlyList<PathDocKindRule> PathDocKindRules { get; init; } = [];

    public IReadOnlyList<string> StatusTableColumns { get; init; } = [];

    /// <summary>Lowercase status cell → lifecycle name (Active|Planned|Done|Draft).</summary>
    public IReadOnlyDictionary<string, string> StatusTokenAliases { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<string> KeySectionNames { get; init; } = [];

    public IReadOnlyList<GuidancePinRule> GuidancePins { get; init; } = [];

    public bool ShowActiveEpicsInGuidance { get; init; } = true;

    /// <summary><c>off</c> | <c>count</c> | <c>collapsed</c>.</summary>
    public string ShowDoneInGuidance { get; init; } = "count";

    public IReadOnlyList<string> PinExcludeGlobs { get; init; } = [];

    public bool NestedProgramPins { get; init; } = true;

    public static MarkdownConventionsOptions CreateDefault() =>
        new()
        {
            EpicRoots = ["docs/roadmap", "docs/epics", "docs/done"],
            ArchiveRoots = ["docs/done", "documentation/done"],
            DocsHostFolders = ["docs", "documentation", "doc"],
            PlanningHostFolders = ["roadmap", "planning", "plan", "plans"],
            RoadmapFileNames = ["roadmap.md", "road-map.md"],
            ExecutionPlanFileNames =
            [
                "execution_plan.md",
                "execution-plan.md",
                "executionplan.md",
            ],
            StrategyFileNames =
            [
                "companion_strategy.md",
                "companion-strategy.md",
                "strategy.md",
            ],
            ProgramIndexPaths =
            [
                new("docs/roadmap/ROADMAP.md", "Roadmap"),
                new("docs/roadmap.md", "Roadmap"),
                new("docs/roadmap/EXECUTION_PLAN.md", "ExecutionPlan"),
                new("docs/execution-plan.md", "ExecutionPlan"),
                new("docs/roadmap/COMPANION_STRATEGY.md", "Strategy"),
                new("docs/ARCHITECTURE.md", "Architecture"),
                new("docs/architecture.md", "Architecture"),
                new("docs/PRODUCT.md", "Product"),
                new("docs/product.md", "Product"),
            ],
            PathTagRules = [],
            PathDocKindRules = [],
            StatusTableColumns = ["Status", "State"],
            StatusTokenAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["shipped"] = "Done",
                ["complete"] = "Done",
                ["completed"] = "Done",
                ["wip"] = "Active",
                ["ip"] = "Active",
                ["blocked"] = "Active",
            },
            KeySectionNames =
            [
                "Active",
                "Next",
                "Exit criteria",
                "Sequencing",
                "Completed",
                "Completed (archived / living)",
            ],
            GuidancePins =
            [
                new("ExecutionPlan", "docs/roadmap/EXECUTION_PLAN.md", "planning", 0),
                new("ExecutionPlan", "docs/execution-plan.md", "planning", 1),
                new("Roadmap", "docs/roadmap/ROADMAP.md", "planning", 0),
                new("Roadmap", "docs/roadmap.md", "planning", 1),
                new("Strategy", "docs/roadmap/COMPANION_STRATEGY.md", "planning", 0),
                new("Strategy", "docs/strategy.md", "planning", 1),
                new("Architecture", "docs/ARCHITECTURE.md", "planning", 0),
                new("Architecture", "docs/architecture.md", "planning", 1),
                new("Product", "docs/PRODUCT.md", "planning", 0),
                new("AgentControl", "AGENTS.md", "governance", 0),
                new("BranchingControl", "BRANCHING.md", "governance", 0),
                new("Readme", "README.md", "governance", 0),
                // Onboarding / operate (Runbook) — wild path candidates; tag fallback finds any Runbook module.
                new("Runbook", "docs/PROJECT_HANDOVER.md", "onboarding", 0),
                new("Runbook", "PROJECT_HANDOVER.md", "onboarding", 1),
                new("Runbook", "docs/HANDOVER.md", "onboarding", 2),
                new("Runbook", "HANDOVER.md", "onboarding", 3),
                new("Runbook", "docs/ONBOARDING.md", "onboarding", 4),
                new("Runbook", "ONBOARDING.md", "onboarding", 5),
                new("Runbook", "docs/onboarding.md", "onboarding", 6),
                new("Runbook", "docs/getting-started.md", "onboarding", 7),
                new("Runbook", "GETTING_STARTED.md", "onboarding", 8),
                new("Runbook", "docs/getting_started.md", "onboarding", 9),
                new("Runbook", "DEVELOPING.md", "onboarding", 10),
                new("Runbook", "docs/development.md", "onboarding", 11),
                new("Runbook", "docs/runbooks/README.md", "onboarding", 12),
            ],
            ShowActiveEpicsInGuidance = true,
            ShowDoneInGuidance = "count",
            PinExcludeGlobs = ["docs/done/**", "documentation/done/**"],
            NestedProgramPins = true,
        };
}

public sealed record ProgramIndexPathRule(string Path, string Tag);

public sealed record PathTagRule(string Pattern, string Tag);

public sealed record PathDocKindRule(string Pattern, string DocKind);

public sealed record GuidancePinRule(string Tag, string Path, string Group, int Priority);

/// <summary>Classification result with which rules fired (agent explain tool).</summary>
public sealed class MarkdownClassificationExplanation
{
    public required MarkdownDocument Document { get; init; }

    public IReadOnlyList<string> RulesFired { get; init; } = [];
}
