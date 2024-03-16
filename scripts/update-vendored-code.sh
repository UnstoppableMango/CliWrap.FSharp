#!/bin/bash
set -eum

root="$(git rev-parse --show-toplevel)"
ln -s "$root/vendor/CliWrap/CliWrap.Tests.Dummy" "$root/src"
