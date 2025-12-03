FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# copy csproj and restore as distinct layers
COPY Directory.Build.props Directory.Build.props
COPY Directory.Packages.props Directory.Packages.props
COPY ShitpostBot.Worker/*.csproj ./ShitpostBot.Worker/
COPY ShitpostBot.Infrastructure/*.csproj ./ShitpostBot.Infrastructure/
COPY ShitpostBot.Domain/*.csproj ./ShitpostBot.Domain/

RUN dotnet restore ShitpostBot.Worker

# copy everything else and build app
COPY ShitpostBot.Worker/. ./ShitpostBot.Worker/
COPY ShitpostBot.Infrastructure/. ./ShitpostBot.Infrastructure/
COPY ShitpostBot.Domain/. ./ShitpostBot.Domain/

WORKDIR /app/ShitpostBot.Worker
RUN dotnet publish -c Release -o out

# run app
FROM mcr.microsoft.com/dotnet/runtime:8.0-alpine AS runtime
WORKDIR /app

COPY --from=build-env /app/ShitpostBot.Worker/out ./
ENTRYPOINT ["dotnet", "ShitpostBot.Worker.dll"]