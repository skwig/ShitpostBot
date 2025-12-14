{
  inputs = {
    nixpkgs.url = "github:NixOS/nixpkgs/nixos-unstable";
    flake-utils.url = "github:numtide/flake-utils";
  };

  outputs =
    {
      self,
      nixpkgs,
      flake-utils,
    }:
    flake-utils.lib.eachDefaultSystem (
      system:
      let
        pkgs = import nixpkgs {
          inherit system;
          config = {
            allowUnfree = true;
          };
        };

        nativeDeps = with pkgs; [
          kubernetes-helm
          ijhttp
        ];
      in
      {
        devShells.default = pkgs.mkShell {
          packages = nativeDeps;
        };
      }
    );
}
