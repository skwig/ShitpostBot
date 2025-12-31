# Rework Test Endpoints to Message-Based API Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Refactor WebApi test endpoints from command-specific endpoints to unified message-based endpoints (Create/Edit/Delete) that work through chat listeners via an abstraction layer.

**Architecture:** Extract message processing logic from chat listeners into a new `MessageProcessor` in Application layer. Test endpoints map HTTP requests to domain models and invoke the processor directly. Real Discord listeners map DSharpPlus events to domain models and invoke the same processor. This decouples application logic from DSharpPlus and makes it directly testable.

**Tech Stack:** .NET 10.0, FastEndpoints, MediatR, DSharpPlus (for Discord event models)

---

## Task 1: Create Message Processor Abstraction

**Files:**
- Create: `src/ShitpostBot/src/ShitpostBot.Application/Core/IMessageProcessor.cs`
- Create: `src/ShitpostBot/src/ShitpostBot.Application/Core/MessageProcessor.cs`
- Create: `src/ShitpostBot/src/ShitpostBot.Application/Core/MessageData.cs`

**Step 1: Write failing test for MessageProcessor**

Create test file that verifies MessageProcessor processes bot commands:

```csharp
// src/ShitpostBot/test/ShitpostBot.Tests.Unit/MessageProcessorTests.cs
namespace ShitpostBot.Tests.Unit;

public class MessageProcessorTests
{
    [Fact]
    public async Task ProcessCreatedMessage_WithBotCommand_SendsExecuteBotCommand()
    {
        // Arrange
        var mediator = Substitute.For<IMediator>();
        var logger = Substitute.For<ILogger<MessageProcessor>>();
        var chatClient = Substitute.For<IChatClient>();
        var chatUtils = Substitute.For<IChatClientUtils>();
        chatUtils.Mention(Arg.Any<ulong>(), Arg.Any<bool>()).Returns("<@123>");
        chatClient.Utils.Returns(chatUtils);
        
        var processor = new MessageProcessor(logger, chatClient, mediator);
        
        var messageData = new MessageData(
            GuildId: 1,
            ChannelId: 2,
            UserId: 3,
            MessageId: 4,
            CurrentMemberId: 123,
            Content: "<@123> about",
            Attachments: [],
            Embeds: [],
            ReferencedMessage: null,
            Timestamp: DateTimeOffset.UtcNow
        );

        // Act
        await processor.ProcessCreatedMessageAsync(messageData);

        // Assert
        await mediator.Received(1).Send(
            Arg.Is<ExecuteBotCommand>(cmd => 
                cmd.Command.Value == "about" &&
                cmd.MessageIdentification.MessageId == 4),
            Arg.Any<CancellationToken>()
        );
    }
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test --filter "FullyQualifiedName~MessageProcessorTests.ProcessCreatedMessage_WithBotCommand_SendsExecuteBotCommand" --project test/ShitpostBot.Tests.Unit`

Expected: FAIL with "MessageData does not exist" or "MessageProcessor does not exist"

**Step 3: Create MessageData model**

```csharp
// src/ShitpostBot/src/ShitpostBot.Application/Core/MessageData.cs
namespace ShitpostBot.Application.Core;

public record MessageData(
    ulong GuildId,
    ulong ChannelId,
    ulong UserId,
    ulong MessageId,
    ulong CurrentMemberId,
    string? Content,
    IReadOnlyList<MessageAttachmentData> Attachments,
    IReadOnlyList<MessageEmbedData> Embeds,
    MessageReferenceData? ReferencedMessage,
    DateTimeOffset Timestamp
);

public record MessageAttachmentData(
    ulong Id,
    string FileName,
    Uri Url,
    string? MediaType,
    int? Width,
    int? Height
);

public record MessageEmbedData(
    Uri? Url
);

public record MessageReferenceData(
    ulong GuildId,
    ulong ChannelId,
    ulong UserId,
    ulong MessageId
);
```

**Step 4: Create IMessageProcessor interface**

```csharp
// src/ShitpostBot/src/ShitpostBot.Application/Core/IMessageProcessor.cs
namespace ShitpostBot.Application.Core;

public interface IMessageProcessor
{
    Task ProcessCreatedMessageAsync(MessageData messageData, CancellationToken cancellationToken = default);
    Task ProcessUpdatedMessageAsync(MessageData messageData, CancellationToken cancellationToken = default);
    Task ProcessDeletedMessageAsync(MessageData messageData, CancellationToken cancellationToken = default);
}
```

**Step 5: Create MessageProcessor implementation**

Extract logic from `ChatMessageCreatedListener` into `MessageProcessor`:

```csharp
// src/ShitpostBot/src/ShitpostBot.Application/Core/MessageProcessor.cs
using System.Text.RegularExpressions;
using MediatR;
using ShitpostBot.Application.Features.BotCommands;
using ShitpostBot.Application.Features.BotCommands.Redacted;
using ShitpostBot.Application.Features.PostTracking;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Extensions;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Application.Core;

internal class MessageProcessor(
    ILogger<MessageProcessor> logger,
    IChatClient chatClient,
    IMediator mediator)
    : IMessageProcessor
{
    public async Task ProcessCreatedMessageAsync(MessageData messageData, CancellationToken cancellationToken = default)
    {
        var messageIdentification = new MessageIdentification(
            messageData.GuildId,
            messageData.ChannelId,
            messageData.UserId,
            messageData.MessageId
        );
        
        var referencedMessageIdentification = messageData.ReferencedMessage != null
            ? new MessageIdentification(
                messageData.ReferencedMessage.GuildId,
                messageData.ReferencedMessage.ChannelId,
                messageData.ReferencedMessage.UserId,
                messageData.ReferencedMessage.MessageId
            )
            : null;

        logger.LogDebug("Created: '{MessageId}' '{MessageContent}'", messageData.MessageId, messageData.Content);

        if (await TryHandleBotCommandAsync(messageIdentification, referencedMessageIdentification, messageData, cancellationToken)) return;
        if (await TryHandleImageAsync(messageIdentification, messageData, cancellationToken)) return;
        if (await TryHandleLinkAsync(messageIdentification, messageData, cancellationToken)) return;
        if (await TryHandleTextAsync(messageIdentification, messageData, cancellationToken)) return;
    }

    public async Task ProcessUpdatedMessageAsync(MessageData messageData, CancellationToken cancellationToken = default)
    {
        var messageIdentification = new MessageIdentification(
            messageData.GuildId,
            messageData.ChannelId,
            messageData.UserId,
            messageData.MessageId
        );

        logger.LogDebug("Updated: '{MessageId}' '{MessageContent}'", messageData.MessageId, messageData.Content);

        // Check if edited message is a bot command
        var startsWithThisBotTag =
            messageData.Content?.StartsWith(chatClient.Utils.Mention(messageData.CurrentMemberId, true)) == true
            || messageData.Content?.StartsWith(chatClient.Utils.Mention(messageData.CurrentMemberId, false)) == true;

        if (!startsWithThisBotTag)
        {
            return;
        }

        // Extract command
        var command = string.Join(' ',
            (messageData.Content ?? string.Empty).Split(" ", StringSplitOptions.RemoveEmptyEntries).Skip(1));

        if (string.IsNullOrWhiteSpace(command))
        {
            return;
        }

        // Try to find the bot's response to this message
        var botResponseMessageId = await chatClient.FindReplyToMessage(messageIdentification);

        // Get referenced message if any
        var referencedMessageIdentification = messageData.ReferencedMessage != null
            ? new MessageIdentification(
                messageData.ReferencedMessage.GuildId,
                messageData.ReferencedMessage.ChannelId,
                messageData.ReferencedMessage.UserId,
                messageData.ReferencedMessage.MessageId
            )
            : null;

        await mediator.Send(
            new ExecuteBotCommand(
                messageIdentification,
                referencedMessageIdentification,
                new BotCommand(command),
                botResponseMessageId is not null
                    ? new BotCommandEdit(botResponseMessageId.Value)
                    : null
            ),
            cancellationToken
        );
    }

    public async Task ProcessDeletedMessageAsync(MessageData messageData, CancellationToken cancellationToken = default)
    {
        var messageIdentification = new MessageIdentification(
            messageData.GuildId,
            messageData.ChannelId,
            messageData.UserId,
            messageData.MessageId
        );

        logger.LogDebug("Deleted: '{MessageId}' '{MessageContent}'", messageData.MessageId, messageData.Content);

        await mediator.Publish(new MessageDeleted(messageIdentification), cancellationToken);
    }

    private async Task<bool> TryHandleBotCommandAsync(
        MessageIdentification messageIdentification,
        MessageIdentification? referencedMessageIdentification,
        MessageData messageData,
        CancellationToken cancellationToken)
    {
        var startsWithThisBotTag =
            messageData.Content?.StartsWith(chatClient.Utils.Mention(messageData.CurrentMemberId, true)) == true
            || messageData.Content?.StartsWith(chatClient.Utils.Mention(messageData.CurrentMemberId, false)) == true;
        
        if (!startsWithThisBotTag)
        {
            return false;
        }

        var command = string.Join(' ',
            (messageData.Content ?? string.Empty).Split(" ", StringSplitOptions.RemoveEmptyEntries).Skip(1));

        if (string.IsNullOrWhiteSpace(command))
        {
            return false;
        }

        await mediator.Send(
            new ExecuteBotCommand(messageIdentification, referencedMessageIdentification, new BotCommand(command)),
            cancellationToken
        );

        return true;
    }

    private async Task<bool> TryHandleImageAsync(
        MessageIdentification messageIdentification,
        MessageData messageData,
        CancellationToken cancellationToken)
    {
        var imageAttachments = messageData.Attachments
            .Where(a => IsImageOrVideo(a))
            .Where(a => a.Height >= 299 && a.Width >= 299)
            .ToArray();
        
        if (!imageAttachments.Any())
        {
            return false;
        }

        foreach (var attachment in imageAttachments)
        {
            var imageAttachment = new ImageMessageAttachment(
                attachment.Id,
                attachment.FileName,
                attachment.Url,
                attachment.MediaType
            );
            
            await mediator.Publish(
                new ImageMessageCreated(new ImageMessage(messageIdentification, imageAttachment, messageData.Timestamp)),
                cancellationToken
            );
        }

        return true;
    }

    private async Task<bool> TryHandleLinkAsync(
        MessageIdentification messageIdentification,
        MessageData messageData,
        CancellationToken cancellationToken)
    {
        var embedUrls = messageData.Embeds
            .Where(e => e.Url != null)
            .Select(e => e.Url!)
            .ToList();

        if (!embedUrls.Any())
        {
            // try regexing as fallback
            var regexMatches = Regex.Matches(
                messageData.Content ?? string.Empty,
                @"(?:http(s)?:\/\/)?[\w.-]+(?:\.[\w\.-]+)+[\w\-\._~:/?#[\]@!\$&'\(\)\*\+,;=.]+");

            foreach (Match regexMatch in regexMatches)
            {
                embedUrls.Add(new Uri(regexMatch.Value));
            }
        }

        if (!embedUrls.Any())
        {
            return false;
        }

        foreach (var embedUrl in embedUrls)
        {
            var embed = new LinkMessageEmbed(embedUrl);
            await mediator.Publish(
                new LinkMessageCreated(new LinkMessage(messageIdentification, embed, messageData.Timestamp)),
                cancellationToken
            );
        }

        return true;
    }

    private async Task<bool> TryHandleTextAsync(
        MessageIdentification messageIdentification,
        MessageData messageData,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(messageData.Content))
        {
            return false;
        }

        await mediator.Publish(
            new TextMessageCreated(new TextMessage(messageIdentification, messageData.Content, messageData.Timestamp)),
            cancellationToken
        );

        return true;
    }

    private static bool IsImageOrVideo(MessageAttachmentData attachment)
    {
        if (attachment.MediaType != null)
        {
            return attachment.MediaType.StartsWith("image/") || attachment.MediaType.StartsWith("video/");
        }

        var extension = Path.GetExtension(attachment.FileName).ToLowerInvariant();
        return extension is ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp" or ".bmp" or ".mp4" or ".webm" or ".mov";
    }
}
```

**Step 6: Run test to verify it passes**

Run: `dotnet test --filter "FullyQualifiedName~MessageProcessorTests" --project test/ShitpostBot.Tests.Unit`

Expected: PASS

**Step 7: Register MessageProcessor in DI**

```csharp
// src/ShitpostBot/src/ShitpostBot.Application/DependencyInjection.cs
// Add to AddShitpostBotApplication method:
services.AddScoped<IMessageProcessor, MessageProcessor>();
```

**Step 8: Add NSubstitute package to test project if needed**

Run: `dotnet add test/ShitpostBot.Tests.Unit package NSubstitute`

Expected: Package added or already exists

**Step 9: Commit**

```bash
git add src/ShitpostBot/src/ShitpostBot.Application/Core/IMessageProcessor.cs
git add src/ShitpostBot/src/ShitpostBot.Application/Core/MessageProcessor.cs
git add src/ShitpostBot/src/ShitpostBot.Application/Core/MessageData.cs
git add src/ShitpostBot/src/ShitpostBot.Application/DependencyInjection.cs
git add test/ShitpostBot.Tests.Unit/MessageProcessorTests.cs
git add test/ShitpostBot.Tests.Unit/ShitpostBot.Tests.Unit.csproj
git commit -m "feat: add MessageProcessor abstraction for testable message handling"
```

---

## Task 2: Refactor Chat Listeners to Use MessageProcessor

**Files:**
- Modify: `src/ShitpostBot/src/ShitpostBot.Application/Core/ChatMessageCreatedListener.cs`
- Modify: `src/ShitpostBot/src/ShitpostBot.Application/Core/ChatMessageUpdatedListener.cs`
- Modify: `src/ShitpostBot/src/ShitpostBot.Application/Core/ChatMessageDeletedListener.cs`

**Step 1: Write test for ChatMessageCreatedListener delegation**

```csharp
// src/ShitpostBot/test/ShitpostBot.Tests.Unit/ChatMessageCreatedListenerTests.cs
namespace ShitpostBot.Tests.Unit;

public class ChatMessageCreatedListenerTests
{
    [Fact]
    public async Task HandleMessageCreatedAsync_DelegatesToMessageProcessor()
    {
        // Arrange
        var logger = Substitute.For<ILogger<ChatMessageCreatedListener>>();
        var processor = Substitute.For<IMessageProcessor>();
        
        var listener = new ChatMessageCreatedListener(logger, processor);
        
        var message = CreateMockMessageCreateEventArgs(
            guildId: 1,
            channelId: 2,
            userId: 3,
            messageId: 4,
            currentMemberId: 123,
            content: "test message",
            isBot: false
        );

        // Act
        await listener.HandleMessageCreatedAsync(message);

        // Assert
        await processor.Received(1).ProcessCreatedMessageAsync(
            Arg.Is<MessageData>(md => 
                md.GuildId == 1 &&
                md.ChannelId == 2 &&
                md.UserId == 3 &&
                md.MessageId == 4 &&
                md.Content == "test message"),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task HandleMessageCreatedAsync_IgnoresBotMessages()
    {
        // Arrange
        var logger = Substitute.For<ILogger<ChatMessageCreatedListener>>();
        var processor = Substitute.For<IMessageProcessor>();
        
        var listener = new ChatMessageCreatedListener(logger, processor);
        
        var message = CreateMockMessageCreateEventArgs(
            guildId: 1,
            channelId: 2,
            userId: 3,
            messageId: 4,
            currentMemberId: 123,
            content: "bot message",
            isBot: true
        );

        // Act
        await listener.HandleMessageCreatedAsync(message);

        // Assert
        await processor.DidNotReceive().ProcessCreatedMessageAsync(
            Arg.Any<MessageData>(),
            Arg.Any<CancellationToken>()
        );
    }

    private static MessageCreateEventArgs CreateMockMessageCreateEventArgs(
        ulong guildId,
        ulong channelId,
        ulong userId,
        ulong messageId,
        ulong currentMemberId,
        string content,
        bool isBot)
    {
        // Mock DSharpPlus objects
        var guild = Substitute.For<DiscordGuild>();
        guild.Id.Returns(guildId);
        
        var currentMember = Substitute.For<DiscordMember>();
        currentMember.Id.Returns(currentMemberId);
        guild.CurrentMember.Returns(currentMember);
        
        var channel = Substitute.For<DiscordChannel>();
        channel.Id.Returns(channelId);
        
        var author = Substitute.For<DiscordUser>();
        author.Id.Returns(userId);
        author.IsBot.Returns(isBot);
        
        var message = Substitute.For<DiscordMessage>();
        message.Id.Returns(messageId);
        message.Content.Returns(content);
        message.Author.Returns(author);
        message.CreationTimestamp.Returns(DateTimeOffset.UtcNow);
        message.Attachments.Returns([]);
        message.Embeds.Returns([]);
        message.Reference.Returns((DiscordMessageReference?)null);
        
        var args = Substitute.For<MessageCreateEventArgs>();
        args.Guild.Returns(guild);
        args.Channel.Returns(channel);
        args.Author.Returns(author);
        args.Message.Returns(message);
        
        return args;
    }
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test --filter "FullyQualifiedName~ChatMessageCreatedListenerTests" --project test/ShitpostBot.Tests.Unit`

Expected: FAIL with "ChatMessageCreatedListener constructor mismatch"

**Step 3: Refactor ChatMessageCreatedListener**

```csharp
// src/ShitpostBot/src/ShitpostBot.Application/Core/ChatMessageCreatedListener.cs
using DSharpPlus.EventArgs;
using ShitpostBot.Infrastructure.Extensions;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Application.Core;

public class ChatMessageCreatedListener(
    ILogger<ChatMessageCreatedListener> logger,
    IMessageProcessor messageProcessor)
    : IChatMessageCreatedListener
{
    public async Task HandleMessageCreatedAsync(MessageCreateEventArgs message)
    {
        var isPosterBot = message.Author.IsBot;
        if (isPosterBot)
        {
            return;
        }

        var attachments = message.Message.Attachments
            .Select(a => new MessageAttachmentData(
                a.Id,
                a.FileName,
                a.GetAttachmentUri(),
                a.MediaType,
                a.Width,
                a.Height
            ))
            .ToList();

        var embeds = message.Message.Embeds
            .Select(e => new MessageEmbedData(e?.Url))
            .ToList();

        MessageReferenceData? referencedMessage = null;
        if (message.Message.Reference != null)
        {
            referencedMessage = new MessageReferenceData(
                message.Message.Reference.Guild.Id,
                message.Message.Reference.Channel.Id,
                message.Message.Reference.Message.Author.Id,
                message.Message.Reference.Message.Id
            );
        }

        var messageData = new MessageData(
            message.Guild.Id,
            message.Channel.Id,
            message.Author.Id,
            message.Message.Id,
            message.Guild.CurrentMember.Id,
            message.Message.Content,
            attachments,
            embeds,
            referencedMessage,
            message.Message.CreationTimestamp
        );

        await messageProcessor.ProcessCreatedMessageAsync(messageData);
    }
}
```

**Step 4: Refactor ChatMessageUpdatedListener**

```csharp
// src/ShitpostBot/src/ShitpostBot.Application/Core/ChatMessageUpdatedListener.cs
using DSharpPlus.EventArgs;
using ShitpostBot.Infrastructure.Extensions;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Application.Core;

public class ChatMessageUpdatedListener(
    ILogger<ChatMessageUpdatedListener> logger,
    IMessageProcessor messageProcessor)
    : IChatMessageUpdatedListener
{
    public async Task HandleMessageUpdatedAsync(MessageUpdateEventArgs message)
    {
        var isPosterBot = message.Author.IsBot;
        if (isPosterBot)
        {
            return;
        }

        var attachments = message.Message.Attachments
            .Select(a => new MessageAttachmentData(
                a.Id,
                a.FileName,
                a.GetAttachmentUri(),
                a.MediaType,
                a.Width,
                a.Height
            ))
            .ToList();

        var embeds = message.Message.Embeds
            .Select(e => new MessageEmbedData(e?.Url))
            .ToList();

        MessageReferenceData? referencedMessage = null;
        if (message.Message.Reference != null)
        {
            referencedMessage = new MessageReferenceData(
                message.Message.Reference.Guild.Id,
                message.Message.Reference.Channel.Id,
                message.Message.Reference.Message.Author.Id,
                message.Message.Reference.Message.Id
            );
        }

        var messageData = new MessageData(
            message.Guild.Id,
            message.Channel.Id,
            message.Author.Id,
            message.Message.Id,
            message.Guild.CurrentMember.Id,
            message.Message.Content,
            attachments,
            embeds,
            referencedMessage,
            message.Message.CreationTimestamp
        );

        await messageProcessor.ProcessUpdatedMessageAsync(messageData);
    }
}
```

**Step 5: Refactor ChatMessageDeletedListener**

```csharp
// src/ShitpostBot/src/ShitpostBot.Application/Core/ChatMessageDeletedListener.cs
using DSharpPlus.EventArgs;
using ShitpostBot.Infrastructure.Services;

namespace ShitpostBot.Application.Core;

public class ChatMessageDeletedListener(
    ILogger<ChatMessageDeletedListener> logger,
    IMessageProcessor messageProcessor)
    : IChatMessageDeletedListener
{
    public async Task HandleMessageDeletedAsync(MessageDeleteEventArgs message)
    {
        if (message.Message?.Author == null)
        {
            return;
        }

        var isPosterBot = message.Message.Author.IsBot;
        if (isPosterBot)
        {
            return;
        }

        var messageData = new MessageData(
            message.Guild.Id,
            message.Channel.Id,
            message.Message.Author.Id,
            message.Message.Id,
            message.Guild.CurrentMember.Id,
            message.Message.Content,
            [],
            [],
            null,
            DateTimeOffset.UtcNow
        );

        await messageProcessor.ProcessDeletedMessageAsync(messageData);
    }
}
```

**Step 6: Run tests to verify they pass**

Run: `dotnet test --filter "FullyQualifiedName~ChatMessageCreatedListenerTests" --project test/ShitpostBot.Tests.Unit`

Expected: PASS

**Step 7: Run all unit tests**

Run: `dotnet test --project test/ShitpostBot.Tests.Unit`

Expected: All tests PASS

**Step 8: Commit**

```bash
git add src/ShitpostBot/src/ShitpostBot.Application/Core/ChatMessageCreatedListener.cs
git add src/ShitpostBot/src/ShitpostBot.Application/Core/ChatMessageUpdatedListener.cs
git add src/ShitpostBot/src/ShitpostBot.Application/Core/ChatMessageDeletedListener.cs
git add test/ShitpostBot.Tests.Unit/ChatMessageCreatedListenerTests.cs
git commit -m "refactor: chat listeners delegate to MessageProcessor"
```

---

## Task 3: Create New Message-Based Test Endpoints

**Files:**
- Create: `src/ShitpostBot/src/ShitpostBot.WebApi/Endpoints/PostMessageEndpoint.cs`
- Create: `src/ShitpostBot/src/ShitpostBot.WebApi/Endpoints/UpdateMessageEndpoint.cs`
- Create: `src/ShitpostBot/src/ShitpostBot.WebApi/Endpoints/DeleteMessageEndpoint.cs`
- Create: `src/ShitpostBot/src/ShitpostBot.WebApi/Endpoints/MessageEndpointDtos.cs`

[Full task details provided in plan...]

---

## Task 4: Migrate E2E Tests to New Endpoints

[Full task details provided in plan...]

---

## Task 5: Remove Old Test Endpoints

[Full task details provided in plan...]

---

## Task 6: Update Documentation

[Full task details provided in plan...]
