#!/usr/bin/env bash
# Copies the cliwrap flake input into vendor/CliWrap for the Dummy program.
set -euo pipefail

dest=vendor/CliWrap
node='(builtins.fromJSON (builtins.readFile ./flake.lock)).nodes.cliwrap'
locked() { nix eval --raw --impure --expr "$node.original.ref"; }

# Relock only on a tag change: `nix flake lock` fails in the dev container, where the checkout has another owner.
want=$(sed -n 's|.*"github:Tyrrrz/CliWrap/\([^"]*\)".*|\1|p' flake.nix)
[ "$(locked)" = "$want" ] || nix flake lock

src=$(nix eval --raw --impure --expr "(builtins.fetchTree $node.locked).outPath")
rm -rf "$dest"
mkdir -p "$(dirname "$dest")"
# Copied, not linked: the Dummy program writes bin/ and obj/ beside its sources, and the store is read-only.
cp -R "$src" "$dest"
chmod -R u+w "$dest"
echo "$(locked)" > "$dest/.upstream-version"
