WORKING_DIR := $(shell git rev-parse --show-toplevel)

all:
	dotnet build

prepare_dummy:
	cd vendor/CliWrap && git apply ${WORKING_DIR}/vendor/patches/dummy-program.patch

dummy_patch:
	cd vendor/CliWrap && git diff > ${WORKING_DIR}/vendor/patches/dummy-program.patch

dummy_program: prepare_dummy
	cp -r vendor/CliWrap/CliWrap.Tests.Dummy src
	cd vendor/CliWrap && git reset --hard
