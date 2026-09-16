namespace Mnemosyne;

public enum DocumentKind
{
    Unknown = 0,
    Index = 1,
    Epic = 2,
    Brief = 3,
    Policy = 4,
    Reference = 5,
}

public enum DocumentLifecycle
{
    Unknown = 0,
    Active = 1,
    Planned = 2,
    Done = 3,
    Draft = 4,
}

public sealed class DocumentKeySection
{
    public required string Name { get; init; }
    public required int StartLine { get; init; }
    public required int EndLine { get; init; }
}

/// <summary>Deterministic planning / corpus classification for a parsed markdown document.</summary>
public sealed class DocumentSemantics
{
    public DocumentKind DocKind { get; init; } = DocumentKind.Unknown;
    public DocumentLifecycle Lifecycle { get; init; } = DocumentLifecycle.Unknown;
    public string? PlanningRef { get; init; }
    public IReadOnlyList<DocumentKeySection> KeySections { get; init; } = [];
    public string? Owner { get; init; }
    public string? StatusRaw { get; init; }
}
