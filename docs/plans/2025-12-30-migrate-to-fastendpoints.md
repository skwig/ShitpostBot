# Migrate to FastEndpoints with REPR Pattern Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Replace minimal API endpoints with FastEndpoints library and adopt REPR pattern (Request-Endpoint-Response) by returning named response classes instead of anonymous types.

**Architecture:** Migrate from ASP.NET minimal API pattern to FastEndpoints, which provides a more structured, class-based approach. Each endpoint becomes a separate class with strongly-typed request/response DTOs. The REPR pattern improves testability, maintainability, and API documentation.

**Tech Stack:** FastEndpoints 5.x, .NET 10.0, MediatR (existing), MassTransit (existing)

---

## Task 1: Add FastEndpoints Package

**Files:**
- Modify: `src/ShitpostBot/Directory.Packages.props`

**Step 1: Add FastEndpoints package reference**

Add to the `<ItemGroup>` section:

```xml
<PackageVersion Include="FastEndpoints" Version="5.33.0"/>
```

**Step 2: Commit**

```bash
git add src/ShitpostBot/Directory.Packages.props
git commit -m "chore: add FastEndpoints package"
```

---

## Task 2: Add FastEndpoints Package to WebApi Project

**Files:**
- Modify: `src/ShitpostBot/src/ShitpostBot.WebApi/ShitpostBot.WebApi.csproj`

**Step 1: Add package reference**

Add to existing `<ItemGroup>` with PackageReference entries:

```xml
<PackageReference Include="FastEndpoints"/>
```

**Step 2: Commit**

```bash
git add src/ShitpostBot/src/ShitpostBot.WebApi/ShitpostBot.WebApi.csproj
git commit -m "chore: reference FastEndpoints in WebApi project"
```

---

## Task 3: Create GetActionsResponse DTO

**Files:**
- Create: `src/ShitpostBot/src/ShitpostBot.WebApi/Endpoints/GetActionsResponse.cs`

**Step 1: Create named response class for GetActions endpoint**

Replace the anonymous type in line 120-125 of TestEndpoints.cs with:

```csharp
namespace ShitpostBot.WebApi.Endpoints;

public record GetActionsResponse
{
    public required ulong MessageId { get; init; }
    public required IReadOnlyList<TestAction> Actions { get; init; }
    public required long WaitedMs { get; init; }
}
```

**Step 2: Commit**

```bash
git add src/ShitpostBot/src/ShitpostBot.WebApi/Endpoints/GetActionsResponse.cs
git commit -m "feat: add GetActionsResponse DTO for REPR pattern"
```

---

## Task 4: Create PostImageMessage FastEndpoint

**Files:**
- Create: `src/ShitpostBot/src/ShitpostBot.WebApi/Endpoints/PostImageMessageEndpoint.cs`

**Step 1: Create FastEndpoint class**

```csharp
using FastEndpoints;
using MediatR;
using ShitpostBot.Application.Features.PostTracking;
using ShitpostBot.WebApi.Services;

namespace ShitpostBot.WebApi.Endpoints;

public class PostImageMessageEndpoint : Endpoint<PostImageMessageRequest, PostMessageResponse>
{
    public override void Configure()
    {
        Post("/test/image-message");
        AllowAnonymous();
        Tags("Test");
    }

    public override async Task HandleAsync(PostImageMessageRequest request, CancellationToken ct)
    {
        var factory = Resolve<TestMessageFactory>();
        var mediator = Resolve<IMediator>();

        var imageMessage = factory.CreateImageMessage(
            request.ImageUrl,
            request.GuildId,
            request.ChannelId,
            request.UserId,
            request.MessageId,
            request.Timestamp
        );

        await mediator.Publish(new ImageMessageCreated(imageMessage), ct);

        await SendOkAsync(new PostMessageResponse
        {
            MessageId = imageMessage.Identification.MessageId,
            Tracked = true
        }, ct);
    }
}
```

**Step 2: Commit**

```bash
git add src/ShitpostBot/src/ShitpostBot.WebApi/Endpoints/PostImageMessageEndpoint.cs
git commit -m "feat: create PostImageMessage FastEndpoint"
```

---

## Task 5: Create PostLinkMessage FastEndpoint

**Files:**
- Create: `src/ShitpostBot/src/ShitpostBot.WebApi/Endpoints/PostLinkMessageEndpoint.cs`

**Step 1: Create FastEndpoint class**

```csharp
using FastEndpoints;
using MediatR;
using ShitpostBot.Application.Features.PostTracking;
using ShitpostBot.WebApi.Services;

namespace ShitpostBot.WebApi.Endpoints;

public class PostLinkMessageEndpoint : Endpoint<PostLinkMessageRequest, PostMessageResponse>
{
    public override void Configure()
    {
        Post("/test/link-message");
        AllowAnonymous();
        Tags("Test");
    }

    public override async Task HandleAsync(PostLinkMessageRequest request, CancellationToken ct)
    {
        var factory = Resolve<TestMessageFactory>();
        var mediator = Resolve<IMediator>();

        var linkMessage = factory.CreateLinkMessage(
            request.LinkUrl,
            request.GuildId,
            request.ChannelId,
            request.UserId,
            request.MessageId,
            request.Timestamp
        );

        await mediator.Publish(new LinkMessageCreated(linkMessage), ct);

        await SendOkAsync(new PostMessageResponse
        {
            MessageId = linkMessage.Identification.MessageId,
            Tracked = true
        }, ct);
    }
}
```

**Step 2: Commit**

```bash
git add src/ShitpostBot/src/ShitpostBot.WebApi/Endpoints/PostLinkMessageEndpoint.cs
git commit -m "feat: create PostLinkMessage FastEndpoint"
```

---

## Task 6: Create PostBotCommand FastEndpoint

**Files:**
- Create: `src/ShitpostBot/src/ShitpostBot.WebApi/Endpoints/PostBotCommandEndpoint.cs`

**Step 1: Create FastEndpoint class**

```csharp
using FastEndpoints;
using MediatR;
using ShitpostBot.Application.Features.BotCommands;
using ShitpostBot.Infrastructure;
using ShitpostBot.WebApi.Services;

namespace ShitpostBot.WebApi.Endpoints;

public class PostBotCommandEndpoint : Endpoint<PostBotCommandRequest, PostMessageResponse>
{
    public override void Configure()
    {
        Post("/test/bot-command");
        AllowAnonymous();
        Tags("Test");
    }

    public override async Task HandleAsync(PostBotCommandRequest request, CancellationToken ct)
    {
        var factory = Resolve<TestMessageFactory>();
        var mediator = Resolve<IMediator>();

        var commandMessageIdentification = factory.GenerateMessageIdentification(
            request.GuildId,
            request.ChannelId,
            request.UserId,
            request.MessageId
        );

        MessageIdentification? referencedMessageIdentification = null;
        if (request.ReferencedMessageId.HasValue)
        {
            referencedMessageIdentification = factory.GenerateMessageIdentification(
                request.GuildId,
                request.ChannelId,
                request.ReferencedUserId,
                request.ReferencedMessageId
            );
        }

        await mediator.Send(new ExecuteBotCommand(
            commandMessageIdentification,
            referencedMessageIdentification,
            new BotCommand(request.Command)
        ), ct);

        await SendOkAsync(new PostMessageResponse
        {
            MessageId = commandMessageIdentification.MessageId,
            Tracked = true
        }, ct);
    }
}
```

**Step 2: Commit**

```bash
git add src/ShitpostBot/src/ShitpostBot.WebApi/Endpoints/PostBotCommandEndpoint.cs
git commit -m "feat: create PostBotCommand FastEndpoint"
```

---

## Task 7: Create GetActions FastEndpoint

**Files:**
- Create: `src/ShitpostBot/src/ShitpostBot.WebApi/Endpoints/GetActionsEndpoint.cs`

**Step 1: Create request DTO**

We need a request DTO that captures the route parameter and query parameters:

```csharp
using FastEndpoints;
using ShitpostBot.WebApi.Services;
using System.Diagnostics;

namespace ShitpostBot.WebApi.Endpoints;

public class GetActionsRequest
{
    public ulong MessageId { get; set; }
    public int ExpectedCount { get; set; } = 0;
    public int Timeout { get; set; } = 10000;
}

public class GetActionsEndpoint : Endpoint<GetActionsRequest, GetActionsResponse>
{
    public override void Configure()
    {
        Get("/test/actions/{MessageId}");
        AllowAnonymous();
        Tags("Test");
    }

    public override async Task HandleAsync(GetActionsRequest request, CancellationToken ct)
    {
        var store = Resolve<IBotActionStore>();
        var stopwatch = Stopwatch.StartNew();

        var actions = await store.WaitForActionsAsync(
            request.MessageId,
            request.ExpectedCount,
            TimeSpan.FromMilliseconds(request.Timeout)
        );

        await SendOkAsync(new GetActionsResponse
        {
            MessageId = request.MessageId,
            Actions = actions,
            WaitedMs = stopwatch.ElapsedMilliseconds
        }, ct);
    }
}
```

**Step 2: Commit**

```bash
git add src/ShitpostBot/src/ShitpostBot.WebApi/Endpoints/GetActionsEndpoint.cs
git commit -m "feat: create GetActions FastEndpoint with named response"
```

---

## Task 8: Update Program.cs to Use FastEndpoints

**Files:**
- Modify: `src/ShitpostBot/src/ShitpostBot.WebApi/Program.cs`

**Step 1: Replace minimal API setup with FastEndpoints**

Replace the entire contents with:

```csharp
using FastEndpoints;
using ShitpostBot.Application;
using ShitpostBot.Application.Features.Repost;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Services;
using ShitpostBot.WebApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddShitpostBotInfrastructure(builder.Configuration);
builder.Services.AddShitpostBotApplication(builder.Configuration);
builder.Services.AddShitpostBotMassTransit(builder.Configuration, x =>
{
    x.AddConsumer<EvaluateRepost_ImagePostTrackedHandler>();
    x.AddConsumer<EvaluateRepost_LinkPostTrackedHandler>();
});
builder.Services.AddSingleton<IChatClient, NullChatClient>();
builder.Services.AddSingleton<TestMessageFactory>();
builder.Services.AddSingleton<IBotActionStore, BotActionStore>();

// Add FastEndpoints
builder.Services.AddFastEndpoints();

var app = builder.Build();

// Use FastEndpoints
app.UseFastEndpoints();

app.MapGet("/", () => "ShitpostBot WebApi - Test Harness");

app.Run();
```

**Step 2: Commit**

```bash
git add src/ShitpostBot/src/ShitpostBot.WebApi/Program.cs
git commit -m "refactor: replace minimal API with FastEndpoints"
```

---

## Task 9: Delete Old TestEndpoints.cs

**Files:**
- Delete: `src/ShitpostBot/src/ShitpostBot.WebApi/Endpoints/TestEndpoints.cs`

**Step 1: Move DTOs to separate files first**

The DTOs (PostImageMessageRequest, PostLinkMessageRequest, PostBotCommandRequest, PostMessageResponse) are still used by the new endpoints. They're already in TestEndpoints.cs, but we need to keep them accessible.

Actually, looking at the new endpoint files, they reference these types. Let's create separate files for the request/response DTOs that are shared.

**Step 2: Create PostImageMessageRequest.cs**

Create: `src/ShitpostBot/src/ShitpostBot.WebApi/Endpoints/PostImageMessageRequest.cs`

```csharp
namespace ShitpostBot.WebApi.Endpoints;

public record PostImageMessageRequest
{
    public required string ImageUrl { get; init; }
    public ulong? GuildId { get; init; }
    public ulong? ChannelId { get; init; }
    public ulong? UserId { get; init; }
    public ulong? MessageId { get; init; }
    public DateTimeOffset? Timestamp { get; init; }
}
```

**Step 3: Create PostLinkMessageRequest.cs**

Create: `src/ShitpostBot/src/ShitpostBot.WebApi/Endpoints/PostLinkMessageRequest.cs`

```csharp
namespace ShitpostBot.WebApi.Endpoints;

public record PostLinkMessageRequest
{
    public required string LinkUrl { get; init; }
    public ulong? GuildId { get; init; }
    public ulong? ChannelId { get; init; }
    public ulong? UserId { get; init; }
    public ulong? MessageId { get; init; }
    public DateTimeOffset? Timestamp { get; init; }
}
```

**Step 4: Create PostBotCommandRequest.cs**

Create: `src/ShitpostBot/src/ShitpostBot.WebApi/Endpoints/PostBotCommandRequest.cs`

```csharp
namespace ShitpostBot.WebApi.Endpoints;

public record PostBotCommandRequest
{
    public required string Command { get; init; }
    public ulong? GuildId { get; init; }
    public ulong? ChannelId { get; init; }
    public ulong? UserId { get; init; }
    public ulong? MessageId { get; init; }
    public ulong? ReferencedMessageId { get; init; }
    public ulong? ReferencedUserId { get; init; }
}
```

**Step 5: Create PostMessageResponse.cs**

Create: `src/ShitpostBot/src/ShitpostBot.WebApi/Endpoints/PostMessageResponse.cs`

```csharp
namespace ShitpostBot.WebApi.Endpoints;

public record PostMessageResponse
{
    public required ulong MessageId { get; init; }
    public required bool Tracked { get; init; }
}
```

**Step 6: Now delete TestEndpoints.cs**

```bash
git rm src/ShitpostBot/src/ShitpostBot.WebApi/Endpoints/TestEndpoints.cs
```

**Step 7: Commit all changes**

```bash
git add src/ShitpostBot/src/ShitpostBot.WebApi/Endpoints/PostImageMessageRequest.cs
git add src/ShitpostBot/src/ShitpostBot.WebApi/Endpoints/PostLinkMessageRequest.cs
git add src/ShitpostBot/src/ShitpostBot.WebApi/Endpoints/PostBotCommandRequest.cs
git add src/ShitpostBot/src/ShitpostBot.WebApi/Endpoints/PostMessageResponse.cs
git commit -m "refactor: extract DTOs to separate files and remove old TestEndpoints"
```

---

## Task 10: Build the Project

**Files:**
- N/A (build verification)

**Step 1: Build the solution**

Run from repository root:

```bash
dotnet build src/ShitpostBot/ShitpostBot.slnx
```

Expected: Build succeeds with no errors.

**Step 2: If build fails, fix any compilation errors**

Common issues:
- Missing using statements
- Type mismatches
- Incorrect namespaces

**Step 3: Commit any fixes**

```bash
git add .
git commit -m "fix: resolve compilation errors"
```

---

## Task 11: Run E2E Tests

**Files:**
- N/A (test verification)

**Step 1: Run E2E test suite**

Run from repository root:

```bash
./test/e2e/run-e2e-tests.sh
```

Expected: All tests pass (HTTP 200 responses, correct bot actions, proper response structure).

**Step 2: Verify test output**

Check that:
- Scenario 1 (unrelated images): ✓ Passes
- Scenario 2 (reposting downscaled): ✓ Passes  
- Scenario 3 (different formats): ✓ Passes
- Scenario 4 (bot command): ✓ Passes
- Scenario 5 (semantic search): ✓ Passes

**Step 3: If tests fail, debug and fix**

Potential issues:
- FastEndpoints routing not matching expected paths
- Request/response serialization differences
- Missing dependency injection registrations
- Query parameter binding issues in GetActionsEndpoint

**Step 4: Commit any fixes**

```bash
git add .
git commit -m "fix: resolve E2E test failures"
```

---

## Task 12: Verify Anonymous Type Elimination

**Files:**
- N/A (verification step)

**Step 1: Search for anonymous type returns in WebApi**

```bash
grep -r "Results.Ok(new {" src/ShitpostBot/src/ShitpostBot.WebApi/
```

Expected: No matches (all anonymous types replaced with named classes).

**Step 2: Search for anonymous object initializers in responses**

```bash
grep -r "new {$" src/ShitpostBot/src/ShitpostBot.WebApi/Endpoints/
```

Expected: No matches in endpoint files.

**Step 3: Verify all endpoints use FastEndpoints base classes**

```bash
grep -r "class.*Endpoint" src/ShitpostBot/src/ShitpostBot.WebApi/Endpoints/ | grep -v "^.*: Endpoint"
```

Expected: No matches (all endpoint classes inherit from FastEndpoints base).

---

## Task 13: Final Commit and Summary

**Files:**
- N/A (documentation step)

**Step 1: Review all changes**

```bash
git log --oneline feature/fizzbuzz ^main
```

Verify all commits are present and descriptive.

**Step 2: Run final validation**

```bash
dotnet build src/ShitpostBot/ShitpostBot.slnx && ./test/e2e/run-e2e-tests.sh
```

Expected: Build succeeds, all E2E tests pass.

**Step 3: Document the migration**

The migration is complete. Summary of changes:
- ✅ Added FastEndpoints package (v5.33.0)
- ✅ Migrated 4 endpoints to FastEndpoints pattern
- ✅ Adopted REPR pattern with named response classes
- ✅ Replaced anonymous type in GetActions with GetActionsResponse
- ✅ All E2E tests passing
- ✅ No anonymous types remaining in API responses

Next steps for the user:
- Review the new endpoint structure
- Consider adding FastEndpoints validators if needed
- Consider adding Swagger/OpenAPI documentation with FastEndpoints.Swagger

---

## Notes

- **FastEndpoints Dependency Injection**: Use `Resolve<T>()` method instead of constructor injection for simplicity
- **CancellationToken**: Passed to all async operations (MediatR, SendOkAsync)
- **Route Parameters**: Captured in request DTO properties matching `{parameter}` in route
- **Query Parameters**: Automatically bound to request DTO properties
- **Tags**: Used for API grouping (equivalent to minimal API `.WithTags()`)
- **REPR Pattern Benefits**: 
  - Strongly-typed responses improve API contracts
  - Better IntelliSense and compile-time safety
  - Easier to generate OpenAPI/Swagger documentation
  - Improved testability with named types

