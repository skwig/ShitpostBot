FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /app

COPY "Directory.Build.props" "Directory.Build.props"
COPY "Directory.Packages.props" "Directory.Packages.props"

COPY "src/ShitpostBot.Domain/*.csproj" "src/ShitpostBot.Domain/"
COPY "src/ShitpostBot.Infrastructure/*.csproj" "src/ShitpostBot.Infrastructure/"
COPY "src/ShitpostBot.Application/*.csproj" "src/ShitpostBot.Application/"
COPY "src/ShitpostBot.WebApi/*.csproj" "src/ShitpostBot.WebApi/"

RUN dotnet restore src/ShitpostBot.WebApi

COPY "src/ShitpostBot.Domain" "src/ShitpostBot.Domain/"
COPY "src/ShitpostBot.Infrastructure" "src/ShitpostBot.Infrastructure/"
COPY "src/ShitpostBot.Application" "src/ShitpostBot.Application/"
COPY "src/ShitpostBot.WebApi" "src/ShitpostBot.WebApi/"

WORKDIR /app/src/ShitpostBot.WebApi
RUN dotnet build -c Release -o out
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
EXPOSE 8080
WORKDIR /app

COPY --from=build "/app/src/ShitpostBot.WebApi/out" "."
ENTRYPOINT ["dotnet", "ShitpostBot.WebApi.dll"]