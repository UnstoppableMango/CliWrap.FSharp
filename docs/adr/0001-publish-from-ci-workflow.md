# Publish from ci.yml, not a separate release workflow

NuGet.org trusted publishing is tied to the workflow file name in the package's policy, so the release-please job and the pack and publish jobs live in `.github/workflows/ci.yml` rather than a dedicated `release.yml`. Moving them would mean changing the trusted-publishing policy on nuget.org at the same moment, or publishing fails.

## Consequences

The release commit is checked out before its tag exists, so MinVer alone would compute a prerelease version. The pack job passes `MinVerVersionOverride` from release-please's `version` output. The `v*.*.*` tag trigger was removed: tags created by the release app start workflows, which would run CI and publishing a second time.
