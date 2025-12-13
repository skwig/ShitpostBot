FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /app

COPY "Directory.Build.props" "Directory.Build.props"
COPY "Directory.Packages.props" "Directory.Packages.props"

COPY "src/ShitpostBot.Domain/*.csproj" "src/ShitpostBot.Domain/"
COPY "src/ShitpostBot.Infrastructure/*.csproj" "src/ShitpostBot.Infrastructure/"
COPY "src/ShitpostBot.Infrastructure.Migrator/*.csproj" "src/ShitpostBot.Infrastructure.Migrator/"

RUN dotnet restore src/ShitpostBot.Infrastructure.Migrator

COPY "src/ShitpostBot.Domain" "src/ShitpostBot.Domain/"
COPY "src/ShitpostBot.Infrastructure" "src/ShitpostBot.Infrastructure/"
COPY "src/ShitpostBot.Infrastructure.Migrator" "src/ShitpostBot.Infrastructure.Migrator/"

WORKDIR /app/src/ShitpostBot.Infrastructure.Migrator
RUN dotnet build -c Release -o out
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/runtime:10.0-alpine AS runtime
WORKDIR /app

COPY --from=build "/app/src/ShitpostBot.Infrastructure.Migrator/out" "."
ENTRYPOINT ["dotnet", "ShitpostBot.Infrastructure.Migrator.dll"]