{
  description = "F# bindings for CliWrap";

  inputs = {
    nixpkgs.url = "github:nixos/nixpkgs?ref=nixos-unstable";
    flake-parts.url = "github:hercules-ci/flake-parts";
    systems.url = "github:UnstoppableMango/nix-systems";

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

      imports = with inputs; [
        systems.flakeModule
        treefmt-nix.flakeModule
      ];

      perSystem =
        { pkgs, lib, ... }:
        let
          dotnet =
            with pkgs.dotnetCorePackages;
            combinePackages [
              sdk_9_0
              sdk_10_0
            ];

          cliwrapFsharp = pkgs.buildDotnetModule rec {
            pname = "CliWrap.FSharp";
            version = "0.0.12";
            MinVerVersionOverride = version;

            src = lib.cleanSource ./.;
            projectFile = "src/CliWrap.FSharp/CliWrap.FSharp.fsproj";
            nugetDeps = ./src/CliWrap.FSharp/deps.json;

            dotnet-sdk = dotnet;
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
              dotnet
              fantomas
              gnumake
              nixfmt
              nodejs-slim
            ];
          };

          treefmt.programs = {
            actionlint.enable = true;
            deadnix.enable = true;
            fantomas = {
              enable = true;
              dotnet-sdk = dotnet;
            };
            nixfmt.enable = true;
            statix.enable = true;
            zizmor.enable = true;
          };
        };
    };
}
