{
  description = "F# bindings for CliWrap";

  inputs = {
    nixpkgs.url = "github:nixos/nixpkgs?ref=nixos-unstable";
    flake-parts.url = "github:hercules-ci/flake-parts";
    systems.url = "github:nix-systems/default";

    treefmt-nix = {
      url = "github:numtide/treefmt-nix";
      inputs = {
        nixpkgs.follows = "nixpkgs";
      };
    };
  };

  outputs =
    inputs@{ flake-parts, ... }:
    flake-parts.lib.mkFlake { inherit inputs; } {
      systems = import inputs.systems;
      imports = [ inputs.treefmt-nix.flakeModule ];

      perSystem =
        { pkgs, lib, ... }:
        let
          dotnetPkg = (
            with pkgs.dotnetCorePackages;
            combinePackages [
              sdk_9_0
              sdk_10_0
            ]
          );

          cliwrapFsharp = pkgs.buildDotnetModule rec {
            pname = "CliWrap.FSharp";
            version = "0.0.12";
            MinVerVersionOverride = version;

            src = lib.cleanSource ./.;
            projectFile = "src/CliWrap.FSharp/CliWrap.FSharp.fsproj";
            nugetDeps = ./src/CliWrap.FSharp/deps.json;

            dotnet-sdk = dotnetPkg;
            dotnet-runtime = pkgs.dotnetCorePackages.runtime_10_0;
            dontPublish = true;
            packNupkg = true;
          };
        in
        {
          packages = {
            inherit cliwrapFsharp;
            default = cliwrapFsharp;
          };

          devShells.default = pkgs.mkShell {
            packages = with pkgs; [
              docker
              dotnetPkg
              fantomas
              gnumake
              nixfmt
              nodejs-slim
            ];
          };

          treefmt.programs = {
            actionlint.enable = true;
            fantomas = {
              enable = true;
              dotnet-sdk = dotnetPkg;
            };
            nixfmt.enable = true;
          };
        };
    };
}
