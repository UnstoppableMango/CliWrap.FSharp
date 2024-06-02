WORKING_DIR := $(shell git rev-parse --show-toplevel)

all:
	dotnet build

dummy_patch:
	cd vendor/CliWrap && git diff > ${WORKING_DIR}/vendor/patches/dummy-program.patch

dummy_program:
	cd vendor/CliWrap && git apply ${WORKING_DIR}/vendor/patches/dummy-program.patch
	cp -r vendor/CliWrap/CliWrap.Tests.Dummy src
	cd vendor/CliWrap && git reset --hard
