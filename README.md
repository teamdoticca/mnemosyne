# Mnemosyne

**Mnemosyne turns the Markdown a repository already has into structured document intelligence.**

Use it when a tool needs to understand **what a Markdown document is**, **which planning or governance signals it carries**, **where its links lead**, and **how its document set evolved** without putting persistence, Git, or filesystem policy inside the engine.

Mnemosyne is a portable **Markdown language intelligence** library. You provide text, optional repository-relative paths, path indexes, and document snapshots; it returns deterministic DTOs, semantics, diagnostics, and evolution candidates.

```text
Markdown text + path -> parse -> document / headings / tags / semantics
Path index           -> resolve links -> missing targets / anchors
Before + after       -> diff -> added / removed / moved / changed documents
```

Typical consumers: repository explorers, planning indexes, agent tooling, IDE overlays, and applications such as [Mnemon](https://github.com/teamdoticca/Mnemon).

---

## Problems it solves

| Pain without Mnemosyne | With Mnemosyne |
|------------------------|----------------|
| Treating every Markdown file as an untyped blob | Document kind, lifecycle, planning reference, and key sections |
| Reimplementing path heuristics in every consumer | Built-in conventions for roadmaps, epics, briefs, policies, and references |
| Following links with filesystem access during indexing | Pure resolution against a caller-provided `MarkdownPathIndex` |
| Losing document meaning between revisions | Snapshot changes for headings, links, semantics, and moved documents |
| Coupling document intelligence to one application | Standalone library with no reference to Mnemon, Git, SQLite, ASP.NET, or UI |

---

## Add Mnemosyne to your .NET project

Package id: **`Doticca.Mnemosyne`** on [nuget.org](https://www.nuget.org/packages/Doticca.Mnemosyne).

```bash
dotnet add package Doticca.Mnemosyne
```

Or pin a version:

```xml
<PackageReference Include="Doticca.Mnemosyne" Version="0.1.0" />
```

The package targets **.NET 10** and contains managed code only. It has no native runtime assets, so the same package works across supported Windows, Linux, and macOS runtimes.

---

## Parse a Markdown document

```csharp
using Mnemosyne;

var markdown = """
	---
	status: Active
	owner: platform
	---
	# Workspace plan

	## Next

	Ship the package integration.
	""";

var document = MnemosyneFacade.Parse(
	markdown,
	new MarkdownParseOptions { Path = "docs/roadmap/workspace/README.md" });

Console.WriteLine(document.Title);
Console.WriteLine(document.Semantics.DocKind);    // Epic
Console.WriteLine(document.Semantics.Lifecycle);  // Active
Console.WriteLine(document.Semantics.Owner);      // platform
Console.WriteLine(document.Headings.Count);
```

The parser extracts the title, blurb, front matter, headings, links, significance tags, and deterministic document semantics. Parsing does not read from disk.

---

## Resolve links without filesystem I/O

```csharp
using Mnemosyne;

var document = MnemosyneFacade.Parse(
	"See [the guide](../guides/README.md#install).",
	new MarkdownParseOptions { Path = "docs/README.md" });

var index = new MarkdownPathIndex(
	["guides/README.md"],
	new Dictionary<string, IEnumerable<string>>
	{
		["guides/README.md"] = ["install"],
	});

var diagnostics = MnemosyneFacade.ResolveLinks(document, index);
foreach (var diagnostic in diagnostics)
{
	Console.WriteLine($"{diagnostic.Kind}: {diagnostic.Link.Target}");
}
```

`MarkdownPathIndex` normalizes separators and understands common Markdown conventions such as `.md` links and folder `README.md` / `index.md` targets. External links are not treated as repository targets.

---

## Diff document snapshots

```csharp
using Mnemosyne;

var before = MnemosyneFacade.Parse(
	"# Workspace plan\n",
	new MarkdownParseOptions { Path = "docs/roadmap/README.md" });
var after = MnemosyneFacade.Parse(
	"# Workspace plan\n\n## Next\n",
	new MarkdownParseOptions { Path = "docs/done/README.md" });

var diff = MnemosyneFacade.Diff([before], [after]);

foreach (var change in diff.Changes)
{
	Console.WriteLine($"{change.Kind}: {change.Path ?? change.ToPath}");
}
```

The snapshot differ reports document additions, removals, moves, heading changes, link changes, and semantic changes. It is pure and does not persist a ledger.

---

## Public API cheat sheet

| You want... | Call |
|-------------|------|
| Parse one document | `MnemosyneFacade.Parse(markdown, options)` |
| Explain classification | `MnemosyneFacade.Explain(markdown, options)` |
| Resolve repository links | `MnemosyneFacade.ResolveLinks(document, index)` |
| Compare snapshots | `MnemosyneFacade.Diff(before, after, ...)` |
| Configure parser behavior | `MarkdownConventionsOptions` |
| Use replaceable components | `IMarkdownDocumentParser`, `IMarkdownLinkResolver`, `IMarkdownSnapshotDiffer` |

The facade provides default implementations. Consumers can depend on the interfaces when they need to replace parsing, link resolution, or diff behavior.

---

## Classification contract

Mnemosyne exposes deterministic document semantics:

| Concept | Values |
|---------|--------|
| Document kind | `Unknown`, `Index`, `Epic`, `Brief`, `Policy`, `Reference` |
| Lifecycle | `Unknown`, `Active`, `Planned`, `Done`, `Draft` |
| Additional context | `PlanningRef`, `Owner`, `StatusRaw`, `KeySections` |
| Significance | `SignificanceTag` values inferred from paths, content, and optional front matter |

Front matter can provide explicit status, owner, tags, planning references, guidance pins, guidance groups, and commit-note identity. The allowlisted fields are parsed into typed DTOs; arbitrary front matter is not treated as business truth.

---

## Boundary and non-goals

Mnemosyne is not a database, Git client, filesystem crawler, Markdown editor, planner, LLM, dependency graph, or UI framework. It does not perform filesystem I/O for link resolution and does not own application persistence.

The engine is autonomous and has no reference back to Mnemon. Mnemon consumes it as a NuGet package and owns indexing, persistence, and product-specific workflows.

---

## Development

```bash
dotnet test Mnemosyne.sln
dotnet pack src/Mnemosyne/Mnemosyne.csproj --output artifacts/nuget
```

The GitHub Actions workflow validates the solution, packs the NuGet artifact, publishes dogfood builds to [GitHub Packages](https://github.com/orgs/teamdoticca/packages), and publishes explicit releases to [nuget.org](https://www.nuget.org/packages/Doticca.Mnemosyne).

The .NET assembly is not currently published to npm. An npm package would require a separate JavaScript or WebAssembly binding with a deliberate JavaScript API; publishing the DLL itself would not produce a usable npm library.

---

## Further reading

- [src/Mnemosyne/README.md](src/Mnemosyne/README.md) - package boundary and public surface
- [Mnemon](https://github.com/teamdoticca/Mnemon) - primary consumer

Package license: **MIT**.