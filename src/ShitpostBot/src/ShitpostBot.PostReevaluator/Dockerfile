FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /app

COPY "Directory.Build.props" "Directory.Build.props"
COPY "Directory.Packages.props" "Directory.Packages.props"

COPY "src/ShitpostBot.Domain/*.csproj" "src/ShitpostBot.Domain/"
COPY "src/ShitpostBot.Infrastructure/*.csproj" "src/ShitpostBot.Infrastructure/"
COPY "src/ShitpostBot.PostReevaluator/*.csproj" "src/ShitpostBot.PostReevaluator/"

RUN dotnet restore src/ShitpostBot.PostReevaluator

COPY "src/ShitpostBot.Domain" "src/ShitpostBot.Domain/"
COPY "src/ShitpostBot.Infrastructure" "src/ShitpostBot.Infrastructure/"
COPY "src/ShitpostBot.PostReevaluator" "src/ShitpostBot.PostReevaluator/"

WORKDIR /app/src/ShitpostBot.PostReevaluator
RUN dotnet build -c Release -o out
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/runtime:10.0-alpine AS runtime
WORKDIR /app

COPY --from=build "/app/src/ShitpostBot.PostReevaluator/out" "."
ENTRYPOINT ["dotnet", "ShitpostBot.PostReevaluator.dll"]
