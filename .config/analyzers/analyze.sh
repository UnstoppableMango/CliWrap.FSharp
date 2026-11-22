#!/usr/bin/env bash
set -euo pipefail

root="$(git rev-parse --show-toplevel)"
cd "$root"

DOTNET="${DOTNET:-dotnet}"
proj=".config/analyzers/Analyzers.csproj"

"$DOTNET" tool restore >/dev/null
"$DOTNET" restore --locked-mode "$proj" >/dev/null

analyzer_path() {
	echo "$("$DOTNET" msbuild "$proj" -getProperty:"Pkg$1")/analyzers/dotnet/fs"
}

# The net10.0 tool loads the SDK's MSBuild, so it needs the newest runtime.
# Keep rollForward false in dotnet-tools.json, or it overrides this.
DOTNET_ROLL_FORWARD=LatestMajor DOTNET_ROLL_FORWARD_TO_PRERELEASE=1 "$DOTNET" fsharp-analyzers \
	--project src/CliWrap.FSharp/CliWrap.FSharp.fsproj \
	--project src/CliWrap.FSharp.Tests/CliWrap.FSharp.Tests.fsproj \
	--analyzers-path "$(analyzer_path G-Research_FSharp_Analyzers)" \
	--analyzers-path "$(analyzer_path Ionide_Analyzers)" \
	--configuration Release \
	--exclude-files "**/obj/**" "**/.nuget/**" \
	--treat-as-error "*" \
	--code-root . \
	"$@"
