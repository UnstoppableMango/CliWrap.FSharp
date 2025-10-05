DOTNET   ?= dotnet
FANTOMAS ?= $(DOTNET) fantomas
NPX      ?= npx

build:
	$(DOTNET) build

test:
	$(DOTNET) test

format fmt: .fantomasignore .config/dotnet-tools.json
	$(FANTOMAS) .

trimmable:
	$(DOTNET) publish examples/CliWrap.FSharp.Trimming -c Release --use-current-runtime

aot:
	$(DOTNET) publish examples/CliWrap.FSharp.Aot -c Release --use-current-runtime

prepare_dummy:
	cd vendor/CliWrap && git apply ${CURDIR}/vendor/patches/dummy-program.patch

dummy_patch:
	cd vendor/CliWrap && git diff > ${CURDIR}/vendor/patches/dummy-program.patch

dummy_program: prepare_dummy
	cp -r vendor/CliWrap/CliWrap.Tests.Dummy src
	cd vendor/CliWrap && git reset --hard

devcontainer:
	$(NPX) @devcontainers/cli build . --workspace-folder .
