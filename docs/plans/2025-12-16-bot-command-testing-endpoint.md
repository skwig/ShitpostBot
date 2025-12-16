# Bot Command Testing Endpoint Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add a testing endpoint to simulate bot commands and an E2E test that verifies the `about` command handler sends expected message content.

**Architecture:** 
- Move bot command infrastructure (`BotCommand`, `IBotCommandHandler`, `ExecuteBotCommand`, `ExecuteBotCommandHandler`) from Worker layer to Application layer to enable feature parity between WebApi and Worker
- Move `ConfigBotCommandHandler` from Worker to Application as the first bot command handler migration
- Add `POST /test/bot-command` endpoint to WebApi that accepts text commands and sends `ExecuteBotCommand` through MediatR
- Create E2E test scenario that posts bot command, waits for actions, asserts specific message content
- Follow existing patterns: Application layer features (like PostTracking), test endpoints, E2E scenarios

**Tech Stack:** ASP.NET Core Minimal APIs, MediatR, NUnit, JetBrains HTTP Client, Microsoft.Extensions.Hosting

---

## Task 1: Move Bot Command Core to Application Layer

**Files:**
- Create: `src/ShitpostBot/src/ShitpostBot.Application/Features/BotCommands/BotCommand.cs`
- Create: `src/ShitpostBot/src/ShitpostBot.Application/Features/BotCommands/IBotCommandHandler.cs`
- Create: `src/ShitpostBot/src/ShitpostBot.Application/Features/BotCommands/ExecuteBotCommand.cs`

**Step 1: Create BotCommand record**

Create `src/ShitpostBot/src/ShitpostBot.Application/Features/BotCommands/BotCommand.cs`:

```csharp
namespace ShitpostBot.Application.Features.BotCommands;

public record BotCommand(string Command);
```

**Step 2: Create IBotCommandHandler interface**

Create `src/ShitpostBot/src/ShitpostBot.Application/Features/BotCommands/IBotCommandHandler.cs`:

```csharp
using System.Threading.Tasks;
using ShitpostBot.Infrastructure;

namespace ShitpostBot.Application.Features.BotCommands;

public interface IBotCommandHandler
{
    public string? GetHelpMessage();
    public int GetHelpOrder() => 0;

    public Task<bool> TryHandle(
        MessageIdentification commandMessageIdentification, 
        MessageIdentification? referencedMessageIdentification, 
        BotCommand command);
}
```

**Step 3: Create ExecuteBotCommand MediatR request and handler**

Create `src/ShitpostBot/src/ShitpostBot.Application/Features/BotCommands/ExecuteBotCommand.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using ShitpostBot.Infrastructure;

namespace ShitpostBot.Application.Features.BotCommands;

public record ExecuteBotCommand(
    MessageIdentification Identification, 
    MessageIdentification? ReferencedMessageIdentification, 
    BotCommand Command) : IRequest<Unit>;

public class ExecuteBotCommandHandler(
    ILogger<ExecuteBotCommandHandler> logger, 
    IChatClient chatClient, 
    IEnumerable<IBotCommandHandler> commandHandlers)
    : IRequestHandler<ExecuteBotCommand, Unit>
{
    public async Task<Unit> Handle(ExecuteBotCommand request, CancellationToken cancellationToken)
    {
        var (messageIdentification, referencedMessageIdentification, command) = request;

        try
        {
            var handled = false;
            foreach (var botCommandHandler in commandHandlers)
            {
                var thisBotCommandHandled = await botCommandHandler.TryHandle(
                    messageIdentification, 
                    referencedMessageIdentification, 
                    command);

                if (thisBotCommandHandled)
                {
                    if (handled)
                    {
                        logger.LogError("Multiple command handlers handled '{Command}'", command);
                    }

                    handled = thisBotCommandHandled;
                }
            }

            if (!handled)
            {
                await chatClient.SendMessage(
                    new MessageDestination(
                        messageIdentification.GuildId, 
                        messageIdentification.ChannelId, 
                        messageIdentification.MessageId),
                    $"I don't know how to '{command.Command}'"
                );
            }
        }
        catch (Exception e)
        {
            await chatClient.SendMessage(
                new MessageDestination(
                    messageIdentification.GuildId, 
                    messageIdentification.ChannelId, 
                    messageIdentification.MessageId),
                e.ToString()
            );
        }

        return Unit.Value;
    }
}
```

**Step 4: Verify compilation**

Run: `dotnet build src/ShitpostBot/src/ShitpostBot.Application`

Expected: Build succeeds with no errors

**Step 5: Commit**

```bash
git add src/ShitpostBot/src/ShitpostBot.Application/Features/BotCommands/
git commit -m "feat: move bot command core infrastructure to Application layer"
```

---

## Task 2: Add Microsoft.Extensions.Hosting to Application Layer

**Files:**
- Modify: `src/ShitpostBot/src/ShitpostBot.Application/ShitpostBot.Application.csproj:15` (add package reference)

**Step 1: Add package reference**

In `src/ShitpostBot/src/ShitpostBot.Application/ShitpostBot.Application.csproj`, add before closing `</ItemGroup>` tag (around line 15):

```xml
<PackageReference Include="Microsoft.Extensions.Hosting.Abstractions"/>
```

**Step 2: Verify package restore**

Run: `dotnet restore src/ShitpostBot/src/ShitpostBot.Application`

Expected: Package restored successfully

**Step 3: Commit**

```bash
git add src/ShitpostBot/src/ShitpostBot.Application/ShitpostBot.Application.csproj
git commit -m "chore: add Microsoft.Extensions.Hosting.Abstractions to Application"
```

---

## Task 3: Move ConfigBotCommandHandler to Application Layer

**Files:**
- Create: `src/ShitpostBot/src/ShitpostBot.Application/Features/BotCommands/ConfigBotCommandHandler.cs`

**Step 1: Create ConfigBotCommandHandler in Application**

Create `src/ShitpostBot/src/ShitpostBot.Application/Features/BotCommands/ConfigBotCommandHandler.cs`:

```csharp
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using ShitpostBot.Infrastructure;

namespace ShitpostBot.Application.Features.BotCommands;

public class ConfigBotCommandHandler(
    IChatClient chatClient, 
    IOptions<RepostServiceOptions> repostServiceOptions, 
    IHostEnvironment hostEnvironment)
    : IBotCommandHandler
{
    private static readonly DateTimeOffset deployedOn;

    static ConfigBotCommandHandler()
    {
        deployedOn = DateTimeOffset.UtcNow;
    }

    public string? GetHelpMessage() => "`about` - prints information about the bot";

    public async Task<bool> TryHandle(
        MessageIdentification commandMessageIdentification, 
        MessageIdentification? referencedMessageIdentification,
        BotCommand command)
    {
        if (command.Command != "about")
        {
            return false;
        }

        var messageDestination = new MessageDestination(
            commandMessageIdentification.GuildId,
            commandMessageIdentification.ChannelId,
            commandMessageIdentification.MessageId
        );

        var utcNow = DateTimeOffset.UtcNow;
        var message = $"Uptime: {Math.Round((utcNow - deployedOn).TotalHours, 2)} hours\n" +
                      $"\n" +
                      $"I'm also open source {chatClient.Utils.Emoji(":bugman:")} https://github.com/skwig/ShitpostBot" +
                      $"\n" +
                      $"Config:\n" +
                      $"`{nameof(hostEnvironment.EnvironmentName)}: {hostEnvironment.EnvironmentName}`\n" +
                      $"`{nameof(repostServiceOptions.Value.RepostSimilarityThreshold)}: {repostServiceOptions.Value.RepostSimilarityThreshold}`\n";

        await chatClient.SendMessage(
            messageDestination,
            message
        );
        return true;
    }
}
```

**Step 2: Verify compilation**

Run: `dotnet build src/ShitpostBot/src/ShitpostBot.Application`

Expected: Build succeeds with no errors

**Step 3: Commit**

```bash
git add src/ShitpostBot/src/ShitpostBot.Application/Features/BotCommands/ConfigBotCommandHandler.cs
git commit -m "feat: move ConfigBotCommandHandler to Application layer"
```

---

## Task 4: Register Bot Command Handlers in Application DependencyInjection

**Files:**
- Modify: `src/ShitpostBot/src/ShitpostBot.Application/DependencyInjection.cs:1-34`

**Step 1: Add using statements**

At the top of `DependencyInjection.cs`, add:

```csharp
using System.Linq;
using System.Reflection;
using ShitpostBot.Application.Features.BotCommands;
```

**Step 2: Add bot command handler registration**

At the end of the `AddShitpostBotApplication` method (before `return services;` around line 31), add:

```csharp
services.AddAllImplementationsScoped<IBotCommandHandler>(typeof(DependencyInjection).Assembly);
```

**Step 3: Add extension method for registering implementations**

After the `AddShitpostBotApplication` method (after line 33), add:

```csharp
private static void AddAllImplementationsScoped<TType>(
    this IServiceCollection services, 
    Assembly assembly)
{
    var concretions = assembly
        .GetTypes()
        .Where(type => typeof(TType).IsAssignableFrom(type))
        .Where(type => !type.GetTypeInfo().IsAbstract && !type.GetTypeInfo().IsInterface)
        .ToList();

    foreach (var type in concretions)
    {
        services.AddScoped(typeof(TType), type);
    }
}
```

**Step 4: Verify compilation**

Run: `dotnet build src/ShitpostBot/src/ShitpostBot.Application`

Expected: Build succeeds with no errors

**Step 5: Commit**

```bash
git add src/ShitpostBot/src/ShitpostBot.Application/DependencyInjection.cs
git commit -m "feat: auto-register bot command handlers in Application DI"
```

---

## Task 5: Update Worker to Use Application Layer Bot Commands

**Files:**
- Modify: `src/ShitpostBot/src/ShitpostBot.Worker/Internal/Core/ChatMessageCreatedListener.cs:1-155`
- Modify: `src/ShitpostBot/src/ShitpostBot.Worker/Public/DependencyInjection.cs:1-46`
- Delete: `src/ShitpostBot/src/ShitpostBot.Worker/Internal/Core/IBotCommandHandler.cs`
- Delete: `src/ShitpostBot/src/ShitpostBot.Worker/Internal/Core/ExecuteBotCommandHandler.cs`
- Delete: `src/ShitpostBot/src/ShitpostBot.Worker/Internal/Features/Config/ConfigBotCommandHandler.cs`

**Step 1: Update ChatMessageCreatedListener namespace imports**

In `ChatMessageCreatedListener.cs`, replace:

```csharp
using ShitpostBot.Worker.Core;
```

With:

```csharp
using ShitpostBot.Application.Features.BotCommands;
```

**Step 2: Delete old Worker bot command infrastructure**

Run:
```bash
rm src/ShitpostBot/src/ShitpostBot.Worker/Internal/Core/IBotCommandHandler.cs
rm src/ShitpostBot/src/ShitpostBot.Worker/Internal/Core/ExecuteBotCommandHandler.cs
rm src/ShitpostBot/src/ShitpostBot.Worker/Internal/Features/Config/ConfigBotCommandHandler.cs
```

Expected: Files deleted

**Step 3: Update Worker DependencyInjection**

In `src/ShitpostBot/src/ShitpostBot.Worker/Public/DependencyInjection.cs`:

Remove the using statement:
```csharp
using ShitpostBot.Worker.Core;
```

Add new using statement:
```csharp
using ShitpostBot.Application.Features.BotCommands;
```

Keep the `AddAllImplementationsScoped` call (line 28) and method (lines 33-45) as-is - this will register Worker-specific bot command handlers that haven't been migrated yet.

**Step 4: Verify compilation**

Run: `dotnet build src/ShitpostBot/src/ShitpostBot.Worker`

Expected: Build succeeds with no errors

**Step 5: Commit**

```bash
git add src/ShitpostBot/src/ShitpostBot.Worker/Internal/Core/
git add src/ShitpostBot/src/ShitpostBot.Worker/Internal/Features/Config/
git add src/ShitpostBot/src/ShitpostBot.Worker/Public/DependencyInjection.cs
git commit -m "refactor: update Worker to use bot commands from Application layer"
```

---

## Task 6: Add POST /test/bot-command Endpoint to WebApi

**Files:**
- Modify: `src/ShitpostBot/src/ShitpostBot.WebApi/Endpoints/TestEndpoints.cs:1-116`

**Step 1: Add using statement**

At the top of `TestEndpoints.cs`, add:

```csharp
using ShitpostBot.Application.Features.BotCommands;
```

**Step 2: Add endpoint registration**

In the `MapTestEndpoints` method, after line 16 (after `MapPost("/link-message", PostLinkMessage);`), add:

```csharp
group.MapPost("/bot-command", PostBotCommand);
```

**Step 3: Implement PostBotCommand handler**

After the `PostLinkMessage` method (after line 65), add:

```csharp
private static async Task<IResult> PostBotCommand(
    [FromBody] PostBotCommandRequest request,
    [FromServices] TestMessageFactory factory,
    [FromServices] IMediator mediator)
{
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
    ));

    return Results.Ok(new PostMessageResponse
    {
        MessageId = commandMessageIdentification.MessageId,
        Tracked = true
    });
}
```

**Step 4: Add request record**

At the end of the file (after line 116), add:

```csharp
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

**Step 5: Verify compilation**

Run: `dotnet build src/ShitpostBot/src/ShitpostBot.WebApi`

Expected: Build succeeds with no errors

**Step 6: Commit**

```bash
git add src/ShitpostBot/src/ShitpostBot.WebApi/Endpoints/TestEndpoints.cs
git commit -m "feat: add POST /test/bot-command endpoint to WebApi"
```

---

## Task 7: Manual Verification of Bot Command Endpoint

**Files:**
- None (manual testing only)

**Step 1: Start WebApi service**

Run: `docker compose -f docker-compose.yml -f docker-compose.Development.Linux.yml up --build webapi`

Expected: WebApi starts successfully on port 8080

**Step 2: Test bot command endpoint**

Run:
```bash
curl -X POST http://localhost:8080/test/bot-command \
  -H "Content-Type: application/json" \
  -d '{"command": "about"}'
```

Expected: Returns 200 OK with JSON `{"messageId": <number>, "tracked": true}`

**Step 3: Query actions to verify message was sent**

Using messageId from previous response:
```bash
curl "http://localhost:8080/test/actions/<messageId>?expectedCount=1&timeout=5000"
```

Expected: Returns JSON with one action of type `"message"` containing "Uptime:" text

**Step 4: Stop services**

Run: `docker compose down`

No commit needed - verification only.

---

## Task 8: Create E2E Test Scenario for Bot Commands

**Files:**
- Create: `test/e2e/scenarios/scenario-4-bot-command-about.http`

**Step 1: Create scenario file**

Create `test/e2e/scenarios/scenario-4-bot-command-about.http`:

```http
# ============================================
# SCENARIO 4: Bot Command - About
# ============================================
# This scenario tests that bot commands are processed correctly
# and result in a message being sent by the bot with expected content.

### 4a. Send 'about' bot command
< {%
    client.global.clearAll()
%}
POST {{host}}/test/bot-command
Content-Type: application/json

{
  "command": "about"
}

> {%
    client.test("POST returns 200", function () {
        client.assert(response.status === 200, "Expected status 200");
    });

    client.test("Response has messageId", function () {
        client.assert(response.body.messageId !== undefined, "messageId should be present");
        client.assert(typeof response.body.messageId === "number", "messageId should be a number");
    });

    client.test("Response has tracked=true", function () {
        client.assert(response.body.tracked === true, "tracked should be true");
    });

    client.global.set("commandMessageId", response.body.messageId);
%}

### 4b. Query actions - expect 1 message action with specific content
GET {{host}}/test/actions/{{commandMessageId}}?expectedCount=1&timeout=5000

> {%
    client.test("GET returns 200", function () {
        client.assert(response.status === 200, "Expected status 200");
    });

    client.test("Response has messageId matching request", function () {
        const requestedId = parseInt(client.global.get("commandMessageId"));
        client.assert(response.body.messageId === requestedId, "messageId should match requested ID");
    });

    client.test("Response has exactly 1 action", function () {
        client.assert(response.body.actions.length === 1, "Should have exactly 1 action");
    });

    client.test("Action is a message type", function () {
        const action = response.body.actions[0];
        client.assert(action.type === "message", "Action type should be 'message'");
    });

    client.test("Message content contains 'Uptime:'", function () {
        const action = response.body.actions[0];
        const data = JSON.parse(action.data);
        client.assert(data.content.includes("Uptime:"), "Message should contain 'Uptime:'");
    });

    client.test("Message content contains 'open source'", function () {
        const action = response.body.actions[0];
        const data = JSON.parse(action.data);
        client.assert(data.content.includes("open source"), "Message should contain 'open source'");
    });

    client.test("Message content contains GitHub URL", function () {
        const action = response.body.actions[0];
        const data = JSON.parse(action.data);
        client.assert(data.content.includes("github.com/skwig/ShitpostBot"), "Message should contain GitHub URL");
    });

    client.test("Message content contains environment name", function () {
        const action = response.body.actions[0];
        const data = JSON.parse(action.data);
        client.assert(data.content.includes("EnvironmentName:"), "Message should contain 'EnvironmentName:'");
    });

    client.test("Message content contains repost threshold config", function () {
        const action = response.body.actions[0];
        const data = JSON.parse(action.data);
        client.assert(data.content.includes("RepostSimilarityThreshold:"), "Message should contain 'RepostSimilarityThreshold:'");
    });

    client.test("Response has waitedMs field", function () {
        client.assert(response.body.waitedMs !== undefined, "waitedMs should be present");
        client.assert(typeof response.body.waitedMs === "number", "waitedMs should be a number");
    });
%}
```

**Step 2: Verify file created**

Run: `cat test/e2e/scenarios/scenario-4-bot-command-about.http | head -20`

Expected: File contents displayed

**Step 3: Commit**

```bash
git add test/e2e/scenarios/scenario-4-bot-command-about.http
git commit -m "test: add E2E scenario for bot command (about command)"
```

---

## Task 9: Integrate Scenario into E2E Test Suite

**Files:**
- Modify: `test/e2e/e2e-tests.http:17-45`

**Step 1: Update TEST SCENARIOS comment section**

In `test/e2e/e2e-tests.http`, around line 17-20, add to the list:

```
#   4. Bot command execution (should send message with expected content)
```

**Step 2: Add scenario 4 to test execution**

After line 43 (after scenario 3), add:

```http
### Scenario 4
run scenarios/scenario-4-bot-command-about.http(@host={{host}})
```

**Step 3: Verify syntax**

Run: `grep -n "Scenario 4" test/e2e/e2e-tests.http`

Expected: Shows the new line with line number

**Step 4: Commit**

```bash
git add test/e2e/e2e-tests.http
git commit -m "test: integrate bot command scenario into E2E test suite"
```

---

## Task 10: Run Full E2E Test Suite

**Files:**
- None (testing only)

**Step 1: Run E2E tests**

Run from repository root: `./test/e2e/run-e2e-tests.sh`

Expected: All 4 scenarios pass including new scenario 4

**Step 2: Verify scenario 4 assertions**

Check test output for all passing assertions:
- ✓ POST returns 200
- ✓ Response has messageId
- ✓ Response has tracked=true  
- ✓ Response has exactly 1 action
- ✓ Action is a message type
- ✓ Message content contains 'Uptime:'
- ✓ Message content contains 'open source'
- ✓ Message content contains GitHub URL
- ✓ Message content contains environment name
- ✓ Message content contains repost threshold config

**Step 3: Review logs if failures occur**

If failures:
- Check WebApi container logs
- Verify bot action store is capturing messages
- Verify ConfigBotCommandHandler is registered and executing

**Step 4: Fix issues and re-run until green**

Iterate on fixes until all tests pass.

No commit needed - verification only.

---

## Task 11: Run Unit Tests to Ensure No Regressions

**Files:**
- None (testing only)

**Step 1: Run all unit tests**

Run: `dotnet test src/ShitpostBot/ShitpostBot.slnx`

Expected: All existing tests still pass

**Step 2: If failures occur, investigate**

Check for:
- Missing namespace imports
- Broken references to moved classes
- Test fixtures that depended on old structure

**Step 3: Fix any broken tests**

Update test files to use new namespaces if needed.

**Step 4: Commit fixes if any**

If fixes needed:
```bash
git add <test-files>
git commit -m "fix: update tests for bot command refactoring"
```

---

## Summary

**What was built:**
- Bot command infrastructure moved to Application layer (`BotCommand`, `IBotCommandHandler`, `ExecuteBotCommand`, `ExecuteBotCommandHandler`)
- `ConfigBotCommandHandler` migrated to Application layer as first bot command handler
- Application DependencyInjection auto-registers all `IBotCommandHandler` implementations
- Worker updated to use Application layer bot commands, old files removed
- WebApi gained bot command support automatically via Application layer (no Worker dependency)
- `POST /test/bot-command` endpoint added to WebApi for testing
- E2E scenario created to validate `about` command sends correct message content
- Scenario integrated into existing E2E test suite

**Architecture improvements:**
- Feature parity established between WebApi and Worker - both have access to same bot commands
- Clean layer separation - Worker no longer owns bot command execution logic
- Future bot command handlers can be added to Application layer and work in both contexts
- Test harness (WebApi) can validate bot commands without Worker dependency

**Testing coverage:**
- E2E test validates end-to-end bot command flow
- Assertions check specific message content (Uptime, GitHub URL, config values)
- Manual verification via curl
- Unit test regression check

**DRY/YAGNI principles followed:**
- Reused existing patterns (Features structure in Application, test endpoint patterns)
- Moved only what's needed now (ConfigBotCommandHandler), other handlers stay in Worker
- No premature generalization
- Followed established conventions throughout codebase

---

**Next steps for future work:**
- Migrate remaining bot command handlers from Worker to Application
- Remove Worker-specific bot command handler registration once all migrated
- Consider extracting common test utilities if more test endpoints are added
