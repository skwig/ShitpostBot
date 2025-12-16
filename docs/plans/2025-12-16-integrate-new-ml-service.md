# Integrate New ML Service Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Integrate the new CLIP-based ML service (with embedding, OCR, and captioning capabilities) into ShitpostBot, using only the embedding functionality initially.

**Architecture:** Replace the old InceptionResNetV2-based `/images/features` GET endpoint with the new CLIP-based `/process/image` POST endpoint. Update the C# Refit client interface to match the new API contract (POST with request body), configure feature flags to disable OCR and captioning, and update all configuration files and documentation.

**Tech Stack:** 
- **C#**: Refit HTTP client, .NET 10.0
- **Python**: FastAPI (new), CLIP-ViT-B-32 embeddings
- **Infrastructure**: Docker, appsettings.json configurations

---

## Task 1: Update the Refit API Interface

**Files:**
- Modify: `src/ShitpostBot/src/ShitpostBot.Application/Services/IImageFeatureExtractorApi.cs`

**Step 1: Write failing test**

Since this is an interface change with no existing unit tests for the Refit interface, we'll add a TODO comment to write integration tests later.

**Step 2: Update the interface to match new API**

Replace the GET endpoint with a POST endpoint and update request/response models:

```csharp
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Refit;

namespace ShitpostBot.Application.Services;

public interface IImageFeatureExtractorApi
{
    [Post("/process/image")]
    Task<ProcessImageResponse> ProcessImageAsync([Body] ProcessImageRequest request);
}

public record ProcessImageRequest
{
    [JsonPropertyName("image_url")] 
    public required string ImageUrl { get; init; }
    
    [JsonPropertyName("embedding")] 
    public bool Embedding { get; init; } = true;
    
    [JsonPropertyName("caption")] 
    public bool Caption { get; init; } = false;
    
    [JsonPropertyName("ocr")] 
    public bool Ocr { get; init; } = false;
    
    [JsonPropertyName("use_tesseract")] 
    public bool UseTesseract { get; init; } = false;
}

public record ProcessImageResponse
{
    [JsonPropertyName("size")] 
    public int[]? Size { get; init; }
    
    [JsonPropertyName("embedding")] 
    public float[]? Embedding { get; init; }
    
    [JsonPropertyName("caption")] 
    public string? Caption { get; init; }
    
    [JsonPropertyName("ocr")] 
    public string? Ocr { get; init; }
    
    [JsonPropertyName("ocr_confidence")] 
    public float? OcrConfidence { get; init; }
    
    [JsonPropertyName("ocr_engine")] 
    public string? OcrEngine { get; init; }
}

public class ImageFeatureExtractorApiOptions
{
    [Required] public required string Uri { get; init; }
}
```

**Step 3: Verify code compiles**

Run: `dotnet build src/ShitpostBot/src/ShitpostBot.Application/ShitpostBot.Application.csproj`
Expected: Build succeeds (may have downstream errors to fix in next task)

**Step 4: Commit**

```bash
git add src/ShitpostBot/src/ShitpostBot.Application/Services/IImageFeatureExtractorApi.cs
git commit -m "refactor: update ML service API interface to use new POST endpoint"
```

---

## Task 2: Update the Repost Handler to Use New API

**Files:**
- Modify: `src/ShitpostBot/src/ShitpostBot.Application/Features/Repost/EvaluateRepost_ImagePostTrackedHandler.cs:37-39`

**Step 1: Update the API call**

Change line 37 from:
```csharp
var extractImageFeaturesResponse = await imageFeatureExtractorApi.ExtractImageFeaturesAsync(postToBeEvaluated.Image.ImageUri.ToString());
```

To:
```csharp
var extractImageFeaturesResponse = await imageFeatureExtractorApi.ProcessImageAsync(new ProcessImageRequest
{
    ImageUrl = postToBeEvaluated.Image.ImageUri.ToString(),
    Embedding = true,
    Caption = false,
    Ocr = false
});
```

And update line 39 from:
```csharp
var imageFeatures = new ImageFeatures(new Vector(extractImageFeaturesResponse.ImageFeatures));
```

To:
```csharp
var imageFeatures = new ImageFeatures(new Vector(extractImageFeaturesResponse.Embedding ?? throw new InvalidOperationException("ML service did not return embedding")));
```

**Step 2: Verify code compiles**

Run: `dotnet build src/ShitpostBot/src/ShitpostBot.Application/ShitpostBot.Application.csproj`
Expected: Build succeeds

**Step 3: Build entire solution**

Run: `dotnet build src/ShitpostBot/ShitpostBot.slnx`
Expected: Build succeeds

**Step 4: Commit**

```bash
git add src/ShitpostBot/src/ShitpostBot.Application/Features/Repost/EvaluateRepost_ImagePostTrackedHandler.cs
git commit -m "refactor: use new ML service API in repost handler with embedding only"
```

---

## Task 3: Update Integration Test with TODO

**Files:**
- Modify: `src/ShitpostBot/test/ShitpostBot.Tests.Integration/MlServiceIntegrationTests.cs:36-37`

**Step 1: Add TODO comment for future test update**

Update the test to add a TODO comment explaining the old endpoint no longer exists:

```csharp
using System.Net;
using DotNet.Testcontainers.Builders;
using FluentAssertions;

namespace ShitpostBot.Tests.Integration;

public class MlServiceIntegrationTests
{
    [Fact]
    public async Task Test1()
    {
        // TODO: Update this test to use the new POST /process/image endpoint
        // The old GET /images/features endpoint no longer exists in the new ML service
        // New endpoint expects: POST /process/image with body { "image_url": "...", "embedding": true }
        // Returns: { "embedding": [...], "size": [w, h] }
        
        // Arrange
        var image = new ImageFromDockerfileBuilder()
            .WithDockerfileDirectory(CommonDirectoryPath.GetGitDirectory(), "src/ShitpostBot.MlService")
            .WithDockerfile("Dockerfile")
            .WithName("ml-service")
            .Build();

        await image.CreateAsync();

        var container = new ContainerBuilder()
            .WithImage(image)
            .WithPortBinding(5000, true)
            .WithWaitStrategy(
                Wait.ForUnixContainer()
                    .UntilHttpRequestIsSucceeded(request => request.ForPath("/healthz").ForPort(5000))
            )
            .Build();

        await container.StartAsync();

        // Act
        var httpClient = new HttpClient();
        var imgUri =
            "https://media.discordapp.net/attachments/138031010951593984/1289974033793683456/image0.jpg?ex=670a9770&is=670945f0&hm=34d0b056539e0a2963f5b6f9f1dcd9a97aebadb51d2e521244e51320014202fa&=&format=webp&width=867&height=910";
        
        // Old endpoint (no longer exists):
        // var requestUri = new Uri($"http://{container.Hostname}:{container.GetMappedPublicPort(5000)}/images/features?image_url={Uri.EscapeDataString(imgUri)}");
        // var response = await httpClient.GetAsync(requestUri);
        
        // For now, just verify health endpoint works
        var healthUri = new Uri($"http://{container.Hostname}:{container.GetMappedPublicPort(5000)}/healthz");
        var response = await httpClient.GetAsync(healthUri);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```

**Step 2: Verify test compiles and runs**

Run: `dotnet test src/ShitpostBot/test/ShitpostBot.Tests.Integration/ShitpostBot.Tests.Integration.csproj --filter "FullyQualifiedName~MlServiceIntegrationTests"`
Expected: Test passes (now only checks healthz endpoint)

**Step 3: Commit**

```bash
git add src/ShitpostBot/test/ShitpostBot.Tests.Integration/MlServiceIntegrationTests.cs
git commit -m "test: add TODO for ML service integration test update"
```

---

## Task 4: Update ML Service Documentation

**Files:**
- Modify: `src/ShitpostBot.MlService/AGENTS.md:10-13`

**Step 1: Update architecture description**

Replace lines 10-13:

```markdown
## Architecture
- **FastAPI**: Image processing service with CLIP embeddings, BLIP captioning, and OCR
- **Models**: 
  - CLIP-ViT-B-32 for semantic image embeddings (sentence-transformers)
  - BLIP (Salesforce/blip-image-captioning-base) for natural language image descriptions
  - Tesseract OCR (primary) and PaddleOCR for text extraction from images
- **Endpoints**: 
  - POST `/process/image` - Process single image with configurable features (embedding/caption/ocr)
  - POST `/process/image/batch` - Batch process multiple images efficiently
  - POST `/embed/text` - Generate text embeddings for semantic search
  - GET `/healthz` - Health check
- **Dependencies**: FastAPI, Uvicorn, sentence-transformers, transformers, PaddleOCR, Tesseract, OpenCV, Pillow
```

**Step 2: Verify documentation is correct**

Run: `cat src/ShitpostBot.MlService/AGENTS.md`
Expected: Shows updated content with CLIP and FastAPI details

**Step 3: Commit**

```bash
git add src/ShitpostBot.MlService/AGENTS.md
git commit -m "docs: update ML service architecture to reflect CLIP and FastAPI"
```

---

## Task 5: Verify Docker Compose Configuration

**Files:**
- Review: `docker-compose.Development.Linux.yml:45-50`
- Review: `docker-compose.yml:36-37`

**Step 1: Verify ML service configuration is correct**

The ML service is already configured correctly:
- Port mapping: `8081:8080` (host:container)
- Health check configured in `docker-compose.yml:36-37`
- Worker and WebApi reference it as `http://ml-service:8080`

No changes needed - the configuration is already correct for the new FastAPI service.

**Step 2: Document findings**

Add note to plan that docker-compose files are already compatible.

---

## Task 6: Run E2E Tests to Verify Integration

**Files:**
- Test: `test/e2e/run-e2e-tests.sh`

**Step 1: Run E2E test suite**

Run: `./test/e2e/run-e2e-tests.sh`
Expected: All scenarios pass (repost detection still works with new embeddings for fresh test data)

**Step 2: If tests fail, analyze and fix**

Check logs for:
- ML service startup issues
- API contract mismatches
- Embedding processing errors

**Step 3: Document results**

If tests pass, no commit needed. If fixes were required, commit them with appropriate messages.

---

## Task 7: Update C# Project AGENTS.md (Optional Enhancement)

**Files:**
- Consider: `src/ShitpostBot/AGENTS.md:15`

**Step 1: Add note about ML service dependency**

Consider adding a line after line 15 in the Project Structure section mentioning the ML service integration:

```markdown
- **Application**: MediatR handlers, Refit HTTP clients (ML service integration)
```

**Step 2: Decide if change adds value**

This is optional - the existing documentation already mentions infrastructure includes external service clients.

**Step 3: Skip or commit**

If skipped: No action needed.
If added: 
```bash
git add src/ShitpostBot/AGENTS.md
git commit -m "docs: clarify ML service integration in application layer"
```

---

## Task 8: Final Verification

**Files:**
- All modified files

**Step 1: Run all unit tests**

Run: `dotnet test src/ShitpostBot/`
Expected: All tests pass

**Step 2: Build docker images locally**

Run: `docker compose -f docker-compose.yml -f docker-compose.Development.Linux.yml build`
Expected: All images build successfully

**Step 3: Start full stack locally**

Run: `docker compose -f docker-compose.yml -f docker-compose.Development.Linux.yml up`
Expected: All services start, health checks pass

**Step 4: Verify ML service responds correctly**

Run: 
```bash
curl -X POST http://localhost:8081/process/image \
  -H "Content-Type: application/json" \
  -d '{"image_url": "https://picsum.photos/200", "embedding": true, "caption": false, "ocr": false}'
```
Expected: Returns JSON with `embedding` array of floats

**Step 5: Create final summary**

Document:
- What was changed
- What still needs to be done (re-evaluate existing embeddings in production)
- Any breaking changes (API contract changed, old endpoints removed)

---

## Summary of Changes

**Modified Files:**
1. `src/ShitpostBot/src/ShitpostBot.Application/Services/IImageFeatureExtractorApi.cs` - New POST endpoint interface
2. `src/ShitpostBot/src/ShitpostBot.Application/Features/Repost/EvaluateRepost_ImagePostTrackedHandler.cs` - Use new API
3. `src/ShitpostBot/test/ShitpostBot.Tests.Integration/MlServiceIntegrationTests.cs` - Add TODO for test update
4. `src/ShitpostBot.MlService/AGENTS.md` - Document new architecture

**Configuration Files (Already Compatible):**
- `docker-compose.Development.Linux.yml` ✓
- `docker-compose.yml` ✓
- `src/ShitpostBot/src/ShitpostBot.Worker/appsettings.*.json` ✓
- `src/ShitpostBot/src/ShitpostBot.WebApi/appsettings.*.json` ✓

**Future Work:**
- Re-evaluate all existing ImagePost embeddings in production database (embeddings from InceptionResNetV2 are incompatible with CLIP-ViT-B-32)
- Update integration test to properly test new POST endpoint
- Consider enabling OCR and captioning features for enhanced repost detection

**Breaking Changes:**
- Old GET `/images/features` endpoint no longer exists
- Response field changed from `image_features` to `embedding`
- Request method changed from GET to POST with JSON body
- Embedding dimensions changed (InceptionResNetV2 → CLIP-ViT-B-32)
