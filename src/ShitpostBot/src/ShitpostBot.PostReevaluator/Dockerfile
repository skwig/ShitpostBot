FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/ShitpostBot/src/ShitpostBot.PostReevaluator/ShitpostBot.PostReevaluator.csproj", "ShitpostBot.PostReevaluator/"]
COPY ["src/ShitpostBot/src/ShitpostBot.Infrastructure/ShitpostBot.Infrastructure.csproj", "ShitpostBot.Infrastructure/"]
COPY ["src/ShitpostBot/src/ShitpostBot.Application/ShitpostBot.Application.csproj", "ShitpostBot.Application/"]
COPY ["src/ShitpostBot/src/ShitpostBot.Domain/ShitpostBot.Domain.csproj", "ShitpostBot.Domain/"]
COPY ["src/ShitpostBot/Directory.Packages.props", "./"]
COPY ["src/ShitpostBot/Directory.Build.props", "./"]
RUN dotnet restore "ShitpostBot.PostReevaluator/ShitpostBot.PostReevaluator.csproj"
COPY src/ShitpostBot/src/ .
WORKDIR "/src/ShitpostBot.PostReevaluator"
RUN dotnet build "ShitpostBot.PostReevaluator.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ShitpostBot.PostReevaluator.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ShitpostBot.PostReevaluator.dll"]
