---
name: upgrade-dependencies
description: Inventory or upgrade NuGet dependencies in relewise-sdk-csharp-extensions, preserve the published library's dependency ranges and target frameworks, validate builds, NUnit tests and the packed NuGet manifest, and prepare dependency-upgrade PRs when requested.
---

# Upgrade dependencies

Maintain `Relewise.Client.Extensions.sln` as a multi-targeted NuGet library. Read any applicable `AGENTS.md` and honor the user's package exclusions and delivery scope. An inventory or local upgrade does not require a Trello card or Git delivery.

## Repository compatibility contract

Inspect the project files at execution time; these are the current roles and baselines:

- `src/Relewise.Client.Extensions/Relewise.Client.Extensions.csproj` is the published library. It targets `netstandard2.0`, `net6.0`, `net7.0`, `net8.0`, `net9.0`, and `net10.0`.
- Its `Relewise.Client` dependency is `[1.320.0, 2)`. Version 1.320.0 is required for the OAuth client-credentials constructors used by the factories; do not lower this minimum. Microsoft dependencies are `Microsoft.Extensions.Configuration` `[7.0.0, 11)`, `Microsoft.Extensions.Configuration.Binder` `[7.0.4, 11)`, and `Microsoft.Extensions.DependencyInjection.Abstractions` `[7.0.0, 11)`.
- `src/Relewise.Client.Extensions.Tests/Relewise.Client.Extensions.Tests.csproj` is not packable, targets `net10.0`, and uses specific package versions. Its Microsoft.Extensions references exercise a newer dependency set than the library's minimums; NUnit, NUnit3TestAdapter, and Microsoft.NET.Test.Sdk provide the test toolchain.
- `.github/workflows/ci.yml` builds the library for every target framework in Debug and runs tests. `.github/workflows/publish.yml` builds, tests, and packs in Release using .NET 10. `.github/dependabot.yml` currently covers GitHub Actions only; it does not maintain NuGet references.

Preserve both ends of the published dependency ranges during routine maintenance. A lower bound is a consumer requirement, not a version to raise whenever an outdated report finds a newer package. The Microsoft ranges intentionally span multiple majors; do not normalize them into next-major ranges or pinned references.

MessagePack is a direct dependency with range `[2.5.307, 3)` to override the SDK's older transitive minimum with a patched v2 release. Keep the v2 major during routine maintenance; do not upgrade to v3 solely to clear the outdated report. Verify current security advisories before raising its minimum, and exercise SDK binary request/response compatibility when changing serializer versions.

Raise a lower bound only for a demonstrated API, compatibility, security, or explicitly requested support-policy requirement, and document the reason. Widen an upper bound only as an explicit compatibility change supported by validation. Report newer versions outside the allowed range as deferred. Do not import dependency exclusions from other repositories.

Keep specific versions in the test project specific. Update Microsoft.Extensions test packages together within the current target-framework major. Do not apply that major restriction to Microsoft.NET.Test.Sdk, NUnit, or NUnit3TestAdapter; select mutually compatible stable releases for those tools.

Do not change target frameworks, workflow SDK versions, the library's `PackageVersion`, or release automation as an incidental dependency update. Include GitHub Actions upgrades only when requested. There is no npm application or lockfile to regenerate.

## Inventory and version selection

1. Work from the repository root. Inspect project declarations, solution membership, package sources, workflows, and existing changes. Preserve unrelated work; resolve overlapping edits before modifying them. Use Git commands only within the user's authorized scope.
2. Record declared and resolved versions by project. Use the configured NuGet sources and current package metadata/release notes to choose compatible stable versions; use prereleases only when requested.
3. Run:

   ```powershell
   dotnet list Relewise.Client.Extensions.sln package --outdated --format json
   dotnet list Relewise.Client.Extensions.sln package --vulnerable --include-transitive
   ```

   The outdated report can show the global latest rather than the highest release inside an allowed range. Verify compatible candidates before selecting versions, and distinguish preserved library minimums from outdated test dependencies.
4. Report proposed updates, preserved ranges, deferred majors, and advisories. Do not add direct transitive-package overrides solely to empty an outdated report; require a concrete security or compatibility reason.

## Apply upgrades

- Upgrade eligible test dependencies and retain their existing XML formatting and specific-version convention.
- Preserve library ranges unless the compatibility decision above justifies a change. Avoid `dotnet add package --version` on ranged references when it would replace them with a specific version.
- Restore and inspect downgrade warnings, incompatible assets, and conflicts between the test project's specific versions and the library ranges.
- Fix API, configuration binding, dependency injection, or test-runner fallout directly caused by the upgrades. Preserve API-key and OAuth client-credentials authentication, named-client resolution, option precedence, timeouts, and server URL handling. Add regression coverage when an actual behavior change needs it.
- Request direction if resolution requires broad redesign or dropping supported consumers. Do not suppress diagnostics to make validation pass. The projects currently exempt NU1901–NU1904 from warnings-as-errors; those advisories still need reporting.

## Validation

Run these commands from the repository root, checking each exit code before continuing:

```powershell
dotnet restore Relewise.Client.Extensions.sln
dotnet build Relewise.Client.Extensions.sln --configuration Debug --no-restore
dotnet test Relewise.Client.Extensions.sln --configuration Debug --no-build --no-restore
dotnet build Relewise.Client.Extensions.sln --configuration Release --no-restore
dotnet test Relewise.Client.Extensions.sln --configuration Release --no-build --no-restore
dotnet pack src/Relewise.Client.Extensions/Relewise.Client.Extensions.csproj --configuration Release --no-build --no-restore --output ./artifacts/dependency-upgrade
dotnet list Relewise.Client.Extensions.sln package --outdated
dotnet list Relewise.Client.Extensions.sln package --vulnerable --include-transitive
```

An unrestricted solution build must compile every library target, including `netstandard2.0`; do not substitute a build of only the test framework. The NUnit tests execute on the test project's framework and do not establish runtime compatibility on every library target. Check that tests are discovered and executed, rather than accepting a successful command with zero tests.

Inspect the newly generated `.nupkg` as a ZIP archive and read `Relewise.Client.Extensions.nuspec`. Verify:

- Dependency groups and `lib/` assemblies cover every current library target framework.
- Each framework's dependency entries preserve the intended ranges, allowing equivalent NuGet normalization of version syntax.
- Dependencies have not accidentally become pinned, open-ended, or floating.
- The package still contains its README, license, and icon.

Test-project references can override the library's minimum dependencies. If a library range or API usage changes, also validate the affected minimum and proposed supported dependency versions using an isolated project copy or consumer harness. Compile against the minimums for all applicable targets and exercise configuration binding, DI registration, and named-client behavior on a runnable compatible framework. Keep temporary version overrides out of the delivered project files. Report exactly which combinations were exercised; do not infer whole-range compatibility from the net10.0 test suite alone.

Treat failed builds, tests, or pack checks as blocking for delivery unless the user explicitly accepts them. Report missing SDKs/runtimes or unavailable package feeds as incomplete validation, including the affected command. Keep generated reports and PR drafts outside source files; `artifacts/` is ignored.

## Delivery when requested

Do not commit, push, create a PR, publish a package, or merge merely because this skill was invoked. Follow the user's requested Git scope. When Git delivery is authorized:

1. Inspect status and diff, preserve unrelated changes, and reuse an appropriate task branch. When starting from `main`, update it with fast-forward only before creating a branch such as `chore/upgrade-dependencies-yyyyMM`; never overwrite an existing branch.
2. Commit only intended dependency and direct compatibility changes after validation, then push and create the requested PR type against the repository's verified base branch.
3. Put the task's Trello URL first in the PR description when one is available. Do not block local work on a missing Trello URL; request it only if the selected delivery workflow requires it.
4. For CLI delivery, write multiline PR text as UTF-8 to a temporary file outside the repository and pass it with `gh pr create --body-file`. Attach every created PR to the current chat using the available Codex artifact tool.

## Final report

Summarize updated packages by project, preserved ranges and justified range changes, deferred versions, compatibility fixes, and unresolved advisories. State the build, test, pack, manifest, and dependency-combination results with their limits. Include branch, Trello, push, and PR links only when delivery was performed.
