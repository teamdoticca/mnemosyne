# Mnemosyne

Portable **markdown language intelligence** library for Mnemon (and future NuGet consumers).

## Boundary

| In | Out |
|----|-----|
| Markdown text + optional path | Structured DTOs |
| Optional path index (strings) | Link diagnostics |
| Two document snapshots | Evolution candidates (incl. Moved) |

**No** references to `Mnemon.*`, EF, LibGit2Sharp, ASP.NET, or filesystem I/O for resolve.

Parser implementation is **internal** and swappable behind `IMarkdownDocumentParser`.

## Public surface

- `IMarkdownDocumentParser` / `MarkdownDocumentParser` — parse headings, blurb, front-matter, tags, raw links
- `IMarkdownLinkResolver` / `MarkdownLinkResolver` — resolve vs `MarkdownPathIndex`
- `IMarkdownSnapshotDiffer` / `MarkdownSnapshotDiffer` — diff two document sets
- `MnemosyneFacade` — convenience defaults

Mnemon language id stays `markdown`; this package does not own persistence.
