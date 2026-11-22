DOTNET   ?= dotnet
FANTOMAS ?= $(DOTNET) fantomas
NPX      ?= npx

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

vendor: vendor/CliWrap/.upstream-version

vendor/CliWrap/.upstream-version: flake.nix flake.lock scripts/vendor.sh
	scripts/vendor.sh

devcontainer:
	$(NPX) @devcontainers/cli build . --workspace-folder .

result: src/CliWrap.FSharp/deps.json
	nix build .#cliwrapFsharp

bin/fetch-deps.sh:
	nix build .#cliwrapFsharp.fetch-deps --out-link $@

src/CliWrap.FSharp/deps.json: bin/fetch-deps.sh
	${CURDIR}/$< ${CURDIR}/$@
