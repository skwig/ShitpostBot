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
          file
          kubernetes-helm
          ijhttp
          just
          uv
        ];

        pythonEnv = pkgs.python312.withPackages (
          ps: with ps; [
            black
            fastapi
            httpx
            mypy
            pip
            pytest
            requests
            uvicorn
          ]
        );
      in
      {
        devShells.default = pkgs.mkShell {
          packages = nativeDeps ++ [ pythonEnv ];

          LD_LIBRARY_PATH = pkgs.lib.makeLibraryPath [
            pkgs.file
            pkgs.stdenv.cc.cc.lib
            pkgs.zlib
          ];

          shellHook = ''
            if [ -f ./src/ShitpostBot/dotnet-tools.json ]; then
              dotnet tool restore --tool-manifest ./src/ShitpostBot/dotnet-tools.json
            fi
          '';
        };
      }
    );
}
