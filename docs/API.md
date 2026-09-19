# API contract and limitations

## Support and compatibility

Mnemosyne targets .NET 8, .NET 9, and .NET 10 with no runtime NuGet dependencies. The CI matrix covers Windows, Linux, and macOS, and package consumers are smoke-tested on all three target frameworks. Native AOT, trimming, and browser/WASM are not verified.

During 0.x, patch releases preserve the public API. Minor releases may introduce breaking changes with changelog and migration notes. Documented bug fixes can change incorrect outputs in a patch release. Consumers should pin package versions and test their own document corpus when upgrading.

The facade exposes shared, get-only parser, resolver, and differ instances. Implementations keep per-call work in local state. Independent concurrent calls are supported; do not mutate options, input collections, or DTO contents during calls. `IReadOnlyList` does not guarantee deep immutability: treat returned collections as read-only and make your own copies when necessary.

Use non-null text, documents, indexes, and collection entries. Optional parse options and optional indexes may be null where their signatures allow it. The parser currently treats null text as empty, but callers should not rely on undocumented tolerance outside the nullable annotations. Invalid caller-created DTOs are not comprehensively validated and may throw standard .NET exceptions.

## Parsing

`MnemosyneFacade.Parse(text, options)` extracts document intelligence; it does not render Markdown or construct a complete CommonMark syntax tree.

| Surface | Current behavior |
| --------- | ------------------ |
| Headings | Column-zero ATX headings, levels 1-3. Levels 4-6 and setext headings are not returned. |
| Lines | One-based, including front matter; LF, CRLF, and CR are normalized. A heading ends immediately before the next returned heading, not at the end of a nested subtree. |
| Title | Front-matter title, then first H1, then first returned heading. |
| Blurb | First eligible body line or title, with basic inline markup stripping. Default limit is 240 characters plus a possible ellipsis; non-positive limits disable truncation. |
| Slugs | Lowercase ASCII letters/digits and collapsed separators; empty results become `section`. Repeated headings can have identical slugs; GitHub duplicate suffixes and Unicode slugs are not implemented. |
| Fences | Column-zero triple backtick or tilde prefixes toggle code-fence exclusion. Full nested, indented, and mixed-fence semantics are not implemented. |
| Links | Simple inline `[text](target)` links and basic `[[target]]`/anchor links in body lines. HTTP, HTTPS, and mailto targets are external. |

Reference-style links, autolinks, links in heading lines, escaped/nested link syntax, HTML semantics, and complete inline-code exclusion are not supported. Image syntax may be collected as a link. Link targets containing spaces, balanced parentheses, URL-encoded paths, query strings, or other URI schemes do not have complete URL semantics. Use a full Markdown parser if your application requires standards conformance.

## Front matter and classification

Front matter must start with `---` on the first line and have a closing delimiter. Empty front matter is allowed; unterminated front matter is treated as body text. This is an allowlisted line-oriented key/value reader, not a general YAML parser. Block scalars, nested objects, YAML aliases, and multiline lists are unsupported. Lists are inline comma-separated values, optionally enclosed in brackets.

Recognized keys are `title`, `status`, `tags`, `owner`, `commitSha`, `mnemosyne.tags`, `mnemosyne.clearBuiltIns`, `mnemosyne.planningRef`, `mnemosyne.pin`, and `mnemosyne.guidanceGroup`. Unknown keys are ignored. Duplicate keys use the last recognized value. Boolean true values are `true` or `1`.

`MarkdownConventionsOptions` controls path recognition, status aliases, and guidance options. `MnemosyneFacade.Explain` returns the parsed document and fired classification rules. Semantic values are deterministic heuristics derived from supplied text, paths, and conventions; they do not prove that a plan is active or that a named owner is authorized.

## Link resolution

`ResolveLinks(document, index)` returns missing-target or missing-anchor diagnostics; valid and external links produce no diagnostic. The caller must populate paths and, where needed, anchor sets for other documents. Missing anchor data can therefore produce a missing-anchor diagnostic even when the actual file has that heading.

Paths use slash normalization and case-insensitive comparisons on every operating system. Relative links use the document directory; `/path` uses the repository root. Dot segments are normalized lexically. Bare targets may match `.md`, `.mdc`, `.markdown`, or folder `README.md`/`index.md` candidates. Do not use indexes containing distinct files whose names differ only by case.

The resolver performs no I/O. Its normalization is not a path-security boundary and must not be used as filesystem authorization.

## Snapshot differences

`Diff(before, after, afterIndex, beforeIndex)` compares parsed documents. Supply unique normalized paths and retain before/after indexes when you need link-resolution-aware changes. The API compares extracted structure and semantics, not arbitrary body text; it is not a textual diff.

Moves are heuristic candidates based on document/heading identity. Duplicate titles/slugs and similar documents can be ambiguous. Consumers must not treat a candidate as authoritative Git rename history or a globally unique identity. Preserve input ordering for reproducible results; arbitrary ordering of equivalent snapshots is not a promised invariant.

## Untrusted inputs

Processing is synchronous without cancellation, streaming, or built-in input limits. Apply host-side size and concurrency limits. There are no performance guarantees or published benchmarks yet. See [SECURITY.md](../SECURITY.md) for the full trust boundary.
