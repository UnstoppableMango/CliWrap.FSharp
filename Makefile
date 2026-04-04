DOTNET   ?= dotnet
FANTOMAS ?= $(DOTNET) fantomas
NPX      ?= npx

build:
	$(DOTNET) build

check:
	nix flake check

test:
	$(DOTNET) test

format fmt: .fantomasignore .config/dotnet-tools.json
	$(FANTOMAS) .

trimmable:
	$(DOTNET) publish examples/CliWrap.FSharp.Trimming -c Release --use-current-runtime

aot:
	$(DOTNET) publish examples/CliWrap.FSharp.Aot -c Release --use-current-runtime

update:
	nix flake update

prepare_dummy:
	cd vendor/CliWrap && git apply ${CURDIR}/vendor/patches/dummy-program.patch

dummy_patch:
	cd vendor/CliWrap && git diff > ${CURDIR}/vendor/patches/dummy-program.patch

dummy_program: prepare_dummy
	cp -r vendor/CliWrap/CliWrap.Tests.Dummy src
	cd vendor/CliWrap && git reset --hard

devcontainer:
	$(NPX) @devcontainers/cli build . --workspace-folder .

result: src/CliWrap.FSharp/deps.json
	nix build .#cliwrapFsharp

bin/fetch-deps.sh:
	nix build .#cliwrapFsharp.fetch-deps --out-link $@

src/CliWrap.FSharp/deps.json: bin/fetch-deps.sh
	${CURDIR}/$< ${CURDIR}/$@
