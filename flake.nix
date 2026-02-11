{
  description = "";

  inputs = {
    nixpkgs.url = "github:nixos/nixpkgs?ref=nixos-unstable";
    flake-parts.url = "github:hercules-ci/flake-parts";
    systems.url = "github:nix-systems/default";

    treefmt-nix = {
      url = "github:numtide/treefmt-nix";
      inputs = {
        nixpkgs.follows = "nixpkgs";
        flake-parts.follows = "flake-parts";
      };
    };
  };

  outputs = { self, nixpkgs }: {
  };
}
