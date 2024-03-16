#!/bin/bash
set -eum

root="$(git rev-parse --show-toplevel)"
patch="$root/patches/0001-CliWrap.Tests.Dummy.csproj.patch"
vendorDir="$root/vendor/CliWrap"
targetDir="$root/src/CliWrap.Tests.Dummy"

git submodule foreach --recursive git clean -xfd
rm -rf "$targetDir" || echo 'Target is clean'
cp -r "$vendorDir/CliWrap.Tests.Dummy" "$targetDir"
patch "$targetDir/CliWrap.Tests.Dummy.csproj" <"$patch"
