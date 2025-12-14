FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files
COPY ["src/ShitpostBot.WebApi/ShitpostBot.WebApi.csproj", "src/ShitpostBot.WebApi/"]
COPY ["src/ShitpostBot.Application/ShitpostBot.Application.csproj", "src/ShitpostBot.Application/"]
COPY ["src/ShitpostBot.Infrastructure/ShitpostBot.Infrastructure.csproj", "src/ShitpostBot.Infrastructure/"]
COPY ["src/ShitpostBot.Domain/ShitpostBot.Domain.csproj", "src/ShitpostBot.Domain/"]
COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]

# Restore dependencies
RUN dotnet restore "src/ShitpostBot.WebApi/ShitpostBot.WebApi.csproj"

# Copy source code
COPY src/ src/

# Build
WORKDIR "/src/src/ShitpostBot.WebApi"
RUN dotnet build "ShitpostBot.WebApi.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "ShitpostBot.WebApi.csproj" -c Release -o /app/publish

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Copy test fixtures
COPY test/fixtures/ /app/fixtures/

EXPOSE 8080
ENTRYPOINT ["dotnet", "ShitpostBot.WebApi.dll"]