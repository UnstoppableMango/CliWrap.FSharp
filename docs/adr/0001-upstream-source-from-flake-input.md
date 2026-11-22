# Upstream source comes from a flake input pinned to the wrapped release

Conformance specs and equivalence checks run upstream's Dummy program. It has to come from the same upstream release as the CliWrap NuGet package the library wraps. Otherwise the tests check the wrapper against behavior it doesn't ship with. That is what happened when a git submodule tracked upstream's `prime` branch while the package stayed on a release tag. We now get the upstream source from a non-flake input (`github:Tyrrrz/CliWrap/<tag>`, `flake = false`) pinned to that release tag. A make target copies it into a gitignored `vendor/CliWrap`, and the test project references upstream's `CliWrap.Tests.Dummy.csproj` there directly. Nothing from upstream is checked in, copied by hand, or patched.

## Considered Options

- **Git submodule** (previous approach): tracked a branch, drifted from the package version, and needed a hand-run copy and patch step.
- **MSBuild download of the tag matching the PackageReference**: needs no nix and has a single version pin, but it adds a network fetch to builds and needs a second, hashed fetch for sandboxed nix checks.

## Consequences

- Running tests needs nix, both locally and in the CI test job.
- The input must be copied, not symlinked, because the store path is read-only and the Dummy program builds `bin/` and `obj/` next to its sources.
- There are two version pins, the flake input tag and the `CliWrap` PackageReference. A Renovate group bumps them together, and a CI check fails when they differ.
- `CSharpier_Bypass=true` is set on the Dummy program's ProjectReference so upstream's formatter never runs in this repo's builds.
