namespace Mnemosyne;

/// <summary>Parses markdown text into a structured <see cref="MarkdownDocument"/>.</summary>
public interface IMarkdownDocumentParser
{
    MarkdownDocument Parse(string markdown, MarkdownParseOptions? options = null);

    MarkdownClassificationExplanation Explain(string markdown, MarkdownParseOptions? options = null);
}

/// <summary>Classifies links against a path index (pure; no I/O).</summary>
public interface IMarkdownLinkResolver
{
    IReadOnlyList<MarkdownLinkDiagnostic> Resolve(MarkdownDocument document, MarkdownPathIndex index);
}

/// <summary>Diffs two document snapshots into evolution candidates.</summary>
public interface IMarkdownSnapshotDiffer
{
    MarkdownSnapshotDiff Diff(
        IReadOnlyList<MarkdownDocument> before,
        IReadOnlyList<MarkdownDocument> after,
        MarkdownPathIndex? afterIndex = null,
        MarkdownPathIndex? beforeIndex = null);
}

/// <summary>Default entry points for consumers.</summary>
public static class MnemosyneFacade
{
    public static IMarkdownDocumentParser Parser { get; } = new Internal.MarkdownDocumentParser();

    public static IMarkdownLinkResolver LinkResolver { get; } = new Internal.MarkdownLinkResolver();

    public static IMarkdownSnapshotDiffer SnapshotDiffer { get; } = new Internal.MarkdownSnapshotDiffer();

    public static MarkdownDocument Parse(string markdown, MarkdownParseOptions? options = null) =>
        Parser.Parse(markdown, options);

    public static MarkdownClassificationExplanation Explain(
        string markdown,
        MarkdownParseOptions? options = null) =>
        Parser.Explain(markdown, options);

    public static IReadOnlyList<MarkdownLinkDiagnostic> ResolveLinks(
        MarkdownDocument document,
        MarkdownPathIndex index) =>
        LinkResolver.Resolve(document, index);

    public static MarkdownSnapshotDiff Diff(
        IReadOnlyList<MarkdownDocument> before,
        IReadOnlyList<MarkdownDocument> after,
        MarkdownPathIndex? afterIndex = null,
        MarkdownPathIndex? beforeIndex = null) =>
        SnapshotDiffer.Diff(before, after, afterIndex, beforeIndex);
}
