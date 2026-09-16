namespace Mnemosyne;

/// <summary>Built-in significance tags (multi-tag allowed).</summary>
public enum SignificanceTag
{
    AgentControl,
    Architecture,
    BranchingControl,
    Contributing,
    Security,
    Changelog,
    ExecutionPlan,
    Readme,
    Adr,
    Product,
    Roadmap,
    Runbook,
    Spec,
    Strategy,
    /// <summary>Git-backed commit annotation under docs/commit-notes (or FM commitSha).</summary>
    CommitNote,
    Other,
}

/// <summary>Why a markdown link failed to resolve against a path index.</summary>
public enum LinkDiagnosticKind
{
    MissingTarget,
    MissingAnchor,
}

/// <summary>Evolution candidate kinds from snapshot diff (no body text).</summary>
public enum SnapshotChangeKind
{
    DocumentAdded,
    DocumentRemoved,
    DocumentMoved,
    TitleChanged,
    HeadingAdded,
    HeadingRemoved,
    HeadingMoved,
    HeadingRenamed,
    HeadingLevelChanged,
    LinkAdded,
    LinkRemoved,
    LinkRoleChanged,
    BrokenLinkIntroduced,
    BrokenLinkCleared,
    DocKindChanged,
    LifecycleChanged,
    PlanningRefChanged,
    KeySectionAdded,
    KeySectionRemoved,
    KeySectionChanged,
    TagsChanged,
}
