DOTNET   ?= dotnet
FANTOMAS ?= $(DOTNET) fantomas
NPX      ?= npx

.PHONY: build check test coverage format fmt deps trimmable aot update vendor analyze devcontainer

build: vendor
	$(DOTNET) build

check:
	nix flake check

test: vendor
	$(DOTNET) test

coverage: vendor
	rm -rf TestResults
	$(DOTNET) test --collect:"XPlat Code Coverage" --results-directory TestResults
	scripts/coverage.sh TestResults/*/coverage.cobertura.xml

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

analyze: vendor
	DOTNET="$(DOTNET)" .config/analyzers/analyze.sh

devcontainer:
	$(NPX) @devcontainers/cli build . --workspace-folder .

result: src/CliWrap.FSharp/deps.json
	nix build .#cliwrapFsharp

bin/fetch-deps.sh:
	nix build .#cliwrapFsharp.fetch-deps --out-link $@

src/CliWrap.FSharp/deps.json: bin/fetch-deps.sh
	${CURDIR}/$< ${CURDIR}/$@
