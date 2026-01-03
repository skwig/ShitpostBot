# ML Service Resilience for Repost Evaluation

**Date**: 2026-01-03  
**Status**: Implemented

## Problem

When the ML service is unavailable (network issues, service down, timeouts), `EvaluateRepost_ImagePostTrackedHandler` throws exceptions that get logged but messages are never retried. This causes permanent loss of repost detection for images posted during ML service outages.

Current behavior:
- 404 errors: Handled correctly (clears features, doesn't retry)
- 5xx/network/timeout errors: Exception logged, message lost forever
- No retry mechanism configured in MassTransit

## Solution Overview

Implement MassTransit retry middleware with exponential backoff for transient failures:

1. **Global retry policy**: Configure exponential backoff (10s, 30s, 90s) for all MassTransit consumers
2. **Smart failure classification**: Retry only transient failures (5xx, network, timeouts), not client errors (4xx)
3. **Preserve 404 handling**: Existing special case for missing images remains unchanged
4. **Error queue**: Use MassTransit's built-in dead letter queue for exhausted retries
5. **Enhanced logging**: Distinguish between retryable and non-retryable failures

## Design Details

### 1. MassTransit Retry Configuration

**File**: `src/ShitpostBot/src/ShitpostBot.Infrastructure/Public/MassTransitConfiguration.cs`

Add retry middleware in the `UsingPostgres` configuration:

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

**Retry behavior**:
- Attempt 1: Immediate
- Attempt 2: 10 seconds later
- Attempt 3: 30 seconds later  
- Attempt 4: 90 seconds later
- Total time: ~2 minutes 10 seconds before moving to error queue

### 2. Enhanced Error Handling in Handler

**File**: `src/ShitpostBot/src/ShitpostBot.Application/Features/Repost/EvaluateRepost_ImagePostTrackedHandler.cs`

Replace the current error handling (lines 46-60) with classification logic:

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

### 3. Error Queue (Dead Letter Queue)

MassTransit automatically creates an `_error` queue in PostgreSQL when messages exhaust all retries.

**No configuration needed** - this is built-in behavior.

**Error queue contains**:
- Original message payload (`ImagePostTracked`)
- All exception details
- Retry attempt history
- Timestamps

**Manual recovery**: Messages can be inspected and replayed using MassTransit tooling or direct database queries.

### 4. Observability & Logging

**MassTransit Built-in Logging** (automatic):
```
[Warning] RETRY faulted ShitpostBot.Infrastructure.Messages:ImagePostTracked ... Retrying (Attempt 1, Delay: 00:00:10)
[Warning] RETRY faulted ShitpostBot.Infrastructure.Messages:ImagePostTracked ... Retrying (Attempt 2, Delay: 00:00:30)
[Warning] RETRY faulted ShitpostBot.Infrastructure.Messages:ImagePostTracked ... Retrying (Attempt 3, Delay: 00:01:30)
[Error] MOVE message to transport/_error queue after 4 delivery attempts
```

**Application Logging** (from handler):
```
[Warning] ML service unavailable (transient failure, status: ServiceUnavailable) for ImagePost 12345. Will retry with exponential backoff.
[Error] ML service rejected image (client error BadRequest) for ImagePost 67890, URL: https://... This is likely an invalid image format or ML service bug. Not retrying.
```

**Monitoring Recommendations**:
- Alert on high error queue depth (indicates sustained ML service outage)
- Track retry metrics via MassTransit's built-in telemetry
- Monitor ML service health separately

## Edge Cases Handled

| Scenario                    | Behavior                        | Rationale                                      |
| --------------------------- | ------------------------------- | ---------------------------------------------- |
| 404 from ML service         | Clear features, no retry        | Image gone from Discord CDN                    |
| 400/415 from ML service     | Log error, no retry             | Invalid image format, unrecoverable            |
| 500/502/503 from ML service | Retry with exponential backoff  | Transient server issue                         |
| Network timeout             | Retry with exponential backoff  | Transient network issue                        |
| ML service completely down  | Retry 3 times, then error queue | Prevents message loss                          |
| Error queue messages        | Manual inspection/replay        | Operator intervention for persistent issues    |
| Re-evaluation messages      | Same retry behavior             | `IsReevaluation` flag doesn't affect retry logic |

## Testing Strategy

### Unit Tests

Create `src/ShitpostBot/test/ShitpostBot.Tests.Unit/EvaluateRepostRetryLogicTests.cs`:

```csharp
public class EvaluateRepostRetryLogicTests
{
    [Fact]
    public void Should_Not_Retry_On_404_Response()
    {
        // Test that 404 handling doesn't throw retryable exception
    }
    
    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.UnsupportedMediaType)]
    public void Should_Not_Retry_On_4xx_Client_Errors(HttpStatusCode statusCode)
    {
        // Test that 4xx errors throw InvalidOperationException (non-retryable)
    }
    
    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public void Should_Retry_On_5xx_Server_Errors(HttpStatusCode statusCode)
    {
        // Test that 5xx errors throw retryable exception
    }
}
```

### Integration Tests

Add to `src/ShitpostBot/test/ShitpostBot.Tests.Integration/WebApiIntegrationTests.cs`:

```csharp
[Fact]
public async Task Should_Retry_ImagePostTracked_When_ML_Service_Unavailable()
{
    // 1. Stop ML service container
    // 2. Publish ImagePostTracked message
    // 3. Verify retry attempts in logs
    // 4. Restart ML service
    // 5. Verify message eventually processes successfully
}

[Fact]
public async Task Should_Not_Retry_ImagePostTracked_When_404()
{
    // 1. Publish ImagePostTracked with 404-generating URL
    // 2. Verify no retry attempts
    // 3. Verify ImageFeatures cleared
}
```

### Manual Testing

1. **Stop ML service**: `docker stop shitpostbot-ml-service-1`
2. **Post image to Discord**: Trigger `ImagePostTracked` message
3. **Observe logs**: Verify retry warnings appear (10s, 30s, 90s delays)
4. **Start ML service**: `docker start shitpostbot-ml-service-1`
5. **Verify**: Message processes successfully on next retry

## Performance Characteristics

**Impact on message processing**:
- **Normal operation**: No change (retry middleware is transparent)
- **During ML outage**: Messages delayed up to 2 minutes before error queue
- **Error queue growth**: ~20 messages/minute if ML service down during active hours
- **Recovery after ML service restore**: Error queue messages require manual replay

**Resource usage**:
- **Database**: Minimal (retry state stored in MassTransit tables)
- **Memory**: Negligible (retry logic is lightweight)
- **Network**: 3 additional HTTP requests per failed message (max)

## Files to Modify

1. **`src/ShitpostBot/src/ShitpostBot.Infrastructure/Public/MassTransitConfiguration.cs`**
   - Add `UseMessageRetry` configuration (~15 lines)

2. **`src/ShitpostBot/src/ShitpostBot.Application/Features/Repost/EvaluateRepost_ImagePostTrackedHandler.cs`**
   - Replace error handling logic (lines 46-60, ~25 lines)
   - Add error classification for 4xx vs 5xx

**Optional (for comprehensive testing)**:

3. **`src/ShitpostBot/test/ShitpostBot.Tests.Unit/EvaluateRepostRetryLogicTests.cs`** (new file)
   - Unit tests for retry classification (~50 lines)

4. **`src/ShitpostBot/test/ShitpostBot.Tests.Integration/WebApiIntegrationTests.cs`**
   - Integration tests for retry behavior (~30 lines added)

**No changes required**:
- No database migrations
- No NuGet packages (MassTransit retry is built-in)
- No appsettings changes (hardcoded retry config)
- No changes to Worker or WebApi Program.cs

## Implementation Effort

**Estimated time**: 1-2 hours including testing

**Breakdown**:
- MassTransit configuration: 15 minutes
- Handler error classification: 30 minutes
- Unit tests: 30 minutes
- Integration/manual testing: 30 minutes

## Future Considerations

- **Circuit breaker**: Add Polly circuit breaker if ML service outages cause cascading failures
- **Configurable retry parameters**: Move retry config to appsettings.json for runtime tuning
- **Metrics/alerting**: Add custom telemetry for ML service availability tracking
- **Error queue replay automation**: Build admin endpoint to replay failed messages
- **Graceful degradation**: Skip repost detection instead of queueing during extended outages
