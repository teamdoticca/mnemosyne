# Mnemosyne

Portable **Markdown language intelligence** library for .NET 8, .NET 9, and .NET 10 consumers, including Mnemon. Package: `Doticca.Mnemosyne`.

## Boundary

| In | Out |
|  --- -  | -- --- |
| Markdown text + optional path | Structured DTOs |
| Optional path index (strings) | Link diagnostics |
| Two document snapshots | Evolution candidates (incl. Moved) |

**No** references to `Mnemon.*`, EF, LibGit2Sharp, ASP.NET, or filesystem I/O for resolve.

Parser implementation is **internal** and swappable behind `IMarkdownDocumentParser`.

## Public surface

- `IMarkdownDocumentParser` — parse headings, blurb, front-matter, tags, raw links; explain classification
- `IMarkdownLinkResolver` — resolve against `MarkdownPathIndex`
- `IMarkdownSnapshotDiffer` — diff two document sets
- `MnemosyneFacade` — convenience defaults

Default implementation classes are internal. The facade exposes get-only shared instances; consumers needing alternatives implement the interfaces in their own applications.

See the [API contract](../../docs/API.md), [contributor guide](../../CONTRIBUTING.md), and [MIT license](../../LICENSE). This package does not own persistence.
