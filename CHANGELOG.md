# Changelog

## 0.1.5 - Unreleased

### Fixed

- Incomplete or empty front-matter delimiters no longer throw during parsing.
- Root-relative links resolve from the repository root rather than the current document directory.

### Added

- MIT license file and NuGet license metadata; symbols packages and source commit metadata.
- Public API compatibility validation against the published `0.1.2.17` package.
- Cross-platform CI, coverage gates, and an isolated package consumer smoke test.
- Security/dependency automation, contribution guidance, issue templates, and a tagged release process.
- Regression tests for line endings, malformed input, path resolution, fenced examples, and concurrent facade calls.

### Changed

- Main builds use SemVer prerelease identifiers instead of four-component release-looking versions.
- NuGet publication requires an explicit dispatch on a version-matching tag.
- Test assertions use the Apache-2.0-licensed FluentAssertions 6 line.
- .NET 10 support, Markdown subset limitations, and 0.x compatibility expectations are explicit.
- .NET 8, .NET 9, and .NET 10 package targets are verified by build and consumer smoke tests.

## 0.1.4

- Multi-target package preparation was merged, but the release publish was blocked by a missing GitHub Actions OIDC permission and the immutable `v0.1.4` tag remains unpublished.

The public API remains compatible with `0.1.2.17`. The two parser/resolver fixes above intentionally correct previously erroneous results.

## Initial public packages

The initial standalone library was extracted and packaged in September 2026. It provides parsing, classification explanations, link diagnostics, and snapshot differences. Early CI packages used four-component versions, including `0.1.2.17`, without corresponding GitHub release tags. See [NuGet history](https://www.nuget.org/packages/Doticca.Mnemosyne#versions-body-tab) for the authoritative published versions.
