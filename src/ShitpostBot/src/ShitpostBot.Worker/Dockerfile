FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /app

COPY "Directory.Build.props" "Directory.Build.props"
COPY "Directory.Packages.props" "Directory.Packages.props"

COPY "src/ShitpostBot.Domain/*.csproj" "src/ShitpostBot.Domain/"
COPY "src/ShitpostBot.Infrastructure/*.csproj" "src/ShitpostBot.Infrastructure/"
COPY "src/ShitpostBot.Worker/*.csproj" "src/ShitpostBot.Worker/"

RUN dotnet restore src/ShitpostBot.Worker

COPY "src/ShitpostBot.Domain" "src/ShitpostBot.Domain/"
COPY "src/ShitpostBot.Infrastructure" "src/ShitpostBot.Infrastructure/"
COPY "src/ShitpostBot.Worker" "src/ShitpostBot.Worker/"

WORKDIR /app/src/ShitpostBot.Worker
RUN dotnet build -c Release -o out
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/runtime:10.0-alpine AS runtime
WORKDIR /app

COPY --from=build "/app/src/ShitpostBot.Worker/out" "."
ENTRYPOINT ["dotnet", "ShitpostBot.Worker.dll"]