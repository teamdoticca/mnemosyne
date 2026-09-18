# Contributing

Mnemosyne accepts bug reports, documentation improvements, and focused pull requests. Discuss new public APIs and behavior changes in an issue first. Participation follows our [code of conduct](CODE_OF_CONDUCT.md).

## Local setup

1. Fork and clone this repository.
2. Install the .NET 10 SDK and PowerShell 7. `global.json` allows stable .NET 10 feature-band updates.
3. Run `dotnet test Mnemosyne.sln --configuration Release`.
4. Run `pwsh -File scripts/verify.ps1` before submitting a pull request.

All dependencies restore from nuget.org. No Mnemon checkout, credentials, database, or native build is required. The package smoke project intentionally sits outside the solution: the verification script supplies its freshly built NuGet version and isolated package cache.

## Scope and quality

- Keep the engine pure: callers own I/O, persistence, Git, and application policy.
- Preserve the public surface and documented behavior in patch releases. Include migration guidance for deliberate breaking changes in a minor release.
- Add a minimal regression test in the nearest existing test class. Include Markdown text and expected DTO/diagnostic behavior, not private customer data.
- Use four-space indentation for C# and follow nearby style. Avoid unrelated reformatting.
- Keep source, tests, documentation, and the changelog in English.
- Document new public APIs with XML comments and update the API contract and examples.
- Do not lower coverage thresholds or suppress package compatibility failures just to make CI pass. Explain baseline updates in the PR.
- Keep dependencies small and license-compatible. Tests use FluentAssertions 6.12.2 under Apache-2.0; newer major versions require a fresh licensing decision, not an automatic upgrade.

The verification gate requires 80% line and 70% branch coverage. Build warnings and reported NuGet vulnerabilities fail the build. API/package validation compares against version `0.1.2.17`; behavior changes still need tests because binary compatibility cannot detect semantic regressions.

## Pull requests

Describe the problem, approach, tests, and compatibility impact. Link the issue when one exists. Use the PR checklist. The maintainer reviews contributions; submitting a PR does not guarantee acceptance or a release date. There is currently one code owner, so review is best-effort.

Contributions must be your own work or material you have permission to contribute under the repository's MIT license. Retain required third-party notices. Do not submit secrets or proprietary fixtures.

For security issues, use [SECURITY.md](SECURITY.md), not a public bug report. Release maintainers should follow [docs/RELEASING.md](docs/RELEASING.md).
