FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# copy csproj and restore as distinct layers
COPY Directory.Packages.props Directory.Packages.props
COPY ShitpostBot.Infrastructure.Migrator/*.csproj ./ShitpostBot.Infrastructure.Migrator/
COPY ShitpostBot.Infrastructure/*.csproj ./ShitpostBot.Infrastructure/
COPY ShitpostBot.Domain/*.csproj ./ShitpostBot.Domain/

RUN dotnet restore ShitpostBot.Infrastructure.Migrator

# copy everything else and build app
COPY ShitpostBot.Infrastructure.Migrator/. ./ShitpostBot.Infrastructure.Migrator/
COPY ShitpostBot.Infrastructure/. ./ShitpostBot.Infrastructure/
COPY ShitpostBot.Domain/. ./ShitpostBot.Domain/

WORKDIR /app/ShitpostBot.Infrastructure.Migrator
RUN dotnet publish -c Release -o out

# run app
FROM mcr.microsoft.com/dotnet/runtime:8.0-alpine AS runtime
WORKDIR /app

COPY --from=build-env /app/ShitpostBot.Infrastructure.Migrator/out ./
ENTRYPOINT ["dotnet", "ShitpostBot.Infrastructure.Migrator.dll"]