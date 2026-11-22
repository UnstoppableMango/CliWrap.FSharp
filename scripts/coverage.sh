#!/usr/bin/env bash
# Prints line and branch coverage from a Cobertura report, and fails when line coverage is below COVERAGE_MIN.
# Branch coverage is reported but not gated: F# pattern matches and computation expressions compile to extra branches.
set -euo pipefail

min=${COVERAGE_MIN:-80}
if [ "$#" -ne 1 ] || [ ! -f "$1" ]; then
	echo "usage: $0 REPORT (expected exactly one Cobertura report, got: $*)" >&2
	exit 2
fi

root=$(grep -m1 -o '<coverage [^>]*>' "$1")
percent() { sed -n "s/.* $1=\"\([0-9.]*\)\".*/\1/p" <<<"$root" | awk '{ printf "%.2f", $1 * 100 }'; }
line=$(percent line-rate)
branch=$(percent branch-rate)

summary="Line coverage: ${line}% (minimum ${min}%)
Branch coverage: ${branch}% (not gated)"
echo "$summary"
if [ -n "${GITHUB_STEP_SUMMARY:-}" ]; then
	printf '## Coverage\n\n%s\n' "$(sed 's/^/- /' <<<"$summary")" >>"$GITHUB_STEP_SUMMARY"
fi

if ! awk -v line="$line" -v min="$min" 'BEGIN { exit !(line >= min) }'; then
	echo "::error::Line coverage ${line}% is below the ${min}% minimum"
	exit 1
fi
