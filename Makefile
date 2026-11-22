DOTNET   ?= dotnet
FANTOMAS ?= $(DOTNET) fantomas
NPX      ?= npx
NIX      ?= nix

UPSTREAM_DIR    := vendor/CliWrap
UPSTREAM_STAMP  := $(UPSTREAM_DIR)/.upstream-version
CLIWRAP_FSPROJ  := src/CliWrap.FSharp/CliWrap.FSharp.fsproj
FLAKE_LOCK_NODE := (builtins.fromJSON (builtins.readFile ./flake.lock)).nodes.cliwrap

.PHONY: build check test format fmt deps trimmable aot update vendor devcontainer

build: vendor
	$(DOTNET) build

check:
	nix flake check

test: vendor
	$(DOTNET) test

format fmt:
	nix fmt

deps:
	$(shell nix build .#cliwrapFsharp.fetch-deps --print-out-paths) src/CliWrap.FSharp/deps.json

trimmable:
	$(DOTNET) publish examples/CliWrap.FSharp.Trimming -c Release --use-current-runtime

aot:
	$(DOTNET) publish examples/CliWrap.FSharp.Aot -c Release --use-current-runtime

update:
	nix flake update

vendor: $(UPSTREAM_STAMP)

# Copied, not linked: the Dummy program writes bin/ and obj/ beside its sources, and the store is read-only.
# Relock only on a tag change: `nix flake lock` fails in the dev container, where the checkout has another owner.
$(UPSTREAM_STAMP): flake.nix flake.lock $(CLIWRAP_FSPROJ)
	@want="$$(sed -n 's|.*"github:Tyrrrz/CliWrap/\([^"]*\)".*|\1|p' flake.nix)"; \
	locked="$$($(NIX) eval --raw --impure --expr '$(FLAKE_LOCK_NODE).original.ref')"; \
	if [ "$$want" != "$$locked" ]; then \
		echo "Relocking the cliwrap input: $$locked -> $$want"; \
		$(NIX) flake lock || exit 1; \
	fi
	@tag="$$($(NIX) eval --raw --impure --expr '$(FLAKE_LOCK_NODE).original.ref')"; \
	pkg="$$(sed -n 's/.*Include="CliWrap" Version="\([^"]*\)".*/\1/p' $(CLIWRAP_FSPROJ))"; \
	if [ "$$tag" != "$$pkg" ]; then \
		echo "error: the cliwrap flake input is pinned to '$$tag', but $(CLIWRAP_FSPROJ) uses CliWrap '$$pkg'." >&2; \
		echo "Set the cliwrap tag in flake.nix to '$$pkg' and run 'make vendor' again." >&2; \
		exit 1; \
	fi; \
	src="$$($(NIX) eval --raw --impure --expr '(builtins.fetchTree $(FLAKE_LOCK_NODE).locked).outPath')" || exit 1; \
	rm -rf $(UPSTREAM_DIR); \
	mkdir -p $(dir $(UPSTREAM_DIR)); \
	cp -R "$$src" $(UPSTREAM_DIR); \
	chmod -R u+w $(UPSTREAM_DIR); \
	echo "$$tag" > $@

devcontainer:
	$(NPX) @devcontainers/cli build . --workspace-folder .

result: src/CliWrap.FSharp/deps.json
	nix build .#cliwrapFsharp

bin/fetch-deps.sh:
	nix build .#cliwrapFsharp.fetch-deps --out-link $@

src/CliWrap.FSharp/deps.json: bin/fetch-deps.sh
	${CURDIR}/$< ${CURDIR}/$@
