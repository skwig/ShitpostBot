# Handle 404 Image Downloads Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Handle deleted/unavailable images (404) gracefully by returning 404 from ML service and setting ImageFeatures to null in the database, preventing infinite retry loops.

**Architecture:** When the ML service attempts to download an image that returns 404, catch the error and return a 404 response to the caller. In the C# handler, catch 404 responses from the ML service using Refit's ApiException, set ImageFeatures to null, and save the post. This marks the post as "processed but unavailable" so the PostReevaluator skips it.

**Tech Stack:** Python (FastAPI, requests), C# (.NET, Refit, MassTransit), pytest, NUnit

---

## Task 1: Make ML Service Return 404 for Unavailable Images

**Files:**
- Modify: `src/ShitpostBot.MlService/src/image_loader.py:28-38`
- Modify: `src/ShitpostBot.MlService/src/app.py:201-233`

**Step 1: Update ImageLoader to re-raise HTTPError**

In `src/ShitpostBot.MlService/src/image_loader.py`, the `load` method at line 28 already calls `response.raise_for_status()` which raises `requests.exceptions.HTTPError` on 4xx/5xx. We need to let this propagate.

Current code is fine - it already raises `HTTPError` on 404. No changes needed in `image_loader.py`.

**Step 2: Update app.py to catch and return 404**

In `src/ShitpostBot.MlService/src/app.py`, modify the `/process/image` endpoint to catch HTTPError and return appropriate status codes:

```python
# Add import at top of file (around line 6)
from fastapi import FastAPI, UploadFile, File, Depends, Query, HTTPException

# Replace the process_image function (lines 201-233) with:
@app.post("/process/image")
async def process_image(request: ProcessImageRequest):
    """Process image with optional embedding, captioning, and OCR extraction"""
    try:
        cv_img, pil_img = _load_and_convert_image(request.image_url)
    except requests.exceptions.HTTPError as e:
        # Re-raise HTTP errors from image download as FastAPI HTTPException
        status_code = e.response.status_code if e.response else 500
        raise HTTPException(
            status_code=status_code,
            detail=f"Failed to download image: {str(e)}"
        )
    except Exception as e:
        # Other errors (network issues, invalid image format, etc.)
        raise HTTPException(
            status_code=500,
            detail=f"Failed to process image: {str(e)}"
        )

    result = {
        "image_url": request.image_url,
        "size": list(pil_img.size),
        "model_name": MODEL_NAME,
    }

    if request.embedding:
        embedding = _generate_embedding(pil_img)
        result["embedding"] = embedding.tolist()

    if request.caption:
        caption = _generate_caption(pil_img)
        result["caption"] = caption

    if request.ocr:
        if request.use_tesseract:
            text, confidence = _extract_ocr_text_tesseract(pil_img)
            ocr_engine_used = "tesseract"
        else:
            text, confidence = _extract_ocr_text(cv_img)
            ocr_engine_used = "paddleocr"

        result["ocr"] = text
        result["ocr_confidence"] = confidence
        result["ocr_engine"] = ocr_engine_used

    return result
```

**Verification:** Check imports and function signature
```bash
grep "from fastapi import" src/ShitpostBot.MlService/src/app.py | head -1
grep "async def process_image" src/ShitpostBot.MlService/src/app.py
```
Expected: Shows `HTTPException` in imports and function definition

**Step 3: Add missing import for requests**

In `src/ShitpostBot.MlService/src/app.py`, add import for requests at top of file (around line 10):

```python
import requests
```

**Verification:** Check import exists
```bash
grep "^import requests" src/ShitpostBot.MlService/src/app.py
```
Expected: Shows `import requests`

**Step 4: Commit the ML service changes**

```bash
git add src/ShitpostBot.MlService/src/app.py
git commit -m "feat(ml-service): return 404 when image download fails with 404"
```

**Verification:** Commit created
```bash
git log -1 --oneline
```
Expected: Shows commit message

---

## Task 2: Write Test for ML Service 404 Handling

**Files:**
- Create: `src/ShitpostBot.MlService/test/test_image_404.py`

**Step 1: Create test file for 404 handling**

Create `src/ShitpostBot.MlService/test/test_image_404.py`:

```python
import pytest
from fastapi.testclient import TestClient
from unittest.mock import patch, MagicMock
import requests

import sys
import os
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '../src'))

from app import app

client = TestClient(app)


def test_process_image_returns_404_when_image_not_found():
    """Test that 404 from image URL returns 404 response"""
    
    # Mock requests.get to return 404
    mock_response = MagicMock()
    mock_response.status_code = 404
    mock_response.raise_for_status.side_effect = requests.exceptions.HTTPError(response=mock_response)
    
    with patch('image_loader.requests.get', return_value=mock_response):
        response = client.post(
            "/process/image",
            json={
                "image_url": "https://example.com/deleted.jpg",
                "embedding": True,
                "caption": False,
                "ocr": False
            }
        )
    
    assert response.status_code == 404
    assert "Failed to download image" in response.json()["detail"]


def test_process_image_returns_500_for_other_errors():
    """Test that non-HTTP errors return 500"""
    
    with patch('image_loader.requests.get', side_effect=Exception("Network error")):
        response = client.post(
            "/process/image",
            json={
                "image_url": "https://example.com/image.jpg",
                "embedding": True,
                "caption": False,
                "ocr": False
            }
        )
    
    assert response.status_code == 500
    assert "Failed to process image" in response.json()["detail"]
```

**Verification:** Test file created
```bash
ls src/ShitpostBot.MlService/test/test_image_404.py
```
Expected: File exists

**Step 2: Run tests to verify they pass**

```bash
cd src/ShitpostBot.MlService
pytest test/test_image_404.py -v
```

**Verification:** Tests should pass
Expected: `2 passed`

**Step 3: Commit the test**

```bash
git add src/ShitpostBot.MlService/test/test_image_404.py
git commit -m "test(ml-service): add tests for 404 image handling"
```

**Verification:** Commit created
```bash
git log -1 --oneline
```
Expected: Shows commit message

---

## Task 3: Handle 404 in C# Repost Handler

**Files:**
- Modify: `src/ShitpostBot/src/ShitpostBot.Application/Features/Repost/EvaluateRepost_ImagePostTrackedHandler.cs:29-90`

**Step 1: Add using statement for Refit**

At the top of `EvaluateRepost_ImagePostTrackedHandler.cs`, add:

```csharp
using Refit;
```

**Verification:** Check using statement
```bash
grep "using Refit;" src/ShitpostBot/src/ShitpostBot.Application/Features/Repost/EvaluateRepost_ImagePostTrackedHandler.cs
```
Expected: Shows the using statement

**Step 2: Wrap ML service call in try-catch**

In `EvaluateRepost_ImagePostTrackedHandler.cs`, replace the `Consume` method (lines 29-90) with:

```csharp
public async Task Consume(ConsumeContext<ImagePostTracked> context)
{
    var postToBeEvaluated = await unitOfWork.ImagePostsRepository.GetById(context.Message.ImagePostId);
    if (postToBeEvaluated == null)
    {
        throw new InvalidOperationException($"ImagePost {context.Message.ImagePostId} not found");
    }

    ProcessImageResponse? extractImageFeaturesResponse = null;
    
    try
    {
        extractImageFeaturesResponse = await imageFeatureExtractorApi.ProcessImageAsync(new ProcessImageRequest
        {
            ImageUrl = postToBeEvaluated.Image.ImageUri.ToString(),
            Embedding = true,
            Caption = false,
            Ocr = false
        });
    }
    catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
    {
        // Image not found (404) - mark as unavailable by setting features to null
        logger.LogWarning("Image not found (404) for ImagePost {ImagePostId}, URL: {ImageUrl}. Setting ImageFeatures to null.",
            context.Message.ImagePostId, postToBeEvaluated.Image.ImageUri);
        
        postToBeEvaluated.SetImageFeatures(null, dateTimeProvider.UtcNow);
        await unitOfWork.SaveChangesAsync(context.CancellationToken);
        return;
    }

    var imageFeatures = new ImageFeatures(
        extractImageFeaturesResponse.ModelName,
        new Vector(extractImageFeaturesResponse.Embedding ?? throw new InvalidOperationException("ML service did not return embedding"))
    );
    postToBeEvaluated.SetImageFeatures(imageFeatures, dateTimeProvider.UtcNow);

    await unitOfWork.SaveChangesAsync(context.CancellationToken);

    if (context.Message.IsReevaluation)
    {
        logger.LogDebug("Skipping repost detection for ImagePost {ImagePostId} (re-evaluation mode)", context.Message.ImagePostId);
        return;
    }

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
```

**Verification:** Check try-catch exists
```bash
grep -A 5 "catch (ApiException ex)" src/ShitpostBot/src/ShitpostBot.Application/Features/Repost/EvaluateRepost_ImagePostTrackedHandler.cs
```
Expected: Shows the catch block with NotFound handling

**Step 3: Commit the handler changes**

```bash
git add src/ShitpostBot/src/ShitpostBot.Application/Features/Repost/EvaluateRepost_ImagePostTrackedHandler.cs
git commit -m "feat(repost): handle 404 from ML service by setting ImageFeatures to null"
```

**Verification:** Commit created
```bash
git log -1 --oneline
```
Expected: Shows commit message

---

## Task 4: Update Domain Model to Allow Null ImageFeatures

**Files:**
- Modify: `src/ShitpostBot/src/ShitpostBot.Domain/Posts/ImagePost.cs`

**Step 1: Check if SetImageFeatures accepts null**

```bash
grep -A 10 "SetImageFeatures" src/ShitpostBot/src/ShitpostBot.Domain/Posts/ImagePost.cs
```

Expected: Shows the method signature

**Step 2: Update SetImageFeatures to accept nullable ImageFeatures**

If the method signature is:
```csharp
public void SetImageFeatures(ImageFeatures imageFeatures, DateTimeOffset evaluatedOn)
```

Change it to:
```csharp
public void SetImageFeatures(ImageFeatures? imageFeatures, DateTimeOffset evaluatedOn)
{
    Image.ImageFeatures = imageFeatures;
    Image.EvaluatedOn = evaluatedOn;
}
```

**Verification:** Check nullable parameter
```bash
grep "SetImageFeatures(ImageFeatures?" src/ShitpostBot/src/ShitpostBot.Domain/Posts/ImagePost.cs
```
Expected: Shows nullable parameter

**Step 3: Commit domain model change**

```bash
git add src/ShitpostBot/src/ShitpostBot.Domain/Posts/ImagePost.cs
git commit -m "feat(domain): allow null ImageFeatures in SetImageFeatures"
```

**Verification:** Commit created
```bash
git log -1 --oneline
```
Expected: Shows commit message

---

## Task 5: Update PostReevaluator to Skip Posts with Null Features

**Files:**
- Modify: `src/ShitpostBot/src/ShitpostBot.PostReevaluator/PostReevaluatorWorker.cs:40-47`

**Step 1: Update query to exclude null ImageFeatures**

In `PostReevaluatorWorker.cs`, modify the query at lines 40-47:

```csharp
// OLD:
var imagePosts = await imagePostsReader
    .All()
    .Where(p => p.Image.ImageFeatures != null 
             && p.Image.ImageFeatures.ModelName != currentModelName)
    .OrderBy(p => p.Id)
    .Skip(pageNumber * PageSize)
    .Take(PageSize)
    .ToListAsync(cancellationToken);

// NEW (add check that EvaluatedOn is not null):
var imagePosts = await imagePostsReader
    .All()
    .Where(p => p.Image.ImageFeatures != null 
             && p.Image.EvaluatedOn != null
             && p.Image.ImageFeatures.ModelName != currentModelName)
    .OrderBy(p => p.Id)
    .Skip(pageNumber * PageSize)
    .Take(PageSize)
    .ToListAsync(cancellationToken);
```

**Context:** The query already checks `p.Image.ImageFeatures != null`, but we should also ensure `EvaluatedOn != null` to skip posts that were evaluated but have null features (404 images).

Actually, checking `ImageFeatures != null` is sufficient because:
- If image was 404, ImageFeatures is set to null
- If ImageFeatures is null, the post won't be in the query results
- Posts with null ImageFeatures won't match the ModelName check anyway

**No changes needed** - the existing query already handles this correctly.

**Step 2: Add logging for skipped posts**

Actually, we want to count posts that have been evaluated but have null features (404s). Let's add a log message at the end showing how many such posts exist:

In `PostReevaluatorWorker.cs`, after the while loop (around line 75), add:

```csharp
// After the "No more outdated embeddings found" log at line 52
logger.LogInformation("No more outdated embeddings found. Migration complete.");

// Add this:
var unavailableCount = await imagePostsReader
    .All()
    .CountAsync(p => p.Image.EvaluatedOn != null && p.Image.ImageFeatures == null, cancellationToken);

if (unavailableCount > 0)
{
    logger.LogInformation("{UnavailableCount} posts have unavailable images (404) and were skipped", unavailableCount);
}

break;
```

**Verification:** Check logging added
```bash
grep "unavailable images" src/ShitpostBot/src/ShitpostBot.PostReevaluator/PostReevaluatorWorker.cs
```
Expected: Shows the log message

**Step 3: Commit the PostReevaluator logging**

```bash
git add src/ShitpostBot/src/ShitpostBot.PostReevaluator/PostReevaluatorWorker.cs
git commit -m "feat(reevaluator): log count of unavailable images (404)"
```

**Verification:** Commit created
```bash
git log -1 --oneline
```
Expected: Shows commit message

---

## Task 6: Write Integration Test for 404 Handling

**Files:**
- Create: `src/ShitpostBot/test/ShitpostBot.Tests.Integration/Handle404ImageTests.cs`

**Step 1: Create integration test**

Create `src/ShitpostBot/test/ShitpostBot.Tests.Integration/Handle404ImageTests.cs`:

```csharp
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Protected;
using NUnit.Framework;
using Refit;
using ShitpostBot.Application.Services;
using ShitpostBot.Domain;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Messages;
using System.Net;

namespace ShitpostBot.Tests.Integration;

[TestFixture]
public class Handle404ImageTests
{
    [Test]
    public async Task WhenImageReturns404_ShouldSetImageFeaturesToNull()
    {
        // Arrange - Mock ML service to return 404
        var mockApiException = await ApiException.Create(
            new HttpRequestMessage(),
            HttpMethod.Post,
            new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("{\"detail\":\"Failed to download image\"}")
            },
            new RefitSettings()
        );

        var mockMlService = new Mock<IImageFeatureExtractorApi>();
        mockMlService
            .Setup(x => x.ProcessImageAsync(It.IsAny<ProcessImageRequest>()))
            .ThrowsAsync(mockApiException);

        // Create test services with mocked ML service
        var services = new ServiceCollection();
        // Add required services (DbContext, UnitOfWork, etc.)
        // Replace IImageFeatureExtractorApi with mock
        services.AddSingleton(mockMlService.Object);

        var provider = services.BuildServiceProvider();
        var unitOfWork = provider.GetRequiredService<IUnitOfWork>();

        // Create a test ImagePost
        var imagePost = new ImagePost(/* constructor args */);
        await unitOfWork.ImagePostsRepository.Add(imagePost);
        await unitOfWork.SaveChangesAsync();

        // Act - Publish ImagePostTracked message
        var bus = provider.GetRequiredService<IBus>();
        await bus.Publish(new ImagePostTracked
        {
            ImagePostId = imagePost.Id,
            IsReevaluation = false
        });

        // Wait for message processing
        await Task.Delay(TimeSpan.FromSeconds(2));

        // Assert - ImageFeatures should be null
        var updatedPost = await unitOfWork.ImagePostsRepository.GetById(imagePost.Id);
        updatedPost.Should().NotBeNull();
        updatedPost!.Image.ImageFeatures.Should().BeNull();
        updatedPost.Image.EvaluatedOn.Should().NotBeNull();
    }
}
```

**Note:** This is a skeleton test. The actual test would need proper setup of DbContext, test containers, etc. For now, we'll rely on manual testing.

**Step 2: Skip creating the full integration test**

Integration tests for this codebase require significant setup (Testcontainers, MassTransit, etc.). Instead, we'll verify manually with the running system.

**Step 3: Document manual testing approach**

Create a manual test plan:

```markdown
## Manual Test Plan for 404 Handling

1. Start ML service: `docker compose up ml-service`
2. Start worker: `docker compose up worker`
3. Post a message with an image in Discord
4. Wait for image to be processed
5. Delete the image from Discord (or use a URL that returns 404)
6. Run PostReevaluator: `docker compose up post-reevaluator`
7. Check logs:
   - Should see "Image not found (404)" warning
   - Should see post being skipped
   - PostReevaluator should complete without errors
8. Check database:
   - ImagePost should have `ImageFeatures = null`
   - `EvaluatedOn` should be set
```

Skip the integration test for now.

---

## Task 7: Build and Test ML Service Changes

**Files:**
- None (verification only)

**Step 1: Run ML service unit tests**

```bash
cd src/ShitpostBot.MlService
pytest test/ -v
```

**Verification:** Tests should pass
Expected: All tests pass including new 404 tests

**Step 2: Build ML service Docker image**

```bash
docker compose build ml-service
```

**Verification:** Build succeeds
Expected: `Successfully tagged ...`

**Step 3: Start ML service**

```bash
docker compose up ml-service -d
```

**Verification:** Service is running
Expected: Container is up

**Step 4: Test 404 handling manually**

```bash
curl -X POST http://localhost:8000/process/image \
  -H "Content-Type: application/json" \
  -d '{"image_url": "https://httpstat.us/404", "embedding": true}'
```

**Verification:** Should return 404
Expected: `{"detail":"Failed to download image: ..."}` with status 404

---

## Task 8: Build and Test C# Changes

**Files:**
- None (verification only)

**Step 1: Build C# solution**

```bash
cd src/ShitpostBot
dotnet build
```

**Verification:** Build succeeds
Expected: `Build succeeded. 0 Warning(s). 0 Error(s).`

**Step 2: Run C# tests**

```bash
cd src/ShitpostBot
dotnet test
```

**Verification:** Tests should pass
Expected: All existing tests pass

**Step 3: Build Docker images**

```bash
docker compose build worker post-reevaluator
```

**Verification:** Builds succeed
Expected: Images built successfully

---

## Task 9: Update Documentation

**Files:**
- Modify: `src/ShitpostBot/AGENTS.md` or create new doc

**Step 1: Document 404 handling behavior**

Add to relevant documentation (or create a new section):

```markdown
## Image Availability Handling

When processing images, the system handles unavailable images (404) gracefully:

1. **ML Service**: Returns 404 when image URL returns 404
2. **Repost Handler**: Catches 404, sets `ImageFeatures` to `null`, saves post
3. **PostReevaluator**: Skips posts with `null` features and logs count

This prevents:
- Infinite retry loops for deleted images
- Messages stuck in error queues
- PostReevaluator never completing

**Database State:**
- `ImageFeatures`: `null` (image unavailable)
- `EvaluatedOn`: Set to current time (marks as processed)

**Logs to check:**
- ML Service: HTTP 404 errors
- Worker: "Image not found (404)" warnings
- PostReevaluator: "N posts have unavailable images (404)"
```

**Verification:** Documentation added
```bash
grep "404" src/ShitpostBot/AGENTS.md
```
Expected: Shows documentation

**Step 2: Commit documentation**

```bash
git add src/ShitpostBot/AGENTS.md
git commit -m "docs: document 404 image handling behavior"
```

**Verification:** Commit created
```bash
git log -1 --oneline
```
Expected: Shows commit message

---

## Task 10: Final Verification

**Files:**
- None (testing only)

**Step 1: Review all changes**

```bash
git log --oneline -10
```

**Verification:** Should show all commits from this implementation
Expected: Shows commits for ML service, handler, domain, tests, docs

**Step 2: End-to-end test scenario**

**Setup:**
1. Start full stack: `docker compose up -d`
2. Ensure database is clean

**Test:**
1. Post image in Discord → should process normally
2. Simulate 404 by using invalid URL in database
3. Trigger re-evaluation → should handle 404 gracefully
4. Check logs for 404 warnings
5. Check database: `ImageFeatures` is null, `EvaluatedOn` is set
6. Run PostReevaluator → should skip the 404 post

**Step 3: Verify PostReevaluator completes**

```bash
docker compose up post-reevaluator
```

**Verification:** Should complete without errors
Expected: "Migration complete" with count of unavailable images

**Step 4: Confirm no messages in error queue**

Check MassTransit error queue (RabbitMQ):
```bash
docker exec -it rabbitmq rabbitmqctl list_queues
```

**Verification:** No messages stuck in error queues
Expected: Error queues are empty or don't exist

---

## Summary

This implementation handles 404 errors from image downloads gracefully:

1. **ML Service**: Returns 404 when image URL returns 404
   - Added `HTTPException` import and try-catch in `process_image`
   - Added `requests` import
   - Returns appropriate HTTP status codes

2. **C# Handler**: Catches 404 and sets `ImageFeatures = null`
   - Added `using Refit;`
   - Wrapped ML service call in try-catch
   - Catches `ApiException` with `StatusCode.NotFound`
   - Saves post with null features and current timestamp

3. **Domain Model**: Allows null `ImageFeatures`
   - Updated `SetImageFeatures` to accept nullable parameter

4. **PostReevaluator**: Skips posts with null features
   - Existing query already handles this correctly
   - Added logging for count of unavailable images

5. **Testing**: Python tests and manual test plan
   - Unit tests for ML service 404 handling
   - Manual test plan for end-to-end verification

**Benefits:**
- No infinite retry loops
- No messages stuck in error queues
- PostReevaluator completes successfully
- Clear logging of unavailable images
- Database accurately reflects post status

**Behavior:**
- Posts with 404 images are marked as "evaluated but unavailable"
- These posts won't be re-evaluated on subsequent runs
- System continues processing other posts normally
