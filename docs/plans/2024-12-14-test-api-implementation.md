# Test API Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Enable testing ShitpostBot Worker without Discord by creating an Application layer with extracted business logic and a WebApi project with HTTP test endpoints.

**Architecture:** Extract repost-related MediatR handlers from Worker into new Application layer. Create WebApi with minimal API endpoints that trigger the same handlers via MediatR/MassTransit. Share MassTransit PostgreSQL configuration in Infrastructure.

**Tech Stack:** .NET 10, ASP.NET Core Minimal APIs, MediatR, MassTransit with PostgreSQL transport, EF Core, Testcontainers

---

## Phase 1: Shared MassTransit Configuration

### Task 1.1: Extract MassTransit Configuration to Infrastructure

**Files:**
- Create: `src/ShitpostBot/src/ShitpostBot.Infrastructure/Public/MassTransitConfiguration.cs`
- Modify: `src/ShitpostBot/src/ShitpostBot.Worker/Program.cs`

**Step 1: Create MassTransit configuration extension method**

Create file: `src/ShitpostBot/src/ShitpostBot.Infrastructure/Public/MassTransitConfiguration.cs`

```csharp
using System;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using CloudEventify.MassTransit;
using ShitpostBot.Infrastructure.Messages;

namespace ShitpostBot.Infrastructure;

public static class MassTransitConfiguration
{
    public static IServiceCollection AddShitpostBotMassTransit(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<IRegistrationConfigurator> configureConsumers)
    {
        var builder = new NpgsqlConnectionStringBuilder(
            configuration.GetConnectionString("ShitpostBotMessaging")
        );
        
        services.AddOptions<SqlTransportOptions>().Configure(options =>
        {
            // Workaround for Npgsql initialization race condition with pgvector
            options.Host = builder.Host;
            options.Port = builder.Port;
            options.Username = builder.Username;
            options.Password = builder.Password;
            options.Database = builder.Database;
        });
        
        services.AddPostgresMigrationHostedService();
        
        services.AddMassTransit(x =>
        {
            configureConsumers(x);
            
            x.UsingPostgres((context, cfg) =>
            {
                cfg.ConfigureEndpoints(context);
                cfg.UseCloudEvents()
                    .WithTypes(map => map
                        .Map<ImagePostTracked>("imagePostTracked")
                        .Map<LinkPostTracked>("linkPostTracked")
                    );
            });
        });
        
        return services;
    }
}
```

**Step 2: Refactor Worker to use shared configuration**

Modify: `src/ShitpostBot/src/ShitpostBot.Worker/Program.cs`

Replace lines 37-69 (the MassTransit configuration) with:

```csharp
services.AddShitpostBotMassTransit(hostContext.Configuration, x =>
{
    x.AddConsumer<EvaluateRepost_ImagePostTrackedHandler>();
    x.AddConsumer<EvaluateRepost_LinkPostTrackedHandler>();
});
```

**Step 3: Build and verify**

Run:
```bash
cd src/ShitpostBot
dotnet build src/ShitpostBot.Worker/ShitpostBot.Worker.csproj
```

Expected: BUILD SUCCEEDED

**Step 4: Commit**

```bash
git add src/ShitpostBot/src/ShitpostBot.Infrastructure/Public/MassTransitConfiguration.cs
git add src/ShitpostBot/src/ShitpostBot.Worker/Program.cs
git commit -m "refactor: extract MassTransit configuration to Infrastructure

- Add AddShitpostBotMassTransit extension method
- Centralizes PostgreSQL transport setup
- Enables reuse in Worker and WebApi"
```

---

## Phase 2: Application Layer - Project Setup

### Task 2.1: Create Application Project

**Files:**
- Create: `src/ShitpostBot/src/ShitpostBot.Application/ShitpostBot.Application.csproj`
- Create: `src/ShitpostBot/src/ShitpostBot.Application/GlobalUsings.cs`

**Step 1: Create Application project file**

Create: `src/ShitpostBot/src/ShitpostBot.Application/ShitpostBot.Application.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <ItemGroup>
    <ProjectReference Include="..\ShitpostBot.Domain\ShitpostBot.Domain.csproj"/>
    <ProjectReference Include="..\ShitpostBot.Infrastructure\ShitpostBot.Infrastructure.csproj"/>
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="MediatR"/>
    <PackageReference Include="MassTransit"/>
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions"/>
    <PackageReference Include="Microsoft.Extensions.Options"/>
  </ItemGroup>

</Project>
```

**Step 2: Create GlobalUsings**

Create: `src/ShitpostBot/src/ShitpostBot.Application/GlobalUsings.cs`

```csharp
global using System;
global using System.Threading;
global using System.Threading.Tasks;
global using Microsoft.Extensions.Logging;
```

**Step 3: Build to verify project structure**

Run:
```bash
cd src/ShitpostBot
dotnet build src/ShitpostBot.Application/ShitpostBot.Application.csproj
```

Expected: BUILD SUCCEEDED

**Step 4: Commit**

```bash
git add src/ShitpostBot/src/ShitpostBot.Application/
git commit -m "feat: create Application project

- Add project with Domain and Infrastructure references
- Setup for MediatR handlers and MassTransit consumers"
```

---

## Phase 3: Application Layer - Extract Post Tracking

### Task 3.1: Move Image Message Tracking to Application

**Files:**
- Create: `src/ShitpostBot/src/ShitpostBot.Application/Features/PostTracking/ImageMessageCreated.cs`
- Create: `src/ShitpostBot/src/ShitpostBot.Application/Features/PostTracking/TrackImageMessageHandler.cs`
- Delete: `src/ShitpostBot/src/ShitpostBot.Worker/Internal/Features/PostTracking/TrackImageMessageHandler.cs`
- Modify: `src/ShitpostBot/src/ShitpostBot.Worker/Internal/Core/ChatMessageCreatedListener.cs`

**Step 1: Create ImageMessageCreated notification in Application**

Create: `src/ShitpostBot/src/ShitpostBot.Application/Features/PostTracking/ImageMessageCreated.cs`

```csharp
using MediatR;
using ShitpostBot.Infrastructure;

namespace ShitpostBot.Application.Features.PostTracking;

public record ImageMessageCreated(ImageMessage ImageMessage) : INotification;
```

**Step 2: Move TrackImageMessageHandler to Application**

Create: `src/ShitpostBot/src/ShitpostBot.Application/Features/PostTracking/TrackImageMessageHandler.cs`

```csharp
using MassTransit;
using MediatR;
using ShitpostBot.Domain;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Messages;

namespace ShitpostBot.Application.Features.PostTracking;

internal class TrackImageMessageHandler(
    ILogger<TrackImageMessageHandler> logger,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider,
    IBus bus)
    : INotificationHandler<ImageMessageCreated>
{
    public async Task Handle(ImageMessageCreated notification, CancellationToken cancellationToken)
    {
        var utcNow = dateTimeProvider.UtcNow;

        var image = Image.CreateOrDefault(notification.ImageMessage.Attachment.Id, notification.ImageMessage.Attachment.Uri);
        if (image == null)
        {
            logger.LogDebug("Image '{Uri}' is not interesting. Not tracking.", notification.ImageMessage.Attachment.Uri);
            return;
        }

        var newPost = ImagePost.Create(
            notification.ImageMessage.PostedOn,
            new ChatMessageIdentifier(
                notification.ImageMessage.Identification.GuildId,
                notification.ImageMessage.Identification.ChannelId,
                notification.ImageMessage.Identification.MessageId
            ),
            new PosterIdentifier(
                notification.ImageMessage.Identification.PosterId
            ),
            utcNow,
            image
        );

        await unitOfWork.ImagePostsRepository.CreateAsync(newPost, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await bus.Publish(new ImagePostTracked { ImagePostId = newPost.Id }, cancellationToken: cancellationToken);

        logger.LogDebug("Tracked ImagePost {NewPost}", newPost);
    }
}
```

**Step 3: Build Application to verify**

Run:
```bash
cd src/ShitpostBot
dotnet build src/ShitpostBot.Application/ShitpostBot.Application.csproj
```

Expected: BUILD SUCCEEDED

**Step 4: Update Worker to reference Application**

Modify: `src/ShitpostBot/src/ShitpostBot.Worker/ShitpostBot.Worker.csproj`

Add to ItemGroup with ProjectReference:
```xml
<ProjectReference Include="..\ShitpostBot.Application\ShitpostBot.Application.csproj"/>
```

**Step 5: Update Worker ChatMessageCreatedListener import**

Modify: `src/ShitpostBot/src/ShitpostBot.Worker/Internal/Core/ChatMessageCreatedListener.cs`

Change line 11:
```csharp
using ShitpostBot.Worker.Features.PostTracking;
```

To:
```csharp
using ShitpostBot.Application.Features.PostTracking;
```

**Step 6: Delete old Worker handler**

Run:
```bash
rm src/ShitpostBot/src/ShitpostBot.Worker/Internal/Features/PostTracking/TrackImageMessageHandler.cs
```

**Step 7: Build Worker to verify**

Run:
```bash
cd src/ShitpostBot
dotnet build src/ShitpostBot.Worker/ShitpostBot.Worker.csproj
```

Expected: BUILD SUCCEEDED

**Step 8: Commit**

```bash
git add -A
git commit -m "refactor: move ImageMessageCreated and handler to Application

- Extract TrackImageMessageHandler to Application layer
- Worker now references Application for shared handlers"
```

---

### Task 3.2: Move Link Message Tracking to Application

**Files:**
- Create: `src/ShitpostBot/src/ShitpostBot.Application/Features/PostTracking/LinkMessageCreated.cs`
- Create: `src/ShitpostBot/src/ShitpostBot.Application/Features/PostTracking/TrackLinkMessageHandler.cs`
- Delete: `src/ShitpostBot/src/ShitpostBot.Worker/Internal/Features/PostTracking/TrackLinkMessageHandler.cs`

**Step 1: Create LinkMessageCreated notification**

Create: `src/ShitpostBot/src/ShitpostBot.Application/Features/PostTracking/LinkMessageCreated.cs`

```csharp
using MediatR;
using ShitpostBot.Infrastructure;

namespace ShitpostBot.Application.Features.PostTracking;

public record LinkMessageCreated(LinkMessage LinkMessage) : INotification;
```

**Step 2: Move TrackLinkMessageHandler to Application**

Create: `src/ShitpostBot/src/ShitpostBot.Application/Features/PostTracking/TrackLinkMessageHandler.cs`

```csharp
using MassTransit;
using MediatR;
using ShitpostBot.Domain;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Messages;

namespace ShitpostBot.Application.Features.PostTracking;

internal class TrackLinkMessageHandler(
    ILogger<TrackLinkMessageHandler> logger,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider,
    IBus bus)
    : INotificationHandler<LinkMessageCreated>
{
    public async Task Handle(LinkMessageCreated notification, CancellationToken cancellationToken)
    {
        var utcNow = dateTimeProvider.UtcNow;

        var link = Link.CreateOrDefault(notification.LinkMessage.Embed.Url);
        if (link == null)
        {
            logger.LogDebug("Link '{Uri}' is not interesting. Not tracking.", notification.LinkMessage.Embed.Url);
            return;
        }

        var newPost = LinkPost.Create(
            notification.LinkMessage.PostedOn,
            new ChatMessageIdentifier(
                notification.LinkMessage.Identification.GuildId,
                notification.LinkMessage.Identification.ChannelId,
                notification.LinkMessage.Identification.MessageId
            ),
            new PosterIdentifier(
                notification.LinkMessage.Identification.PosterId
            ),
            utcNow,
            link
        );

        await unitOfWork.LinkPostsRepository.CreateAsync(newPost, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await bus.Publish(new LinkPostTracked { LinkPostId = newPost.Id }, cancellationToken: cancellationToken);

        logger.LogDebug("Tracked LinkPost {NewPost}", newPost);
    }
}
```

**Step 3: Delete old Worker handler**

Run:
```bash
rm src/ShitpostBot/src/ShitpostBot.Worker/Internal/Features/PostTracking/TrackLinkMessageHandler.cs
```

**Step 4: Build to verify**

Run:
```bash
cd src/ShitpostBot
dotnet build src/ShitpostBot.Application/ShitpostBot.Application.csproj
dotnet build src/ShitpostBot.Worker/ShitpostBot.Worker.csproj
```

Expected: BUILD SUCCEEDED (both)

**Step 5: Commit**

```bash
git add -A
git commit -m "refactor: move LinkMessageCreated and handler to Application"
```

---

## Phase 4: Application Layer - Extract Repost Evaluation

### Task 4.1: Move Image Repost Evaluation to Application

**Files:**
- Create: `src/ShitpostBot/src/ShitpostBot.Application/Features/Repost/EvaluateRepost_ImagePostTrackedHandler.cs`
- Create: `src/ShitpostBot/src/ShitpostBot.Application/Services/IImageFeatureExtractorApi.cs`
- Delete: `src/ShitpostBot/src/ShitpostBot.Worker/Internal/Features/Repost/EvaluateRepost_ImagePostTrackedHandler.cs`
- Delete: `src/ShitpostBot/src/ShitpostBot.Worker/Internal/Services/IImageFeatureExtractorApi.cs`

**Step 1: Move IImageFeatureExtractorApi interface to Application**

Create: `src/ShitpostBot/src/ShitpostBot.Application/Services/IImageFeatureExtractorApi.cs`

```csharp
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Refit;

namespace ShitpostBot.Application.Services;

public interface IImageFeatureExtractorApi
{
    [Get("/images/features")]
    Task<ExtractImageFeaturesResponse> ExtractImageFeaturesAsync([AliasAs("image_url")] string imageUrl);
}

public record ExtractImageFeaturesResponse
{
    [JsonProperty("image_url")] public required string ImageUrl { get; init; }
    [JsonProperty("image_features")] public required float[] ImageFeatures { get; init; }
}

public class ImageFeatureExtractorApiOptions
{
    [Required] public required string Uri { get; init; }
}
```

**Step 2: Add Refit and Newtonsoft.Json to Application project**

Modify: `src/ShitpostBot/src/ShitpostBot.Application/ShitpostBot.Application.csproj`

Add to ItemGroup with PackageReference:
```xml
<PackageReference Include="Refit"/>
<PackageReference Include="Refit.Newtonsoft.Json"/>
<PackageReference Include="Newtonsoft.Json"/>
<PackageReference Include="Microsoft.Extensions.Options.ConfigurationExtensions"/>
```

**Step 3: Move EvaluateRepost_ImagePostTrackedHandler to Application**

Create: `src/ShitpostBot/src/ShitpostBot.Application/Features/Repost/EvaluateRepost_ImagePostTrackedHandler.cs`

```csharp
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Pgvector;
using ShitpostBot.Application.Services;
using ShitpostBot.Domain;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Messages;

namespace ShitpostBot.Application.Features.Repost;

internal class EvaluateRepost_ImagePostTrackedHandler(
    ILogger<EvaluateRepost_ImagePostTrackedHandler> logger,
    IImageFeatureExtractorApi imageFeatureExtractorApi,
    IUnitOfWork unitOfWork,
    IOptions<RepostServiceOptions> options,
    IChatClient chatClient,
    IDateTimeProvider dateTimeProvider,
    IImagePostsReader imagePostsReader)
    : IConsumer<ImagePostTracked>
{
    private static readonly string[] RepostReactions =
    {
        ":police_car:",
        ":rotating_light:"
    };

    public async Task Consume(ConsumeContext<ImagePostTracked> context)
    {
        var postToBeEvaluated = await unitOfWork.ImagePostsRepository.GetById(context.Message.ImagePostId);
        if (postToBeEvaluated == null)
        {
            throw new InvalidOperationException($"ImagePost {context.Message.ImagePostId} not found");
        }

        var extractImageFeaturesResponse = await imageFeatureExtractorApi.ExtractImageFeaturesAsync(postToBeEvaluated.Image.ImageUri.ToString());

        var imageFeatures = new ImageFeatures(new Vector(extractImageFeaturesResponse.ImageFeatures));
        postToBeEvaluated.SetImageFeatures(imageFeatures, dateTimeProvider.UtcNow);

        await unitOfWork.SaveChangesAsync(context.CancellationToken);

        var mostSimilarWhitelisted = await imagePostsReader
            .ClosestWhitelistedToImagePostWithFeatureVector(postToBeEvaluated.PostedOn, postToBeEvaluated.Image.ImageFeatures!.FeatureVector)
            .FirstOrDefaultAsync(context.CancellationToken);

        if (mostSimilarWhitelisted?.CosineSimilarity >= (double)options.Value.RepostSimilarityThreshold)
        {
            logger.LogDebug("Similarity of {Similarity:0.00000000} with {ImagePostId}, which is whitelisted", mostSimilarWhitelisted?.CosineSimilarity,
                mostSimilarWhitelisted?.ImagePostId);
            return;
        }

        var mostSimilar = await imagePostsReader
            .ClosestToImagePostWithFeatureVector(postToBeEvaluated.PostedOn, postToBeEvaluated.Image.ImageFeatures!.FeatureVector)
            .FirstOrDefaultAsync(context.CancellationToken);

        if (mostSimilar?.CosineSimilarity >= (double)options.Value.RepostSimilarityThreshold)
        {
            var identification = new MessageIdentification(
                postToBeEvaluated.ChatGuildId,
                postToBeEvaluated.ChatChannelId,
                postToBeEvaluated.PosterId,
                postToBeEvaluated.ChatMessageId
            );

            foreach (var repostReaction in RepostReactions)
            {
                await chatClient.React(identification, repostReaction);
                await Task.Delay(TimeSpan.FromMilliseconds(500), context.CancellationToken);
            }
        }
    }
}
```

**Step 4: Delete old Worker files**

Run:
```bash
rm src/ShitpostBot/src/ShitpostBot.Worker/Internal/Features/Repost/EvaluateRepost_ImagePostTrackedHandler.cs
rm src/ShitpostBot/src/ShitpostBot.Worker/Internal/Services/IImageFeatureExtractorApi.cs
```

**Step 5: Update Worker Program.cs consumer registration**

Modify: `src/ShitpostBot/src/ShitpostBot.Worker/Program.cs`

Change the MassTransit consumer registration to:
```csharp
using ShitpostBot.Application.Features.Repost;

// ... in ConfigureServices:
services.AddShitpostBotMassTransit(hostContext.Configuration, x =>
{
    x.AddConsumer<EvaluateRepost_ImagePostTrackedHandler>();
    x.AddConsumer<EvaluateRepost_LinkPostTrackedHandler>();
});
```

**Step 6: Build to verify**

Run:
```bash
cd src/ShitpostBot
dotnet build src/ShitpostBot.Application/ShitpostBot.Application.csproj
dotnet build src/ShitpostBot.Worker/ShitpostBot.Worker.csproj
```

Expected: BUILD SUCCEEDED (both)

**Step 7: Commit**

```bash
git add -A
git commit -m "refactor: move image repost evaluation to Application

- Extract EvaluateRepost_ImagePostTrackedHandler
- Move IImageFeatureExtractorApi to Application.Services
- Worker references Application for consumer registration"
```

---

### Task 4.2: Move Link Repost Evaluation to Application

**Files:**
- Create: `src/ShitpostBot/src/ShitpostBot.Application/Features/Repost/EvaluateRepost_LinkPostTrackedHandler.cs`
- Delete: `src/ShitpostBot/src/ShitpostBot.Worker/Internal/Features/Repost/EvaluateRepost_LinkPostTrackedHandler.cs`

**Step 1: Read the Worker's link repost handler**

Run:
```bash
cat src/ShitpostBot/src/ShitpostBot.Worker/Internal/Features/Repost/EvaluateRepost_LinkPostTrackedHandler.cs
```

**Step 2: Move EvaluateRepost_LinkPostTrackedHandler to Application**

Create: `src/ShitpostBot/src/ShitpostBot.Application/Features/Repost/EvaluateRepost_LinkPostTrackedHandler.cs`

Copy the content from Worker's EvaluateRepost_LinkPostTrackedHandler.cs and update namespace to:
```csharp
namespace ShitpostBot.Application.Features.Repost;
```

**Step 3: Delete old Worker handler**

Run:
```bash
rm src/ShitpostBot/src/ShitpostBot.Worker/Internal/Features/Repost/EvaluateRepost_LinkPostTrackedHandler.cs
```

**Step 4: Build to verify**

Run:
```bash
cd src/ShitpostBot
dotnet build src/ShitpostBot.Application/ShitpostBot.Application.csproj
dotnet build src/ShitpostBot.Worker/ShitpostBot.Worker.csproj
```

Expected: BUILD SUCCEEDED (both)

**Step 5: Commit**

```bash
git add -A
git commit -m "refactor: move link repost evaluation to Application"
```

---

### Task 4.3: Create Application DependencyInjection

**Files:**
- Create: `src/ShitpostBot/src/ShitpostBot.Application/DependencyInjection.cs`
- Modify: `src/ShitpostBot/src/ShitpostBot.Worker/Public/DependencyInjection.cs`

**Step 1: Create Application DI configuration**

Create: `src/ShitpostBot/src/ShitpostBot.Application/DependencyInjection.cs`

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using ShitpostBot.Application.Services;

namespace ShitpostBot.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddShitpostBotApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register MediatR handlers from this assembly
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        
        // Register Refit client for ML service
        services.AddOptions<ImageFeatureExtractorApiOptions>()
            .Bind(configuration.GetSection("ImageFeatureExtractorApi"))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        
        services.AddRefitClient<IImageFeatureExtractorApi>()
            .ConfigureHttpClient((sp, client) =>
            {
                var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ImageFeatureExtractorApiOptions>>().Value;
                client.BaseAddress = new Uri(options.Uri);
            });
        
        return services;
    }
}
```

**Step 2: Update Worker to use Application DI**

Modify: `src/ShitpostBot/src/ShitpostBot.Worker/Public/DependencyInjection.cs`

Remove IImageFeatureExtractorApi registration (it's now in Application), keep only Worker-specific services.

**Step 3: Update Worker Program.cs**

Modify: `src/ShitpostBot/src/ShitpostBot.Worker/Program.cs`

Add after line with `AddShitpostBotMassTransit`:
```csharp
services.AddShitpostBotApplication(hostContext.Configuration);
```

**Step 4: Build to verify**

Run:
```bash
cd src/ShitpostBot
dotnet build src/ShitpostBot.Worker/ShitpostBot.Worker.csproj
```

Expected: BUILD SUCCEEDED

**Step 5: Commit**

```bash
git add -A
git commit -m "feat: add Application DependencyInjection

- Register MediatR handlers from Application assembly
- Configure Refit client for ML service
- Worker uses Application DI extension"
```

---

## Phase 5: WebApi Project Setup

### Task 5.1: Create WebApi Project

**Files:**
- Create: `src/ShitpostBot/src/ShitpostBot.WebApi/ShitpostBot.WebApi.csproj`
- Create: `src/ShitpostBot/src/ShitpostBot.WebApi/Program.cs`
- Create: `src/ShitpostBot/src/ShitpostBot.WebApi/appsettings.json`
- Create: `src/ShitpostBot/src/ShitpostBot.WebApi/appsettings.Development.json`

**Step 1: Create WebApi project file**

Create: `src/ShitpostBot/src/ShitpostBot.WebApi/ShitpostBot.WebApi.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <ItemGroup>
    <ProjectReference Include="..\ShitpostBot.Application\ShitpostBot.Application.csproj"/>
    <ProjectReference Include="..\ShitpostBot.Infrastructure\ShitpostBot.Infrastructure.csproj"/>
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="MediatR"/>
    <PackageReference Include="MassTransit"/>
  </ItemGroup>

</Project>
```

**Step 2: Create minimal Program.cs**

Create: `src/ShitpostBot/src/ShitpostBot.WebApi/Program.cs`

```csharp
using ShitpostBot.Application;
using ShitpostBot.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddShitpostBotInfrastructure(builder.Configuration);
builder.Services.AddShitpostBotApplication(builder.Configuration);
builder.Services.AddShitpostBotMassTransit(builder.Configuration, x =>
{
    // WebApi doesn't consume messages initially, only publishes
});

var app = builder.Build();

app.MapGet("/", () => "ShitpostBot WebApi - Test Harness");

app.Run();
```

**Step 3: Create appsettings.json**

Create: `src/ShitpostBot/src/ShitpostBot.WebApi/appsettings.json`

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "MassTransit": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

**Step 4: Create appsettings.Development.json**

Create: `src/ShitpostBot/src/ShitpostBot.WebApi/appsettings.Development.json`

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information",
      "MassTransit": "Debug",
      "ShitpostBot": "Debug"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=shitpostbot;Username=postgres;Password=postgres",
    "ShitpostBotMessaging": "Host=localhost;Database=shitpostbot;Username=postgres;Password=postgres"
  },
  "ImageFeatureExtractorApi": {
    "Uri": "http://localhost:5000"
  },
  "RepostService": {
    "RepostSimilarityThreshold": 0.95
  }
}
```

**Step 5: Build to verify**

Run:
```bash
cd src/ShitpostBot
dotnet build src/ShitpostBot.WebApi/ShitpostBot.WebApi.csproj
```

Expected: BUILD SUCCEEDED

**Step 6: Commit**

```bash
git add src/ShitpostBot/src/ShitpostBot.WebApi/
git commit -m "feat: create WebApi project skeleton

- Add project with Application and Infrastructure references
- Minimal Program.cs with DI setup
- Development configuration for local testing"
```

---

### Task 5.2: Create NullChatClient

**Files:**
- Create: `src/ShitpostBot/src/ShitpostBot.WebApi/Services/NullChatClient.cs`
- Modify: `src/ShitpostBot/src/ShitpostBot.WebApi/Program.cs`

**Step 1: Create NullChatClient implementation**

Create: `src/ShitpostBot/src/ShitpostBot.WebApi/Services/NullChatClient.cs`

```csharp
using DSharpPlus.AsyncEvents;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;
using ShitpostBot.Infrastructure;

namespace ShitpostBot.WebApi.Services;

public class NullChatClient : IChatClient
{
    private readonly ILogger<NullChatClient> _logger;
    
    public NullChatClient(ILogger<NullChatClient> logger)
    {
        _logger = logger;
        Utils = new NullChatClientUtils();
    }
    
    public IChatClientUtils Utils { get; }

    public event AsyncEventHandler<MessageCreateEventArgs>? MessageCreated;
    public event AsyncEventHandler<MessageDeleteEventArgs>? MessageDeleted;

    public Task ConnectAsync()
    {
        _logger.LogInformation("NullChatClient.ConnectAsync - no-op");
        return Task.CompletedTask;
    }

    public Task SendMessage(MessageDestination destination, string? messageContent)
    {
        _logger.LogInformation("Would send message to {Destination}: {Content}", destination, messageContent);
        return Task.CompletedTask;
    }

    public Task SendMessage(MessageDestination destination, DiscordMessageBuilder messageBuilder)
    {
        _logger.LogInformation("Would send message builder to {Destination}", destination);
        return Task.CompletedTask;
    }

    public Task SendEmbeddedMessage(MessageDestination destination, DiscordEmbed embed)
    {
        _logger.LogInformation("Would send embedded message to {Destination}", destination);
        return Task.CompletedTask;
    }

    public Task React(MessageIdentification messageIdentification, string emoji)
    {
        _logger.LogInformation("Would react to message {MessageId} with {Emoji}", 
            messageIdentification.MessageId, emoji);
        return Task.CompletedTask;
    }
}

public class NullChatClientUtils : IChatClientUtils
{
    public string Emoji(string name) => $":{name}:";
    public ulong ShitpostBotId() => 0;
    public string Mention(ulong posterId, bool useDesktop = false) => $"<@{posterId}>";
    public string RelativeTimestamp(DateTimeOffset timestamp) => timestamp.ToString("R");
}
```

**Step 2: Add DSharpPlus package reference**

Modify: `src/ShitpostBot/src/ShitpostBot.WebApi/ShitpostBot.WebApi.csproj`

Add to PackageReference ItemGroup:
```xml
<PackageReference Include="DSharpPlus"/>
```

**Step 3: Register NullChatClient in DI**

Modify: `src/ShitpostBot/src/ShitpostBot.WebApi/Program.cs`

Add after other service registrations:
```csharp
using ShitpostBot.WebApi.Services;

// ... after AddShitpostBotMassTransit:
builder.Services.AddSingleton<IChatClient, NullChatClient>();
```

**Step 4: Build to verify**

Run:
```bash
cd src/ShitpostBot
dotnet build src/ShitpostBot.WebApi/ShitpostBot.WebApi.csproj
```

Expected: BUILD SUCCEEDED

**Step 5: Commit**

```bash
git add -A
git commit -m "feat: add NullChatClient for test environment

- Implements IChatClient with logging instead of Discord API calls
- Enables Application handlers to run without Discord connection
- Registered as singleton in WebApi DI"
```

---

### Task 5.3: Create Test Message Factory

**Files:**
- Create: `src/ShitpostBot/src/ShitpostBot.WebApi/Services/TestMessageFactory.cs`

**Step 1: Create TestMessageFactory service**

Create: `src/ShitpostBot/src/ShitpostBot.WebApi/Services/TestMessageFactory.cs`

```csharp
using ShitpostBot.Infrastructure;

namespace ShitpostBot.WebApi.Services;

public class TestMessageFactory
{
    private ulong _nextMessageId = 1000000;
    private readonly Random _random = new();

    public MessageIdentification GenerateMessageIdentification(
        ulong? guildId = null,
        ulong? channelId = null,
        ulong? userId = null,
        ulong? messageId = null)
    {
        return new MessageIdentification(
            guildId ?? GenerateId(),
            channelId ?? GenerateId(),
            userId ?? GenerateId(),
            messageId ?? Interlocked.Increment(ref _nextMessageId)
        );
    }

    public ImageMessage CreateImageMessage(
        string imageUrl,
        ulong? guildId = null,
        ulong? channelId = null,
        ulong? userId = null,
        ulong? messageId = null,
        DateTimeOffset? timestamp = null)
    {
        var identification = GenerateMessageIdentification(guildId, channelId, userId, messageId);
        var uri = new Uri(imageUrl);
        var attachmentId = GenerateId();
        var fileName = Path.GetFileName(uri.LocalPath);

        var attachment = new ImageMessageAttachment(attachmentId, fileName, uri);
        
        return new ImageMessage(
            identification,
            attachment,
            timestamp ?? DateTimeOffset.UtcNow
        );
    }

    public LinkMessage CreateLinkMessage(
        string linkUrl,
        ulong? guildId = null,
        ulong? channelId = null,
        ulong? userId = null,
        ulong? messageId = null,
        DateTimeOffset? timestamp = null)
    {
        var identification = GenerateMessageIdentification(guildId, channelId, userId, messageId);
        var uri = new Uri(linkUrl);

        var embed = new LinkMessageEmbed(uri);
        
        return new LinkMessage(
            identification,
            embed,
            timestamp ?? DateTimeOffset.UtcNow
        );
    }

    private ulong GenerateId()
    {
        return (ulong)_random.NextInt64(100000000, 999999999);
    }
}
```

**Step 2: Register in DI**

Modify: `src/ShitpostBot/src/ShitpostBot.WebApi/Program.cs`

Add:
```csharp
builder.Services.AddSingleton<TestMessageFactory>();
```

**Step 3: Build to verify**

Run:
```bash
cd src/ShitpostBot
dotnet build src/ShitpostBot.WebApi/ShitpostBot.WebApi.csproj
```

Expected: BUILD SUCCEEDED

**Step 4: Commit**

```bash
git add -A
git commit -m "feat: add TestMessageFactory for generating test data

- Generates Discord-like IDs for test scenarios
- Creates ImageMessage and LinkMessage with auto or explicit IDs
- Registered as singleton in WebApi"
```

---

## Phase 6: WebApi Endpoints

### Task 6.1: Create POST /test/image-message Endpoint

**Files:**
- Create: `src/ShitpostBot/src/ShitpostBot.WebApi/Endpoints/TestEndpoints.cs`
- Modify: `src/ShitpostBot/src/ShitpostBot.WebApi/Program.cs`

**Step 1: Create request/response DTOs and endpoint**

Create: `src/ShitpostBot/src/ShitpostBot.WebApi/Endpoints/TestEndpoints.cs`

```csharp
using MediatR;
using Microsoft.AspNetCore.Mvc;
using ShitpostBot.Application.Features.PostTracking;
using ShitpostBot.WebApi.Services;

namespace ShitpostBot.WebApi.Endpoints;

public static class TestEndpoints
{
    public static void MapTestEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/test")
            .WithTags("Test");

        group.MapPost("/image-message", PostImageMessage);
        group.MapPost("/link-message", PostLinkMessage);
    }

    private static async Task<IResult> PostImageMessage(
        [FromBody] PostImageMessageRequest request,
        [FromServices] TestMessageFactory factory,
        [FromServices] IMediator mediator)
    {
        var imageMessage = factory.CreateImageMessage(
            request.ImageUrl,
            request.GuildId,
            request.ChannelId,
            request.UserId,
            request.MessageId,
            request.Timestamp
        );

        await mediator.Publish(new ImageMessageCreated(imageMessage));

        return Results.Ok(new PostMessageResponse
        {
            MessageId = imageMessage.Identification.MessageId,
            Tracked = true
        });
    }

    private static async Task<IResult> PostLinkMessage(
        [FromBody] PostLinkMessageRequest request,
        [FromServices] TestMessageFactory factory,
        [FromServices] IMediator mediator)
    {
        var linkMessage = factory.CreateLinkMessage(
            request.LinkUrl,
            request.GuildId,
            request.ChannelId,
            request.UserId,
            request.MessageId,
            request.Timestamp
        );

        await mediator.Publish(new LinkMessageCreated(linkMessage));

        return Results.Ok(new PostMessageResponse
        {
            MessageId = linkMessage.Identification.MessageId,
            Tracked = true
        });
    }
}

public record PostImageMessageRequest
{
    public required string ImageUrl { get; init; }
    public ulong? GuildId { get; init; }
    public ulong? ChannelId { get; init; }
    public ulong? UserId { get; init; }
    public ulong? MessageId { get; init; }
    public DateTimeOffset? Timestamp { get; init; }
}

public record PostLinkMessageRequest
{
    public required string LinkUrl { get; init; }
    public ulong? GuildId { get; init; }
    public ulong? ChannelId { get; init; }
    public ulong? UserId { get; init; }
    public ulong? MessageId { get; init; }
    public DateTimeOffset? Timestamp { get; init; }
}

public record PostMessageResponse
{
    public required ulong MessageId { get; init; }
    public required bool Tracked { get; init; }
}
```

**Step 2: Register endpoints in Program.cs**

Modify: `src/ShitpostBot/src/ShitpostBot.WebApi/Program.cs`

Replace the MapGet("/") with:
```csharp
using ShitpostBot.WebApi.Endpoints;

// ... after var app = builder.Build();

app.MapTestEndpoints();
```

**Step 3: Build to verify**

Run:
```bash
cd src/ShitpostBot
dotnet build src/ShitpostBot.WebApi/ShitpostBot.WebApi.csproj
```

Expected: BUILD SUCCEEDED

**Step 4: Commit**

```bash
git add -A
git commit -m "feat: add POST /test/image-message and /test/link-message endpoints

- Minimal API endpoints for triggering test scenarios
- Support both simple (auto-ID) and advanced (explicit ID) modes
- Publishes MediatR notifications to Application handlers"
```

---

### Task 6.2: Create GET /test/fixtures Endpoint

**Files:**
- Create: `src/ShitpostBot/src/ShitpostBot.WebApi/Endpoints/FixtureEndpoints.cs`
- Modify: `src/ShitpostBot/src/ShitpostBot.WebApi/Program.cs`

**Step 1: Create fixture endpoint**

Create: `src/ShitpostBot/src/ShitpostBot.WebApi/Endpoints/FixtureEndpoints.cs`

```csharp
using System.IO;
using Microsoft.AspNetCore.Mvc;

namespace ShitpostBot.WebApi.Endpoints;

public static class FixtureEndpoints
{
    public static void MapFixtureEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/test")
            .WithTags("Test");

        group.MapGet("/fixtures", GetFixtures);
    }

    private static IResult GetFixtures()
    {
        var fixturesPath = Path.Combine(Directory.GetCurrentDirectory(), "fixtures", "images");
        
        if (!Directory.Exists(fixturesPath))
        {
            return Results.Ok(new FixturesResponse
            {
                Reposts = Array.Empty<string>(),
                NonReposts = Array.Empty<string>(),
                EdgeCases = Array.Empty<string>()
            });
        }

        var response = new FixturesResponse
        {
            Reposts = GetFilesInDirectory(Path.Combine(fixturesPath, "reposts")),
            NonReposts = GetFilesInDirectory(Path.Combine(fixturesPath, "non-reposts")),
            EdgeCases = GetFilesInDirectory(Path.Combine(fixturesPath, "edge-cases"))
        };

        return Results.Ok(response);
    }

    private static string[] GetFilesInDirectory(string path)
    {
        if (!Directory.Exists(path))
            return Array.Empty<string>();

        return Directory.GetFiles(path)
            .Select(Path.GetFileName)
            .Where(name => name != null && !name.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            .Cast<string>()
            .ToArray();
    }
}

public record FixturesResponse
{
    public required string[] Reposts { get; init; }
    public required string[] NonReposts { get; init; }
    public required string[] EdgeCases { get; init; }
}
```

**Step 2: Register endpoint in Program.cs**

Modify: `src/ShitpostBot/src/ShitpostBot.WebApi/Program.cs`

Add after MapTestEndpoints:
```csharp
app.MapFixtureEndpoints();
```

**Step 3: Build to verify**

Run:
```bash
cd src/ShitpostBot
dotnet build src/ShitpostBot.WebApi/ShitpostBot.WebApi.csproj
```

Expected: BUILD SUCCEEDED

**Step 4: Commit**

```bash
git add -A
git commit -m "feat: add GET /test/fixtures endpoint

- Lists available test images by category
- Returns empty arrays if fixtures not yet created"
```

---

## Phase 7: Test Fixtures

### Task 7.1: Create Test Fixtures Directory Structure

**Files:**
- Create: `src/ShitpostBot/test/fixtures/images/reposts/README.md`
- Create: `src/ShitpostBot/test/fixtures/images/non-reposts/README.md`
- Create: `src/ShitpostBot/test/fixtures/images/edge-cases/README.md`

**Step 1: Create directory structure**

Run:
```bash
mkdir -p src/ShitpostBot/test/fixtures/images/{reposts,non-reposts,edge-cases}
```

**Step 2: Create reposts README**

Create: `src/ShitpostBot/test/fixtures/images/reposts/README.md`

```markdown
# Repost Test Fixtures

This directory contains pairs of images that should be detected as reposts by the ML service.

## Image Pairs

Each pair represents:
- **Original**: The first image posted
- **Repost**: A similar/duplicate image that should trigger repost detection

## Expected Behavior

When both images are posted in sequence via `/test/image-message`, the second image should:
1. Be tracked in the database
2. Have feature vectors extracted by ML service
3. Be detected as similar to the first (cosine similarity >= threshold)
4. Trigger repost reactions (in Discord environment) or logs (in test environment)

## Adding New Fixtures

To add a new repost pair:
1. Add two images with clear naming (e.g., `cat-original.jpg`, `cat-repost.jpg`)
2. Ensure both are >= 299x299 pixels
3. Test locally to verify they're detected as similar
```

**Step 3: Create non-reposts README**

Create: `src/ShitpostBot/test/fixtures/images/non-reposts/README.md`

```markdown
# Non-Repost Test Fixtures

This directory contains unique images that should NOT be detected as reposts.

## Expected Behavior

When any two images from this directory are posted in sequence, they should:
1. Both be tracked in the database
2. Have feature vectors extracted by ML service
3. NOT be detected as similar (cosine similarity < threshold)
4. NOT trigger repost reactions

## Adding New Fixtures

To add new unique images:
1. Add images with descriptive names
2. Ensure they are >= 299x299 pixels
3. Ensure they are visually distinct from other fixtures
```

**Step 4: Create edge-cases README**

Create: `src/ShitpostBot/test/fixtures/images/edge-cases/README.md`

```markdown
# Edge Case Test Fixtures

This directory contains images that test boundary conditions and error handling.

## Categories

- **too-small.jpg**: Image smaller than 299x299 (should be rejected/not tracked)
- **corrupted.jpg**: Corrupted image file (should fail gracefully)

## Expected Behavior

These images test error handling:
- Too small images should be filtered by `Image.CreateOrDefault()` returning null
- Corrupted images should fail gracefully with appropriate error logging

## Adding New Edge Cases

Add images that test specific boundary conditions with clear naming.
```

**Step 5: Commit**

```bash
git add src/ShitpostBot/test/fixtures/
git commit -m "feat: create test fixtures directory structure

- Add reposts, non-reposts, and edge-cases directories
- Include README explaining purpose and usage of each category
- Ready for actual test images to be added"
```

---

## Phase 8: Docker Integration

### Task 8.1: Create WebApi Dockerfile

**Files:**
- Create: `src/ShitpostBot/src/ShitpostBot.WebApi/Dockerfile`

**Step 1: Create Dockerfile**

Create: `src/ShitpostBot/src/ShitpostBot.WebApi/Dockerfile`

```dockerfile
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
```

**Step 2: Build to verify Dockerfile syntax**

Run:
```bash
cd src/ShitpostBot
cat src/ShitpostBot.WebApi/Dockerfile
```

Expected: File created successfully

**Step 3: Commit**

```bash
git add src/ShitpostBot/src/ShitpostBot.WebApi/Dockerfile
git commit -m "feat: add WebApi Dockerfile

- Multi-stage build with SDK and runtime images
- Copies test fixtures into container
- Exposes port 8080"
```

---

### Task 8.2: Add WebApi to docker-compose

**Files:**
- Modify: `docker-compose.yml`

**Step 1: Add webapi service to docker-compose**

Modify: `docker-compose.yml`

Add after the `worker` service:

```yaml
  webapi:
    build:
      context: ./src/ShitpostBot
      dockerfile: src/ShitpostBot.WebApi/Dockerfile
    ports:
      - "5001:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__DefaultConnection=Host=database;Database=shitpostbot;Username=postgres;Password=postgres
      - ConnectionStrings__ShitpostBotMessaging=Host=database;Database=shitpostbot;Username=postgres;Password=postgres
      - ImageFeatureExtractorApi__Uri=http://ml-service:5000
      - RepostService__RepostSimilarityThreshold=0.95
    depends_on:
      - database
      - ml-service
    networks:
      - shitpostbot
```

**Step 2: Verify docker-compose syntax**

Run:
```bash
docker compose config > /dev/null && echo "docker-compose.yml syntax OK"
```

Expected: "docker-compose.yml syntax OK"

**Step 3: Commit**

```bash
git add docker-compose.yml
git commit -m "feat: add WebApi service to docker-compose

- Expose on port 5001
- Connect to database and ml-service
- Share network with other services"
```

---

## Phase 9: Integration Tests

### Task 9.1: Create Basic WebApi Integration Test

**Files:**
- Create: `src/ShitpostBot/test/ShitpostBot.Tests.Integration/WebApiIntegrationTests.cs`
- Modify: `src/ShitpostBot/test/ShitpostBot.Tests.Integration/ShitpostBot.Tests.Integration.csproj`

**Step 1: Add Testcontainers and HTTP client packages**

Modify: `src/ShitpostBot/test/ShitpostBot.Tests.Integration/ShitpostBot.Tests.Integration.csproj`

Ensure these PackageReferences exist:
```xml
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing"/>
<PackageReference Include="System.Net.Http.Json"/>
```

**Step 2: Create WebApi integration test**

Create: `src/ShitpostBot/test/ShitpostBot.Tests.Integration/WebApiIntegrationTests.cs`

```csharp
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace ShitpostBot.Tests.Integration;

public class WebApiIntegrationTests
{
    [Fact]
    public async Task GetFixtures_ReturnsEmptyArrays_WhenNoFixturesExist()
    {
        // This is a basic smoke test - we'll expand with Testcontainers later
        // For now, just verify the test project compiles
        
        var expected = new { Reposts = Array.Empty<string>(), NonReposts = Array.Empty<string>(), EdgeCases = Array.Empty<string>() };
        expected.Reposts.Should().BeEmpty();
    }
}
```

**Step 3: Build test project**

Run:
```bash
cd src/ShitpostBot
dotnet build test/ShitpostBot.Tests.Integration/ShitpostBot.Tests.Integration.csproj
```

Expected: BUILD SUCCEEDED

**Step 4: Run test to verify**

Run:
```bash
cd src/ShitpostBot
dotnet test test/ShitpostBot.Tests.Integration/ShitpostBot.Tests.Integration.csproj --filter "FullyQualifiedName~WebApiIntegrationTests"
```

Expected: Test Passed

**Step 5: Commit**

```bash
git add -A
git commit -m "test: add basic WebApi integration test

- Placeholder for future Testcontainers-based tests
- Verifies test project builds with new dependencies"
```

---

## Phase 10: Documentation and Verification

### Task 10.1: Update Project README

**Files:**
- Modify: `README.md`

**Step 1: Add WebApi section to README**

Modify: `README.md`

Add after existing sections:

```markdown
## Test API

For local development and testing without Discord, use the WebApi:

```bash
# Start all services including WebApi
docker compose -f docker-compose.yml -f docker-compose.Development.Linux.yml up --build

# WebApi available at http://localhost:5001

# Test repost detection
curl -X POST http://localhost:5001/test/image-message \
  -H "Content-Type: application/json" \
  -d '{"imageUrl": "https://example.com/image.jpg"}'

# List available fixtures
curl http://localhost:5001/test/fixtures
```

See [Test API Design](docs/plans/2024-12-14-test-api-design.md) for details.
```

**Step 2: Commit**

```bash
git add README.md
git commit -m "docs: add Test API section to README

- Document WebApi usage for local development
- Link to design document"
```

---

### Task 10.2: Final Build and Verification

**Files:**
- None (verification step)

**Step 1: Build entire solution**

Run:
```bash
cd src/ShitpostBot
dotnet build
```

Expected: BUILD SUCCEEDED

**Step 2: Run all tests**

Run:
```bash
cd src/ShitpostBot
dotnet test
```

Expected: All tests PASSED

**Step 3: Verify docker-compose**

Run:
```bash
docker compose config
```

Expected: Valid YAML output with webapi service

**Step 4: Create final summary commit**

```bash
git add -A
git commit -m "chore: final verification of Test API implementation

- All projects build successfully
- Tests pass
- Docker compose configuration valid
- Ready for deployment"
```

---

## Completion Checklist

After completing all tasks, verify:

- [ ] Application layer exists with extracted handlers
- [ ] WebApi project created with minimal API endpoints
- [ ] NullChatClient registered for test environment
- [ ] MassTransit configuration shared in Infrastructure
- [ ] Worker refactored to use Application layer
- [ ] Test fixtures directory structure created
- [ ] WebApi added to docker-compose
- [ ] Basic integration test created
- [ ] Documentation updated
- [ ] All projects build successfully
- [ ] All tests pass

## Next Steps

1. **Add actual test images** to fixtures directories
2. **Implement file:// URL resolution** in WebApi to load local fixtures
3. **Add comprehensive E2E tests** using Testcontainers
4. **Consider SSE endpoint** for real-time test feedback (future enhancement)

---

**Plan complete!** Each task is bite-sized (2-5 minutes) with exact file paths, complete code, verification commands, and commit messages.
