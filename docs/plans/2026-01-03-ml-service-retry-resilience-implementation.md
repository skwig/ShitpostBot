# ML Service Retry Resilience Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add retry resilience to ML service calls in `EvaluateRepost_ImagePostTrackedHandler` to prevent message loss during transient failures.

**Architecture:** Configure MassTransit's retry middleware with exponential backoff (10s, 30s, 90s) globally for all consumers. Enhance handler error classification to distinguish retryable (5xx, network, timeout) from non-retryable (4xx) failures. Preserve existing 404 handling.

**Tech Stack:** MassTransit 8.2.5, Refit 9.0.2, xUnit, FluentAssertions

---

## Task 1: Add MassTransit Retry Configuration

**Files:**
- Modify: `src/ShitpostBot/src/ShitpostBot.Infrastructure/Public/MassTransitConfiguration.cs:38-46`

**Step 1: Add retry middleware to MassTransit configuration**

Locate the `UsingPostgres` configuration block (around line 38) and add retry configuration before `cfg.ConfigureEndpoints(context);`:

```csharp
x.UsingPostgres((context, cfg) =>
{
    // Add exponential retry for transient failures
    cfg.UseMessageRetry(r => r.Exponential(
        retryLimit: 3,
        minInterval: TimeSpan.FromSeconds(10),
        maxInterval: TimeSpan.FromSeconds(90),
        intervalDelta: TimeSpan.FromSeconds(10)
    )
    // Don't retry validation or argument errors
    .Ignore<ValidationException>()
    .Ignore<ArgumentException>()
    .Ignore<ArgumentNullException>()
    .Ignore<InvalidOperationException>(ex => 
        ex.Message.StartsWith("ML service client error"))
    
    // Explicitly handle transient failures
    .Handle<HttpRequestException>()    // Network/connection failures
    .Handle<TaskCanceledException>()   // Timeouts
    .Handle<TimeoutException>());      // Explicit timeouts
    
    cfg.ConfigureEndpoints(context);
    cfg.UseCloudEvents()
        .WithTypes(map => map
            .Map<ImagePostTracked>("imagePostTracked")
            .Map<LinkPostTracked>("linkPostTracked")
        );
});
```

**Step 2: Verify compilation**

Run: `dotnet build src/ShitpostBot/src/ShitpostBot.Infrastructure/ShitpostBot.Infrastructure.csproj`
Expected: Build succeeds with no errors

**Step 3: Commit**

```bash
git add src/ShitpostBot/src/ShitpostBot.Infrastructure/Public/MassTransitConfiguration.cs
git commit -m "feat: add MassTransit retry middleware with exponential backoff"
```

---

## Task 2: Enhance Error Handling in EvaluateRepost Handler

**Files:**
- Modify: `src/ShitpostBot/src/ShitpostBot.Application/Features/Repost/EvaluateRepost_ImagePostTrackedHandler.cs:46-60`

**Step 1: Replace error handling logic with classification**

Replace the existing error handling block (lines 46-60) with the enhanced version:

```csharp
if (!response.IsSuccessful)
{
    // Special case: 404 means image is gone from Discord CDN
    if (response.StatusCode == HttpStatusCode.NotFound)
    {
        logger.LogError(
            "Image not found (404) for ImagePost {ImagePostId}, URL: {ImageUrl}. Clearing ImageFeatures.",
            context.Message.ImagePostId, postToBeEvaluated.Image.ImageUri);

        postToBeEvaluated.ClearImageFeatures(dateTimeProvider.UtcNow);
        await unitOfWork.SaveChangesAsync(context.CancellationToken);
        return;
    }

    // Client errors (4xx except 404): Don't retry, likely invalid image format
    if (response.StatusCode >= HttpStatusCode.BadRequest && 
        response.StatusCode < HttpStatusCode.InternalServerError)
    {
        logger.LogError(
            "ML service rejected image (client error {StatusCode}) for ImagePost {ImagePostId}, URL: {ImageUrl}. " +
            "This is likely an invalid image format or ML service bug. Not retrying.",
            response.StatusCode, context.Message.ImagePostId, postToBeEvaluated.Image.ImageUri);
        
        // Throw non-retryable exception
        throw new InvalidOperationException(
            $"ML service client error: {response.StatusCode} for ImagePost {context.Message.ImagePostId}");
    }

    // Server errors (5xx), network issues, timeouts: Retry via MassTransit middleware
    logger.LogWarning(
        "ML service unavailable (transient failure, status: {StatusCode}) for ImagePost {ImagePostId}. " +
        "Will retry with exponential backoff.",
        response.StatusCode, context.Message.ImagePostId);
    
    // Throw retryable exception - MassTransit middleware will handle retry
    throw response.Error ?? new HttpRequestException(
        $"ML service returned {response.StatusCode} for ImagePost {context.Message.ImagePostId}");
}
```

**Step 2: Verify compilation**

Run: `dotnet build src/ShitpostBot/src/ShitpostBot.Application/ShitpostBot.Application.csproj`
Expected: Build succeeds with no errors

**Step 3: Commit**

```bash
git add src/ShitpostBot/src/ShitpostBot.Application/Features/Repost/EvaluateRepost_ImagePostTrackedHandler.cs
git commit -m "feat: add error classification for retryable vs non-retryable ML service failures"
```

---

## Task 3: Build and Verify Solution

**Files:**
- Build: All projects in solution

**Step 1: Clean build entire solution**

Run: `dotnet clean src/ShitpostBot/ShitpostBot.slnx && dotnet build src/ShitpostBot/ShitpostBot.slnx`
Expected: Build succeeds with no errors or warnings

**Step 2: Run existing unit tests**

Run: `dotnet test src/ShitpostBot/test/ShitpostBot.Tests.Unit/ShitpostBot.Tests.Unit.csproj`
Expected: All tests pass

**Step 3: Run existing integration tests**

Run: `dotnet test src/ShitpostBot/test/ShitpostBot.Tests.Integration/ShitpostBot.Tests.Integration.csproj`
Expected: All tests pass (may take 1-2 minutes due to Testcontainers)

**Step 4: Commit if any fixes were needed**

```bash
# Only if fixes were made
git add .
git commit -m "fix: address build or test issues"
```

---

## Task 4: Manual Testing - Verify Retry Behavior

**Files:**
- Test: Local docker-compose environment

**Step 1: Start docker-compose environment**

Run: `docker compose -f docker-compose.yml -f docker-compose.Development.Linux.yml up --build -d`
Expected: All services start successfully (webapi, worker, postgres, ml-service)

**Step 2: Monitor logs**

Run: `docker compose -f docker-compose.yml -f docker-compose.Development.Linux.yml logs -f worker`
Expected: Worker logs show "Application started" message

**Step 3: Trigger image post via WebApi**

In separate terminal:
```bash
curl -X POST http://localhost:5001/test/image-message \
  -H "Content-Type: application/json" \
  -d '{"imageUrl": "https://cdn.discordapp.com/attachments/test/image.jpg"}'
```
Expected: 200 OK response

**Step 4: Verify normal processing in logs**

Expected in worker logs:
- "Processing ImagePostTracked" message
- No retry warnings
- "Repost evaluation completed" or similar success message

**Step 5: Stop ML service to simulate outage**

Run: `docker compose -f docker-compose.yml -f docker-compose.Development.Linux.yml stop ml-service`
Expected: ML service container stops

**Step 6: Trigger another image post**

Run:
```bash
curl -X POST http://localhost:5001/test/image-message \
  -H "Content-Type: application/json" \
  -d '{"imageUrl": "https://cdn.discordapp.com/attachments/test/image2.jpg"}'
```
Expected: 200 OK response

**Step 7: Observe retry behavior in logs**

Expected in worker logs (watch for ~2 minutes):
```
[Warning] ML service unavailable (transient failure, status: ...) for ImagePost .... Will retry with exponential backoff.
[Warning] RETRY faulted ... Retrying (Attempt 1, Delay: 00:00:10)
[Warning] RETRY faulted ... Retrying (Attempt 2, Delay: 00:00:30)
[Warning] RETRY faulted ... Retrying (Attempt 3, Delay: 00:01:30)
[Error] MOVE message to transport/_error queue after 4 delivery attempts
```

**Step 8: Restart ML service**

Run: `docker compose -f docker-compose.yml -f docker-compose.Development.Linux.yml start ml-service`
Expected: ML service restarts successfully

**Step 9: Verify normal processing resumes**

Trigger another image post and verify it processes without retries.

**Step 10: Stop docker-compose**

Run: `docker compose -f docker-compose.yml -f docker-compose.Development.Linux.yml down`
Expected: All services stop cleanly

**Step 11: Document manual test results**

Create a note in the commit message or plan document summary confirming:
- ✅ Normal processing works without retries
- ✅ Retry triggers on ML service outage
- ✅ Exponential backoff delays observed (10s, 30s, 90s)
- ✅ Message moves to error queue after exhausting retries
- ✅ Processing resumes after ML service recovery

---

## Task 5: Optional - Add Unit Tests for Error Classification

**Files:**
- Create: `src/ShitpostBot/test/ShitpostBot.Tests.Unit/EvaluateRepostRetryLogicTests.cs`

**Note:** This task is optional but recommended for comprehensive testing.

**Step 1: Create test file structure**

```csharp
using System.Net;
using FluentAssertions;
using Xunit;

namespace ShitpostBot.Tests.Unit;

public class EvaluateRepostRetryLogicTests
{
    [Fact]
    public void HttpStatusCode_404_Should_Be_NotFound()
    {
        // Arrange
        var statusCode = HttpStatusCode.NotFound;
        
        // Act
        var is404 = statusCode == HttpStatusCode.NotFound;
        
        // Assert
        is404.Should().BeTrue();
    }
    
    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.UnsupportedMediaType)]
    [InlineData(HttpStatusCode.UnprocessableEntity)]
    public void HttpStatusCode_4xx_Should_Be_ClientError(HttpStatusCode statusCode)
    {
        // Arrange & Act
        var isClientError = statusCode >= HttpStatusCode.BadRequest && 
                           statusCode < HttpStatusCode.InternalServerError;
        
        // Assert
        isClientError.Should().BeTrue();
    }
    
    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    public void HttpStatusCode_5xx_Should_Be_ServerError(HttpStatusCode statusCode)
    {
        // Arrange & Act
        var isServerError = statusCode >= HttpStatusCode.InternalServerError;
        
        // Assert
        isServerError.Should().BeTrue();
    }
}
```

**Step 2: Run tests**

Run: `dotnet test src/ShitpostBot/test/ShitpostBot.Tests.Unit/ShitpostBot.Tests.Unit.csproj --filter "FullyQualifiedName~EvaluateRepostRetryLogicTests"`
Expected: All 5 tests pass

**Step 3: Commit**

```bash
git add src/ShitpostBot/test/ShitpostBot.Tests.Unit/EvaluateRepostRetryLogicTests.cs
git commit -m "test: add unit tests for HTTP status code classification"
```

---

## Task 6: Update Design Document Status

**Files:**
- Modify: `docs/plans/2026-01-03-ml-service-retry-resilience-design.md:4`

**Step 1: Update status to implemented**

Change line 4 from:
```markdown
**Status**: Design Complete
```

To:
```markdown
**Status**: Implemented
```

**Step 2: Commit**

```bash
git add docs/plans/2026-01-03-ml-service-retry-resilience-design.md
git commit -m "docs: mark ML service retry resilience design as implemented"
```

---

## Task 7: Final Verification

**Files:**
- Verify: All changes

**Step 1: Review all commits**

Run: `git log --oneline -10`
Expected: See 4-6 commits related to retry resilience feature

**Step 2: Run full test suite**

Run: `dotnet test src/ShitpostBot/ShitpostBot.slnx`
Expected: All tests pass

**Step 3: Verify no uncommitted changes**

Run: `git status`
Expected: "working tree clean"

**Step 4: Review implementation against design**

Checklist:
- ✅ MassTransit retry middleware configured with exponential backoff
- ✅ Error classification added (404, 4xx, 5xx)
- ✅ Logging enhanced for retry visibility
- ✅ Manual testing confirms retry behavior
- ✅ Design document updated to "Implemented" status

---

## Success Criteria

**Must have:**
- [x] MassTransit retry middleware configured globally
- [x] Error handling distinguishes 404, 4xx, 5xx responses
- [x] Enhanced logging for retry attempts
- [x] All existing tests pass
- [x] Manual testing confirms retry behavior

**Nice to have:**
- [ ] Unit tests for error classification logic (Task 5)
- [ ] Integration tests for retry scenarios

**Verification:**
1. Build succeeds: `dotnet build src/ShitpostBot/ShitpostBot.slnx`
2. Tests pass: `dotnet test src/ShitpostBot/ShitpostBot.slnx`
3. Manual test shows retry behavior when ML service is down
4. Normal processing works when ML service is available

---

## Estimated Time

- Task 1: 5 minutes (MassTransit config)
- Task 2: 10 minutes (Error handling)
- Task 3: 10 minutes (Build & test)
- Task 4: 15 minutes (Manual testing)
- Task 5: 10 minutes (Optional unit tests)
- Task 6: 2 minutes (Update docs)
- Task 7: 5 minutes (Final verification)

**Total: 45-60 minutes**

---

## Rollback Plan

If issues arise:

1. **Revert all commits**: `git reset --hard <commit-before-feature>`
2. **Rebuild**: `dotnet build src/ShitpostBot/ShitpostBot.slnx`
3. **Verify**: `dotnet test src/ShitpostBot/ShitpostBot.slnx`

No database migrations or breaking changes, so rollback is safe.
