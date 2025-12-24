# Dynamic Search UX via Message Edit Detection - PoC Design

**Date**: 2025-12-24  
**Status**: Design Complete - Ready for Implementation

## Problem

Currently, refining search queries requires sending multiple messages:
```
User: @ShitpostBot search cat
Bot: [5 cat results]
User: @ShitpostBot search dog  ← Creates spam
Bot: [5 dog results]
User: @ShitpostBot search meme ← More spam
Bot: [5 meme results]
```

In small servers, this creates annoying channel clutter when iteratively refining searches.

## Solution

Allow users to edit their search command (press ↑ in Discord), and the bot will detect the edit and **update** the original search results message instead of sending a new one.

**Desired UX:**
```
User: @ShitpostBot search cat
Bot: [5 cat results]
User: [presses ↑, edits to] @ShitpostBot search dog
Bot: [same message updates to show 5 dog results]
```

## Design Decisions

### 1. Use Discord's In-Memory Message Cache (No App Cache!)

**Key insight:** The bot already replies to the user's command message via `MessageDestination.ReplyToMessageId`. Instead of maintaining application state, we scan Discord's in-memory message cache to find the bot's response.

**How it works:**
1. User edits search command message (ID: 123)
2. `MessageUpdated` event fires for message 123
3. Bot scans last **50 messages** from Discord's in-memory cache (2048 message capacity)
4. Finds bot's message where `message.Reference.Message.Id == 123`
5. Updates that message with new search results

**Why this works:**
- DSharpPlus maintains 2048-message in-memory cache (configured in DependencyInjection.cs)
- Scanning 50 messages is **O(50) in-memory operation** (effectively free)
- In small servers, 50 messages = hours/days of history
- Zero application state to manage
- Works across bot restarts (as long as Discord cache is warm)

### 2. Graceful Degradation

**If bot response not found in last 50 messages:**
- Fallback: Send new message (same behavior as current implementation)
- User gets results either way - no failure case
- Silent fallback (no error messages about cache mechanics)

**When this happens:**
- Message too old (>50 messages ago)
- Bot response was deleted
- Bot restarted and cache not warm yet

### 3. Search Command Only (PoC Scope)

Only `SearchBotCommandHandler` supports edit-to-update for now. Other commands (`about`, `help`, etc.) don't benefit from updating responses.

Easy to extend pattern to other commands later if useful.

## Architecture

### Data Flow

```
┌─────────────────────────────────────────────────────────────┐
│ User edits message: "@ShitpostBot search dog"              │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│ Discord fires MessageUpdated event                          │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│ ChatMessageUpdatedListener.HandleMessageUpdatedAsync()     │
│ - Check if message is bot command (starts with bot mention)│
│ - Extract command from edited message                       │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│ Find bot's response message                                 │
│ - Call chatClient.FindReplyToMessage(userMessageId)        │
│ - Scans last 50 messages from Discord cache                │
│ - Returns bot message ID if found, else null               │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│ Execute command with context                                │
│ mediator.Send(new ExecuteBotCommand(                       │
│     identification,                                         │
│     referencedMessage,                                      │
│     command,                                                │
│     IsEdit: true,                                           │
│     BotResponseMessageId: foundMessageId))                  │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│ SearchBotCommandHandler.TryHandle()                        │
│ - Execute search (same as before)                          │
│ - Build embeds (same as before)                            │
│ - If IsEdit && BotResponseMessageId != null:               │
│     → chatClient.UpdateMessage()                           │
│   Else:                                                     │
│     → chatClient.SendMessage()                             │
└─────────────────────────────────────────────────────────────┘
```

## Edge Cases Handled

| Scenario                                       | Behavior                                                              |
| ---------------------------------------------- | --------------------------------------------------------------------- |
| Edit removes bot mention                       | `ChatMessageUpdatedListener` ignores (no longer a command)              |
| Edit changes command type (`search cat` → `about`) | Both commands execute normally, `about` sends new message (no update)   |
| Bot response deleted by user                   | `UpdateMessage()` returns false → fallback to send new message          |
| Bot response > 50 messages ago                 | `FindReplyToMessage()` returns null → send new message                  |
| Bot restarted (cold cache)                     | Discord cache warms quickly → usually works, fallback if not          |
| User spams edits quickly                       | Each edit triggers new search (rate limited by Discord API naturally) |
| Edit after bot response deleted                | Graceful: sends new message                                           |
| Message edited hours later                     | Works if response still in last 50 messages, otherwise sends new      |

## Performance Characteristics

**Memory:**
- No application cache (0 bytes overhead)
- Uses existing Discord cache (already allocated, 2048 messages)

**CPU:**
- Scan 50 messages: O(50) linear scan in-memory (negligible)
- Typical time: <1ms

**Network:**
- If messages cached: 0 API calls to fetch
- If messages not cached: 1 API call to fetch last 50 messages
- Message update: 1 API call (same as send)

**Discord Rate Limits:**
- Message edits count against rate limits (same as sends)
- Natural user behavior (editing) keeps rate under limits

## Files Summary

### Files to Create (1)
1. `ShitpostBot.Worker/Internal/Core/ChatMessageUpdatedListener.cs` - Handles message edit events

### Files to Modify (12)

**Infrastructure (3):**
1. `ShitpostBot.Infrastructure/Public/Services/IChatClient.cs` - Add event + methods + interface
2. `ShitpostBot.Infrastructure/Internal/Services/DiscordChatClient.cs` - Implement event + methods
3. `ShitpostBot.WebApi/Services/NullChatClient.cs` - Stub implementations

**Worker (2):**
4. `ShitpostBot.Worker/Worker.cs` - Wire up event
5. `ShitpostBot.Worker/Public/DependencyInjection.cs` - Register listener

**Application (7):**
6. `ShitpostBot.Application/Features/BotCommands/ExecuteBotCommand.cs` - Add parameters
7. `ShitpostBot.Application/Features/BotCommands/IBotCommandHandler.cs` - Update interface
8. `ShitpostBot.Application/Features/BotCommands/Search/SearchBotCommandHandler.cs` - Implement edit logic
9-15. All other bot command handlers (7 files) - Add default parameters to signature

## Implementation Phases

### Phase 1: Infrastructure (Event Plumbing & APIs)
- Add `MessageUpdated` event to `IChatClient` interface
- Add `FindReplyToMessage()` method to `IChatClient` interface
- Add `UpdateMessage()` method to `IChatClient` interface
- Add `IChatMessageUpdatedListener` interface
- Implement in `DiscordChatClient`
- Add stub implementations to `NullChatClient`

### Phase 2: Worker Layer (Event Handling)
- Create `ChatMessageUpdatedListener.cs`
- Register listener in DI
- Wire up event in `Worker.cs`

### Phase 3: Application Layer (Command Execution)
- Update `ExecuteBotCommand` record with new parameters
- Update `IBotCommandHandler` interface signature
- Update all command handlers with new default parameters
- Implement edit detection logic in `SearchBotCommandHandler`

## Testing Strategy

### Manual Testing

**Test Case 1: Basic Edit Flow**
1. Send: `@ShitpostBot search cat`
2. Verify: Bot replies with 5 cat-related image results
3. Press ↑, edit to: `@ShitpostBot search dog`
4. **Expected**: Same response message updates to show 5 dog-related results
5. **Expected**: No new message created
6. **Expected**: Discord shows "(edited)" on user's message

**Test Case 2: Multiple Edits**
1. Send: `@ShitpostBot search cat`
2. Edit to: `@ShitpostBot search dog`
3. Edit to: `@ShitpostBot search meme`
4. **Expected**: Same response message updates each time
5. **Expected**: Only 2 total messages in channel (user command + bot response)

**Test Case 3: Edit Different Command**
1. Send: `@ShitpostBot search cat`
2. Edit to: `@ShitpostBot about`
3. **Expected**: New message with "about" response sent
4. **Expected**: Original search results remain unchanged

**Test Case 4: Fallback (Old Message)**
1. Send 60+ messages in channel
2. Scroll up, edit old search command
3. **Expected**: New search results message sent (graceful fallback)

**Test Case 5: Non-Command Edit**
1. Send: `hello world`
2. Edit to: `hello universe`
3. **Expected**: Bot does nothing (no bot mention)

## Future Enhancements

- Expand to other commands (e.g., `repost match all`)
- Visual feedback: Add footer to embeds showing "Updated <timestamp>"
- Configurable scan depth via appsettings
- Metrics: Track edit-to-update success rate, fallback rate
- Rate limiting: Per-user cooldown on search edits if abused
- Cache warming: Pre-warm Discord cache on bot startup

---

**Ready for implementation!** This design provides message edit detection for search refinement using Discord's existing in-memory message cache, with zero application state management and graceful fallback behavior.
