# Release checklist

The source tree prepares version `0.1.4`; version `0.1.3` is already published. Never overwrite an existing NuGet version or move a published tag.

## Setup status (2026-09-18)

- GitHub secret scanning, push protection, Dependabot alerts/security fixes, and private vulnerability reporting are enabled. The initial alert readback returned zero open secret alerts; this is not a complete historical audit.
- `main` requires PRs, resolved conversations, and the four checks listed below. Administrator bypass is retained for bootstrap/recovery; no second-person approval is required while there is one maintainer. Version tags have an active administrators-only creation/update/deletion ruleset.
- The `nuget` environment accepts only `v*` tags and requires approval from `@doticca`. Self-approval is allowed for this single-maintainer project.
- Still pending: merge the repository changes, observe successful hosted CI/CodeQL runs, and confirm the nuget.org trusted-publisher account configuration. The new required checks will not exist on older commits; do not require an older workflow to satisfy them.

## One-time repository setup

These are GitHub/nuget.org settings, not files that take effect on checkout. An administrator must confirm them before the first release under this workflow.

- Keep repository visibility public and Issues enabled.
- Enable private vulnerability reporting, Dependabot alerts/security updates, secret scanning, and push protection. Review historical secret-scanning alerts; enabling scanning is not proof the history is clean. Rotate exposed credentials before discussing remediation publicly.
- Protect `main` with branch protection or a ruleset requiring pull requests, resolved conversations, and the checks `Validate (ubuntu-latest)`, `Validate (windows-latest)`, `Validate (macos-latest)`, and `CodeQL`. Verify these names after the first workflow run. Block force pushes/deletions. With one maintainer, do not require an unavailable second reviewer; choose an explicit administrator bypass policy.
- Protect `v*` tags against unauthorized creation, update, or deletion. Restrict release creation to maintainers.
- Create the `nuget` environment with deployment limited to `v*` tags and a maintainer approval gate. Decide whether self-approval is allowed for the current single-maintainer project.
- Configure nuget.org trusted publishing for owner `teamdoticca`, repository `mnemosyne`, workflow `pack-nuget.yml`, and environment `nuget`. Confirm package ownership for `Doticca.Mnemosyne`. Store the NuGet account name in the GitHub secret `NUGET_USER`; no long-lived API key is needed.
- Use the checked-in CodeQL workflow (advanced setup), not a competing default-setup configuration. Review the first scan before requiring its check.
- Allow Actions to write GitHub Packages for this repository. Configure default token permissions as read-only and require approval for untrusted fork workflows.

## Preparing a release

1. Update `Version` in `src/Mnemosyne/Mnemosyne.csproj` using three-component SemVer. A patch preserves API compatibility; a breaking 0.x change needs a minor bump and migration notes. The current release candidate is `0.1.4`; `0.1.3` must not be republished.
2. Date the corresponding unreleased changelog section and document user-visible behavior changes.
3. Run `pwsh -File scripts/verify.ps1` from a clean checkout. Review coverage, API compatibility, license/source metadata, and the package smoke result.
4. Review dependency, CodeQL, and secret-scanning findings. Check examples against the new package.
5. Merge through a PR after all three OS validation jobs pass. This produces only a `-ci` GitHub Packages build, not a nuget.org release.
6. Create and push the matching tag, for example `v0.1.4`, on the reviewed `main` commit. The tag run validates it and publishes to GitHub Packages.
7. Open Actions > pack-nuget > Run workflow, select that tag, and set `publish` to true. Branch dispatches cannot publish to nuget.org. Approve the `nuget` environment deployment.
8. Verify nuget.org ingestion and symbol availability, the GitHub release and attached artifacts, and installation in a clean consumer. Check that repository commit metadata matches the tag.
9. For the next release, advance the source version and, after reviewing compatibility, update `PackageValidationBaselineVersion` to the release just published. The current baseline is `0.1.3`.

## Pipeline behavior and recovery

PR packages use `X.Y.Z-pr.RUN.ATTEMPT`; main builds use `X.Y.Z-ci.RUN.ATTEMPT`. Tags must match the project version exactly. Validation runs with read-only permissions on all three operating systems; only the Ubuntu package artifact is promoted. Publishing jobs never rebuild it.

NuGet publication uses a short-lived OIDC credential. The publishing job requires the tag commit to be reachable from `main` and runs behind the `nuget` environment. A successful dispatch creates a GitHub release with generated notes and attaches the package and symbols; edit those notes to include the reviewed changelog when necessary.

Reruns skip already-published NuGet versions and can finish a missing GitHub release. Before rerunning, compare the existing package's commit/version to the tag. Never retag or attempt to replace different bytes under an existing NuGet identity. For a defective release, publish a corrected higher version and deprecate the affected version on nuget.org with an explanation.
