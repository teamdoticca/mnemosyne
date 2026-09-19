# Security policy

## Supported versions

Security fixes are delivered in the latest published 0.x release. Older releases and CI prereleases do not receive separate backports. Upgrade to the latest release before reporting an issue when practical. Support is best-effort without a response-time guarantee.

## Reporting a vulnerability

Use [GitHub private vulnerability reporting](https://github.com/teamdoticca/mnemosyne/security/advisories/new). Include the package version, .NET version, a minimal sanitized reproduction, expected impact, and any mitigation you know. Never post credentials, customer documents, or exploit details in public issues.

If the private reporting form is unavailable, open an issue asking the maintainer for a private reporting channel without including vulnerability details. Coordinate disclosure with the maintainer while a report is being investigated.

## Trust boundary

Mnemosyne parses caller-supplied text and resolves links against caller-supplied strings. It does not fetch URLs, execute Markdown, or access files. Classification, guidance pins, owners, and commit identities are document claims, not authenticated instructions or authorization decisions.

The parser is synchronous and has no cancellation or built-in input-size limit. Hosts accepting untrusted documents must bound document size, document counts, and concurrency, and isolate processing when a hard execution budget is required. No denial-of-service resistance or performance SLA is claimed.

Path normalization is lexical and case-insensitive; it is not a filesystem sandbox. Do not pass a resolved target into filesystem or network APIs without your own authorization, traversal protection, and scheme validation. Parsed content is not sanitized HTML.

## Repository protections

CI uses read-only permissions for validation and separate publishing jobs. NuGet publishing uses short-lived trusted-publishing credentials, not a checked-in API key. The assembly is public strong-name signed for consumers that require a strong-named dependency; this is separate from optional NuGet package author signing. Dependency updates and CodeQL are configured in `.github`; repository administrators must keep secret scanning, push protection, and private reporting enabled.
