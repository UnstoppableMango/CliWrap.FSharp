WORKING_DIR := $(shell git rev-parse --show-toplevel)

build:
	dotnet build

test:
	dotnet test

format: .fantomasignore .config/dotnet-tools.json
	dotnet fantomas .

trimmable:
	dotnet publish examples/CliWrap.FSharp.Trimming -c Release --use-current-runtime

aot:
	dotnet publish examples/CliWrap.FSharp.Aot -c Release --use-current-runtime

prepare_dummy:
	cd vendor/CliWrap && git apply ${WORKING_DIR}/vendor/patches/dummy-program.patch

dummy_patch:
	cd vendor/CliWrap && git diff > ${WORKING_DIR}/vendor/patches/dummy-program.patch

dummy_program: prepare_dummy
	cp -r vendor/CliWrap/CliWrap.Tests.Dummy src
	cd vendor/CliWrap && git reset --hard
